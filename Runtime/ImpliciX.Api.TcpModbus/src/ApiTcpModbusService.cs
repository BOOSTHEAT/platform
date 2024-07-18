using System;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Api.TcpModbus
{
    public class ApiTcpModbusService : IDisposable
    {
        public DomainEvent[] HandlePropertiesChanged(PropertiesChanged @event)
        {
            foreach (var modelValue in @event.ModelValues)
            {
                if (modelValue.Urn.Equals(ModbusMapping.Presence))
                {
                    PresenceChanged((Presence)modelValue.ModelValue());
                }
                else if (modelValue.Urn is PropertyUrn<AlarmState>)
                {
                    WriteAlarm(modelValue);
                }
                else
                {
                    WriteMeasure(modelValue);
                }
            }
            return Array.Empty<DomainEvent>();
        }

        private void WriteMeasure(IDataModelValue modelValue)
        {
            if (ModbusMapping.MeasuresMap.TryGetValue(modelValue.Urn, out var register))
                ModbusAdapter.WriteInHoldingRegister(register,ModbusMapping.ModelValueToRegisters(modelValue.ModelValue()));
        }
        
        private void WriteAlarm(IDataModelValue modelValue)
        {
             if (ModbusMapping.AlarmsMap.TryGetValue(modelValue.Urn, out var register))
                 ModbusAdapter.WriteInDiscreteInputs(register, new[] {modelValue.ModelValue() is AlarmState.Active});
        }

        private void PresenceChanged(object presence)
        {
            switch (presence)
            {
                case Presence.Enabled:
                    ModbusAdapter.Start();
                    break;
                case Presence.Disabled:
                    ModbusAdapter.Stop();
                    break;
            }
        }

        public void Dispose()
        {
            ModbusAdapter?.Dispose();
        }

        public bool CanHandle(PropertiesChanged trigger)
        {
            return ModbusMapping.AllPropertiesUrns.Intersect(trigger.PropertiesUrns).Any();
        }
        
        public ModbusMapping ModbusMapping { get; }
        private IModbusTcpSlaveAdapter ModbusAdapter { get; }

        private readonly ModbusPublisher Publisher;

        public ApiTcpModbusService(
            ModbusMapping modbusMapping, 
            IModbusTcpSlaveAdapter modbusAdapter
            , Func<TimeSpan> Clock
            )
        {
            ModbusMapping = modbusMapping;
            ModbusAdapter = modbusAdapter;
            Publisher = new ModbusPublisher(ModbusAdapter, Clock);
        }

        public DomainEvent[] HandleSystemTicked(SystemTicked trigger) => Publisher.PullDomainEvents(ModbusMapping);
    }
   
}