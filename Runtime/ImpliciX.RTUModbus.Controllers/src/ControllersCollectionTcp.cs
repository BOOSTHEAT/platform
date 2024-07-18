using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.RTUModbus.Controllers.Infrastructure;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using static ImpliciX.Language.Core.SideEffect;

namespace ImpliciX.RTUModbus.Controllers
{
    public class TcpControllersCollection : IControllersCollection
    {
        private readonly IClock _clock;
        private readonly DomainEventFactory _domainEventFactory;
        private readonly DriverStateKeeper _driverStateKeeper;

        public static TcpControllersCollection Create(string id, ModbusSettings modbusSettings,
            Func<ModbusSlaveDefinition>[] slaveDefinition, ModbusSlaveModel slaveModel,
            IClock clock, DomainEventFactory domainEventFactory, DriverStateKeeper driverStateKeeper)
        {
            return new TcpControllersCollection(id, modbusSettings, slaveDefinition, slaveModel, clock, domainEventFactory, driverStateKeeper);
        }


        public static TcpControllersCollection Create(ISlaveController[] controllers)
        {
            return new TcpControllersCollection(string.Empty, controllers);
        }


        private TcpControllersCollection(string id, ISlaveController[] controllers)
        {
            _id = id;
            _controllers = controllers;
        }

        private TcpControllersCollection(string id, ModbusSettings modbusSettings,
            Func<ModbusSlaveDefinition>[] slaveDefinition, ModbusSlaveModel slaveModel,
            IClock clock, DomainEventFactory domainEventFactory,
            DriverStateKeeper driverStateKeeper)
        {
            _id = id;
            _clock = clock;
            _domainEventFactory = domainEventFactory;
            _driverStateKeeper = driverStateKeeper;
            RegistersMap.Factory = () => new RegistersMapImpl();
            CommandMap.Factory = () => new CommandMapImpl();
            var definitions = slaveDefinition
                .Select(x => x())
                .ToDictionary(x => x.Name, x => x);
            _controllers = modbusSettings.Slaves.Select(slaveSettings =>
            {
                Debug.PreCondition(() => definitions.ContainsKey(slaveSettings.Factory), () => $"No definition found for slave {slaveSettings.Factory}");
                return CreateController(slaveSettings, modbusSettings.TcpSettings, definitions[slaveSettings.Factory], slaveModel);
            }).ToArray();
        }

        public DomainEvent[] Activate() => _controllers.SelectMany(c => c.Activate()).ToArray();

        public DomainEvent[] HandleDomainEvent(DomainEvent trigger) =>
            _controllers
                .Where(c => TryRunOrDefault(() => c.CanHandle(trigger), _ => false).GetValueOrDefault())
                .SelectMany(c =>
                {
                    return TryRunOrDefault(
                            () => c.HandleDomainEvent(trigger),
                            _ => Array.Empty<DomainEvent>())
                        .GetValueOrDefault();
                }).ToArray();

        public bool CanHandle(DomainEvent arg) => _controllers.Any(controller => controller.CanHandle(arg));

        private ISlaveController CreateController(ModbusSlaveSettings slaveSettings, TcpSettings tcpSettings, ModbusSlaveDefinition definition,
            ModbusSlaveModel slaveModel)
        {
            var modbusAdapter = ModbusAdapterTcp.Create(tcpSettings, slaveSettings);
            switch (definition.SlaveKind)
            {
                case SlaveKind.BH:
                    var bhslave = new BHBoard.Slave(definition, slaveModel, slaveSettings, modbusAdapter, _clock, _driverStateKeeper);
                    return new BHBoard.Controller(bhslave, slaveModel, new FirmwareUpdateContext(bhslave.DeviceNode), _domainEventFactory, _driverStateKeeper);
                case SlaveKind.Brahma:
                    var brahmaSlave = new BrahmaBoard.Slave((BrahmaSlaveDefinition) definition, slaveSettings, modbusAdapter, _clock, _driverStateKeeper);
                    return new BrahmaBoard.Controller(brahmaSlave, _domainEventFactory, _driverStateKeeper);
                case SlaveKind.Vendor:
                    var vendorSlave = new VendorBoard.Slave(definition, slaveSettings, modbusAdapter, _clock, _driverStateKeeper);
                    return new VendorBoard.Controller(vendorSlave, _domainEventFactory, _driverStateKeeper);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public IEnumerator<ISlaveController> GetEnumerator()
        {
            return ((IEnumerable<ISlaveController>) _controllers).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _controllers.GetEnumerator();
        }

        private readonly ISlaveController[] _controllers;
        private readonly string _id;
    }
}