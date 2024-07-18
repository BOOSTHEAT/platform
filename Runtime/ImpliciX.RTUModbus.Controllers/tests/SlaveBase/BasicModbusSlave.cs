using System;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Core;
using ImpliciX.Language.Modbus;
using ImpliciX.SharedKernel.Clock;

namespace ImpliciX.RTUModbus.Controllers.Tests.SlaveBase
{
    public class BasicModbusSlave : ModbusSlave
    {
        public BasicModbusSlave(ModbusSlaveDefinition definition, ModbusSlaveSettings settings, IModbusAdapter modbusAdapter, IClock clock, DriverStateKeeper driverStateKeeper) : base(definition, settings, modbusAdapter, clock, driverStateKeeper)
        {
        }

        protected override Error InterpretReadError(Exception ex)
        {
            return SlaveCommunicationError.Create(DeviceNode);
        }

        
    }
}