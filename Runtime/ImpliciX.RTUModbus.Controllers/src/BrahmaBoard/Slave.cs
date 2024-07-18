using System;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Core;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Clock;
using NModbus;

namespace ImpliciX.RTUModbus.Controllers.BrahmaBoard
{
    public class Slave : ModbusSlave, IBrahmaBoardSlave
    {
        public Slave(BrahmaSlaveDefinition definition, ModbusSlaveSettings settings, IModbusAdapter modbusAdapter, IClock clock, DriverStateKeeper driverStateKeeper) : base(definition, settings, modbusAdapter, clock, driverStateKeeper)
        {
            GenericBurner = definition.GenericBurner;
        }
        protected override Error InterpretReadError(Exception ex) =>
            ex switch
            {
                SlaveException _ => ReadProtocolError.Create(DeviceNode, ex.Message),
                _ => SlaveCommunicationError.Create(DeviceNode, ex.Message)
            };

        public BurnerNode GenericBurner { get; }
    }
}