using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Driver.Common;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.Motors.Controllers.Definitions;
using ImpliciX.Motors.Controllers.Domain;
using ImpliciX.Motors.Controllers.Infrastructure;
using ImpliciX.Motors.Controllers.Settings;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using static ImpliciX.Motors.Controllers.Domain.MotorIds;
using static ImpliciX.Language.Core.SideEffect;
using Debug = ImpliciX.Data.Debug;

namespace ImpliciX.Motors.Controllers.Board
{
    public class MotorsSlave : IBoardSlave
    {
        private readonly IMotorsInfrastructure _motorsInfrastructure;
        private readonly IClock _clock;
        private readonly MotorsDriverSettings _settings;
        private readonly MotorsSlaveDefinition _definition;

        public static Motor[] CreateAllMotorNodes(MotorNode[] nodes) =>
            new [] { M1, M2, M3 }
                .Zip(nodes, (id,node) => new Motor(id,node))
                .ToArray();

        public MotorsSlave(MotorsModuleDefinition motorsModuleDefinition, IMotorsInfrastructure motorsInfrastructure, IClock clock,
            MotorsDriverSettings settings, MotorsSlaveDefinition definition = null)
        {
            _motorsInfrastructure = motorsInfrastructure;
            _clock = clock;
            _settings = settings;
            var allMotors = CreateAllMotorNodes(motorsModuleDefinition.MotorNodes);
            _definition = definition ?? 
                          new MotorsSlaveDefinition(motorsModuleDefinition.MotorsDeviceNode, new RegistersMap(allMotors), new CommandMap(allMotors), 
                new Urn[]{motorsModuleDefinition.MotorsDeviceNode.presence});
            ReadPaceInSystemTicks = (uint) _settings.ReadPaceInSystemTicks;
            DeviceNode = _definition.DeviceNode;
            Name = _settings.Factory;
            SettingsUrns = _definition.SettingsUrn;
        }
        public Result2<IDataModelValue[],CommunicationDetails> ReadProperties(MapKind mapKind)
        {
            Debug.PreCondition(()=>mapKind==MapKind.MainFirmware,()=>$"Map of {mapKind} is not supported by the {nameof(MotorsSlave)}");
                var result = 
                    from motorResponse in ReadMotors()
                    from decodedMotorValues in RegistersMap.Eval(motorResponse.Item1, _clock.Now()).ToResult2(motorResponse.Item2)
                    select decodedMotorValues;
                return result.IsError switch
                    {
                        true => (SlaveCommunicationError.Create(DeviceNode, result.Error.Message), result.Both),
                        false => (result.Value.ToArray(), result.Both)
                    };    
        }

        public Result2<IDataModelValue[], CommunicationDetails> ExecuteCommand(Urn commandUrn, object arg)
        {

            var inputValidationResult = 
                    from rs in SafeCast<RotationalSpeed>(arg)
                    let motorId = CommandMap.MotorIdFrom(commandUrn)
                    select (motorId, rs.Value);
            if (inputValidationResult.IsError) return (InterpretCommandError(commandUrn), new CommunicationDetails(0, 0));

            var (motor, speed) = inputValidationResult.Value;

            var writeResult = _motorsInfrastructure.WriteSimpa(motor, speed);
            if (writeResult.IsError)
            {
                return (InterpretCommandError(commandUrn), writeResult.Both);
            }
 
            return (CommandMap.EvalSuccess(commandUrn, arg, _clock.Now()), writeResult.Both);
        }

        


        private Error InterpretCommandError(Urn commandUrn) =>
                CommandExecutionError.Create(DeviceNode,new IDataModelValue[]{CommandMap.EvalFailed(commandUrn, _clock.Now())});
        
        private Action<Urn, object> LogWriteException(Exception exception, int tryNumber, int totalRetries)
        {
            return (urn, arg) => 
                Log.Error(exception, "[{@Name}] [Try {@Try_Number}/{@Total_TryNumber}] Executing command {@urn}={@arg}. Message: {@message}",
                    Name,
                    tryNumber, totalRetries + 1,
                    urn.Value,arg,
                    exception.Message);
        }
        
        private Result2<Dictionary<MotorIds, MotorResponse>, CommunicationDetails> ReadMotors()
        {
            ushort successComm = 0;
            var motorResponses = new Dictionary<MotorIds, MotorResponse>();
            foreach (var motorId in MotorsUtils.AllMotors())
            {
                var motorResponse = _motorsInfrastructure.ReadSimpa(motorId);
                if (motorResponse.IsError)
                {
                    return (motorResponse.Error, new CommunicationDetails(successComm, 1));
                }
                successComm += motorResponse.Both.SuccessCount;
                motorResponses[motorId] = motorResponse.Value;
            }
            return (motorResponses,new CommunicationDetails(successComm,0));
        }


        private CommandMap CommandMap => _definition.CommandMap;
        private RegistersMap RegistersMap => _definition.RegistersMap;
        public uint ReadPaceInSystemTicks { get; }
        public DeviceNode DeviceNode { get; }
        public string Name { get; }
        
        public Urn[] SettingsUrns { get;}

        public bool IsConcernedByCommandRequested(Urn crUrn) => CommandMap.ContainsUrn(crUrn);
    }
    
 
}