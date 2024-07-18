using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using ImpliciX.Driver.Common;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using static ImpliciX.Language.Core.SideEffect;

namespace ImpliciX.RTUModbus.Controllers
{
    public abstract class ModbusSlave : IBoardSlave
    {
        protected readonly ModbusSlaveDefinition _definition;
        private readonly ModbusSlaveSettings _settings;
        private readonly IModbusAdapter _modbusAdapter;
        private readonly IClock _clock;
        private readonly DriverStateKeeper _driverStateKeeper;

        protected ModbusSlave(ModbusSlaveDefinition definition, ModbusSlaveSettings settings, IModbusAdapter modbusAdapter, IClock clock, DriverStateKeeper driverStateKeeper)
        {
            _definition = definition;
            _settings = settings;
            _modbusAdapter = modbusAdapter;
            _clock = clock;
            _driverStateKeeper = driverStateKeeper;
        }

        
        public Result2<IDataModelValue[],CommunicationDetails> ExecuteCommand(Urn commandUrn, object arg)
        {
            var commandMap = _definition.CommandMap;
            var createCommand = CreateCommand(commandMap, commandUrn, arg, _driverStateKeeper);
            if (createCommand.IsError) 
                return (createCommand.Error,new CommunicationDetails(0,0));
            
            var writeRegisters = TryWriteRegisters2(createCommand.Value, commandUrn, arg);
            var communicationDetails = writeRegisters.Both;
            if(writeRegisters.IsError)
                return (writeRegisters.Error,communicationDetails);
            
            var updateCache = _driverStateKeeper.TryUpdate(createCommand.Value.State);
            if (updateCache.IsError)
                return (updateCache.Error, communicationDetails);

            return (MeasureOfCommand(commandUrn, arg), communicationDetails);
        }
        
        private Result<Command> CreateCommand(ICommandMap map, Urn urn, object arg, IDriverStateKeeper stateKeeper)
        {
            Contract.Assert(map.ContainsKey(urn), $"CommandMap does not contains urn {urn}");
            return 
                from state in stateKeeper.TryRead(urn)
                let currentTime = _clock.Now()
                from command in TryRun(
                    () => map.ModbusCommandFactory(urn).Invoke(arg, currentTime, state),
                    (ex) => new Error("CommandError", $"Could not execute command. Exception: {ex.Message}")).UnWrap()
                select command;
        }

        public Result2<IDataModelValue[], CommunicationDetails> ReadProperties(MapKind mapKind)
        {
            var mapDefinition = _definition.ReadPropertiesMaps.GetValueOrDefault(mapKind, RegistersMap.Empty());
            var segmentsDefinitions = mapDefinition.SegmentsDefinition;
            ushort successCommunications = 0;
            
            var registerSegments = new RegistersSegment[segmentsDefinitions.Length];
            
            for (var index = 0; index < segmentsDefinitions.Length; index++)
            {
                var segDef = segmentsDefinitions[index];
                try
                {
                    Log.Debug("[{@SlaveName}] Reading segment (StartAddress={@StartAddress}, NumberOfRegisters={@NbRegisters})",
                        Name, segDef.StartAddress, segDef.RegistersToRead);
                    var readRegisters = _modbusAdapter.ReadRegisters(_settings.Factory, segDef.Kind, segDef.StartAddress, segDef.RegistersToRead);
                    successCommunications += 1;
                    registerSegments[index] = new RegistersSegment(segDef, readRegisters);
                }
                catch (Exception e)
                {
                    LogReadException(e)(segDef);
                    return (InterpretReadError(e), new CommunicationDetails(successCommunications, 1));
                }
            }

            var modbusRegisters = ModbusRegisters.Create(registerSegments);
            var decodedValues = mapDefinition.Eval(modbusRegisters, _clock.Now(), _driverStateKeeper)
                .GetValueOrDefault(Array.Empty<IDataModelValue>()).ToArray();
            return (decodedValues, new CommunicationDetails(successCommunications, 0));
        }
        public virtual bool IsConcernedByCommandRequested(Urn crUrn) =>
            _definition.CommandMap.ContainsKey(crUrn);


        private Result2<Unit, CommunicationDetails> TryWriteRegisters2(Command command, Urn commandUrn, object arg)
        {
            ushort failureCount = 0;
            var result = TryRun(() =>
                {
                    _modbusAdapter.WriteRegisters(_settings.Factory, command.StartAddress, command.DataToWrite);
                    return default(Unit);
                },
                InterpretCommandError(commandUrn),
                RetryPolicy.Create(_settings.TimeoutSettings.Retries),
                (ex, tryNumber, totalRetries) => LogWriteException(ex, tryNumber, totalRetries)(commandUrn, arg),
                _=> failureCount+=1
                );
            return result.IsError switch
            {
                true => (result.Error, new CommunicationDetails(0, failureCount)),
                false => (result.Value, new CommunicationDetails(1, failureCount))
            };
        }

        protected abstract Error InterpretReadError(Exception ex);
        
        private Action<RegistersSegmentsDefinition> LogReadException(Exception exception)
        {
            return (registersSegmentsDefinition)=>Log.Error(exception, "[{@Name}] Reading segment (StartAddress={@StartAddress}, NumberOfRegisters={@NbRegisters}). Message: {@message}",
                Name,
                registersSegmentsDefinition.StartAddress, 
                registersSegmentsDefinition.RegistersToRead,
                exception.ToString());
        }
        
        private Action<Urn, object> LogWriteException(Exception exception, int tryNumber, int totalRetries)
        {
            return (urn, arg) => 
                Log.Error(exception, "[{@Name}] [Try {@Try_Number}/{@Total_TryNumber}] Executing command {@urn}={@arg}. Message: {@message}",
                    Name,
                    tryNumber, totalRetries + 1,
                    urn.Value,arg,
                    exception.ToString());
        }
        IDataModelValue[] MeasureOfCommand(Urn commandUrn, Result<object> arg)
        {
            var measure = _definition.CommandMap.Measure(commandUrn);
            measure.SetData(arg, _clock.Now());
            return measure.ModelValues().Where(mv => !(mv.ModelValue() is NoArg)).ToArray();
        }
        
        private Func<Exception, Error> InterpretCommandError(Urn commandUrn) =>
            ex =>
                CommandExecutionError.Create(DeviceNode,
                    MeasureOfCommand(commandUrn, SlaveCommunicationError.Create(DeviceNode, ex.Message)));

        public uint ReadPaceInSystemTicks => _settings.ReadPaceInSystemTicks;
        public DeviceNode DeviceNode => _definition.DeviceNode;
        public string Name => _settings.Factory;
        public Urn[] SettingsUrns => _definition.SettingsUrns;
    }
}