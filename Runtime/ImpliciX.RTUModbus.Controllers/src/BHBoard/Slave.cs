using System;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Clock;
using NModbus;

namespace ImpliciX.RTUModbus.Controllers.BHBoard
{
    public class Slave : ModbusSlave
    {
        public Slave(ModbusSlaveDefinition definition, ModbusSlaveModel slaveModel, ModbusSlaveSettings settings,
            IModbusAdapter modbusAdapter, IClock clock, DriverStateKeeper driverStateKeeper) : base(definition, settings, modbusAdapter, clock, driverStateKeeper)
        {
            _slaveModel = slaveModel;
        }
        
      
        protected override Error InterpretReadError(Exception ex) =>
            ex switch
            {
                SlaveException _ => ReadProtocolError.Create(DeviceNode, ex.Message),
                _ => SlaveCommunicationError.Create(DeviceNode, ex.Message)
            };
        public override bool IsConcernedByCommandRequested(Urn crUrn) =>
            base.IsConcernedByCommandRequested(crUrn)
            || DeviceNode.Urn.IsPartOf(crUrn)
            || (crUrn.Equals(_slaveModel.Commit))
            || (crUrn.Equals(_slaveModel.Rollback));

        private ModbusSlaveModel _slaveModel;
    }
}