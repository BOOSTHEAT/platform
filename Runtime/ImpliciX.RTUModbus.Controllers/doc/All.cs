using System;
using System.Collections.Generic;
using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.DocTools;

namespace ImpliciX.RTUModbus.Controllers.Doc
{
  public class All
  {
    public IEnumerable<FSMViewModel> FSMs
    {
      get
      {
        RegistersMap.Factory = () => new RegistersMapImpl();
        CommandMap.Factory = () => new CommandMapImpl();
        var root = new RootModelNode("root");
        var clock = new VirtualClock(DateTime.Now);
        var domainEventFactory = EventFactory.Create(new ModelFactory(Assembly.GetExecutingAssembly()), clock.Now);
        var driverStateKeeper = new DriverStateKeeper();
        var device = new HardwareAndSoftwareDeviceNode("sw",root);
        var slaveModel = new ModbusSlaveModel();
        var settings = new ModbusSlaveSettings();
        
        return new FSMViewModel[] {
          
          new FSMViewModel<BHBoard.State>(
            BHBoard.Fsm.Create(
              new BHBoard.Slave(
                new ModbusSlaveDefinition(device, SlaveKind.BH)
                {
                  Name = "",
                  SettingsUrns = Array.Empty<Urn>(),
                },
                slaveModel, settings, null, clock, driverStateKeeper),
              slaveModel,
              domainEventFactory,
              new FirmwareUpdateContext(device))
          ),
          new FSMViewModel<BrahmaBoard.State>(
            BrahmaBoard.Fsm.Create(
              new BrahmaBoard.Slave(
                new BrahmaSlaveDefinition(device, new BurnerNode("burner",root))
                {
                  Name = "",
                  SettingsUrns = Array.Empty<Urn>(),
                },
                settings, null, clock, driverStateKeeper),
              domainEventFactory)
          ),
          new FSMViewModel<VendorBoard.State>(
            VendorBoard.Fsm.Create(
              new VendorBoard.Slave(
                new ModbusSlaveDefinition(device, SlaveKind.Vendor)
                {
                  Name = "",
                  SettingsUrns = Array.Empty<Urn>(),
                },
                settings, null, clock, driverStateKeeper),
              domainEventFactory)
          )
          
        };
      }
    }
  }
}