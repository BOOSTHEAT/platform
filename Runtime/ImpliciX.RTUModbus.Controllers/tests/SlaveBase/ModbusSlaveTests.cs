using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.Tests.Doubles;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.RTUModbus.Controllers.Tests.SlaveBase
{
    [TestFixture]
    public class ModbusSlaveTests
    {
        [SetUp]
        public void Init()
        {
            RegistersMap.Factory = () => new RegistersMapImpl();
            CommandMap.Factory = () => new CommandMapImpl();
        }
        
        [Test]
        public void read_properties_nominal_case_with_successful_and_failed_decoded_values()
        {
            IModbusAdapter modbusAdapter = new DummyModbusAdapter(
                new (ushort startAddress, object simulatedOutcome)[] {
                (0, new ushort[]{0,17096}),
                (5, new ushort[]{0,17056, 0, 17042}),
                (9, new ushort[]{0})
            }
             ); 
             
            var mainFirmwarePropertiesMap = 
                RegistersMap.Create()
                    .RegistersSegmentsDefinitions(
                        new RegistersSegmentsDefinition(RegisterKind.Holding){StartAddress = 0, RegistersToRead = 2},
                        new RegistersSegmentsDefinition(RegisterKind.Holding){StartAddress = 5, RegistersToRead = 4})
                    .For(fake_urn.temperature1)
                        .DecodeRegisters(0, 2, FakeDecoders.ProbeDecode(it => it, Temperature.FromFloat))
                    .For(fake_urn.temperature2)
                        .DecodeRegisters(5, 2, FakeDecoders.ProbeDecode(it => it, Temperature.FromFloat))
                    .For(fake_urn.pressure1)
                        .DecodeRegisters(7,2, FakeDecoders.SimulateErrorDecode<Pressure>());

            var slaveDefinition = new ModbusSlaveDefinition(_deviceNode, SlaveKind.Vendor)
            {
                Name = "",
                SettingsUrns = Array.Empty<Urn>(),
                ReadPropertiesMaps = new Dictionary<MapKind, IRegistersMap>()
                {
                    [MapKind.MainFirmware] = mainFirmwarePropertiesMap
                }
            };

            var slave = new BasicModbusSlave(slaveDefinition, ModbusSlaveSettings, modbusAdapter, Clock, new DriverStateKeeper());
            var result = slave.ReadProperties(MapKind.MainFirmware);
            
            var expected = new IDataModelValue[]
            {
                Property<Temperature>.Create(fake_urn.temperature1.measure, Temperature.Create(100f), TimeSpan.Zero),
                Property<MeasureStatus>.Create(fake_urn.temperature1.status, MeasureStatus.Success, TimeSpan.Zero),
                Property<Temperature>.Create(fake_urn.temperature2.measure, Temperature.Create(80f), TimeSpan.Zero),
                Property<MeasureStatus>.Create(fake_urn.temperature2.status, MeasureStatus.Success, TimeSpan.Zero),
                Property<MeasureStatus>.Create(fake_urn.pressure1.status, MeasureStatus.Failure, TimeSpan.Zero)
            };
            Check.That(result.IsSuccess).IsTrue();
            Check.That(result.Value).ContainsExactly(expected);
        }

        [Test]
        public void read_properties_for_an_undefined_map()
        {
            IModbusAdapter modbusAdapter = new DummyModbusAdapter(
                new (ushort startAddress, object simulatedOutcome)[] {
                    (0, new ushort[]{0}),
                }
            );
            var slaveDefinition = new ModbusSlaveDefinition(_deviceNode, SlaveKind.Vendor)
            {
                Name = "",
                SettingsUrns = Array.Empty<Urn>(),
                ReadPropertiesMaps = new Dictionary<MapKind, IRegistersMap>()
                {
                    [MapKind.MainFirmware] = RegistersMap.Empty()
                }
            };

            var slave = new BasicModbusSlave(slaveDefinition, ModbusSlaveSettings, modbusAdapter, Clock, new DriverStateKeeper());
            var result = slave.ReadProperties(MapKind.Common);
            Check.That(result.IsSuccess).IsTrue();
            Check.That(result.Value).IsEmpty();
        }
        
        [Test]
        public void execute_command_nominal_case_noarg()
        {
            DummyModbusAdapter modbusAdapter = new DummyModbusAdapter(
                writeSimulations: new (ushort startAddress, object simulatedOutcome)[]
                {
                    (101, default(Unit)),
                }
            );
            var commandMap = CommandMap.Empty();
            commandMap.Add(fake_urn._smurf, (CommandActuator) ((_, _, _) => Result<Command>.Create(Command.Create(101, new ushort[] { 42 }))));
            var slaveDefinition = new ModbusSlaveDefinition(_deviceNode, SlaveKind.Vendor)
            {
                Name = "",
                SettingsUrns = Array.Empty<Urn>(),
                CommandMap = commandMap
            };
            var driverStateKeeper = new DriverStateKeeper();
            var slave = new BasicModbusSlave(slaveDefinition, ModbusSlaveSettings, modbusAdapter, Clock, driverStateKeeper);
            var result = slave.ExecuteCommand(fake_urn._smurf, new NoArg());
            var expectedValues = new IDataModelValue[]
            {
                Property<MeasureStatus>.Create(fake_urn._smurf.status, MeasureStatus.Success, TimeSpan.Zero),
            };
            Check.That(result.IsSuccess);
            Check.That(result.Value).ContainsExactly(expectedValues);
            Check.That(modbusAdapter.WriteCount).IsEqualTo(1);
            Check.That(modbusAdapter.Writes.First().registersToWrite).IsEqualTo(new ushort[] { 42 });
        }
        
        [Test]
        public void execute_stateless_command_nominal_case()
        {
            DummyModbusAdapter modbusAdapter = new DummyModbusAdapter(
                writeSimulations: new (ushort startAddress, object simulatedOutcome)[]
                {
                    (101, default(Unit)),
                }
            );
            var commandMap = CommandMap.Empty();
            var actuator = new FakeActuator(101);
            commandMap.Add(fake_urn._switch,(CommandActuator) actuator.SwitchToStateless);
            var slaveDefinition = new ModbusSlaveDefinition(_deviceNode, SlaveKind.Vendor)
            {
                Name = "",
                SettingsUrns = Array.Empty<Urn>(),
                CommandMap = commandMap
            };
            var driverStateKeeper = new DriverStateKeeper();
            var slave = new BasicModbusSlave(slaveDefinition, ModbusSlaveSettings, modbusAdapter, Clock, driverStateKeeper);
            var result = slave.ExecuteCommand(fake_urn._switch, Position.A);
            var expectedValues = new IDataModelValue[]
            {
                Property<Position>.Create(fake_urn._switch.measure, Position.A, TimeSpan.Zero),
                Property<MeasureStatus>.Create(fake_urn._switch.status, MeasureStatus.Success, TimeSpan.Zero),
            };
            Check.That(result.IsSuccess);
            Check.That(result.Value).ContainsExactly(expectedValues);
            Check.That(modbusAdapter.WriteCount).IsEqualTo(1);
        }
        
        [Test]
        public void execute_stateful_command_nominal_case()
        {
            DummyModbusAdapter modbusAdapter = new DummyModbusAdapter(
                writeSimulations: new (ushort startAddress, object simulatedOutcome)[]
                {
                    (101, default(Unit)),
                }
            );
            var commandMap = CommandMap.Empty();
            var actuator = new FakeActuator(101);
            commandMap.Add(fake_urn._switch,(CommandActuator) actuator.SwitchToStateful);
            var slaveDefinition = new ModbusSlaveDefinition(_deviceNode, SlaveKind.Vendor)
            {
                Name = "",
                SettingsUrns = Array.Empty<Urn>(),
                CommandMap = commandMap
            };
            var driverStateKeeper = new DriverStateKeeper();
            var slave = new BasicModbusSlave(slaveDefinition, ModbusSlaveSettings, modbusAdapter, Clock,driverStateKeeper);
            var result = slave.ExecuteCommand(fake_urn._switch, Position.B);
            var expectedValues = new IDataModelValue[]
            {
                Property<Position>.Create(fake_urn._switch.measure, Position.B, TimeSpan.Zero),
                Property<MeasureStatus>.Create(fake_urn._switch.status, MeasureStatus.Success, TimeSpan.Zero),
            };
            Check.That(result.IsSuccess);
            Check.That(result.Value).ContainsExactly(expectedValues);
            Check.That(driverStateKeeper.Read(fake_urn._switch.command).GetValueOrDefault<Position?>("last_position",null))
                .IsEqualTo(Result<Position?>.Create(Position.B));
            Check.That(modbusAdapter.WriteCount).IsEqualTo(1);
        }

        [Test]
        public void execute_command_with_failure()
        {
            IModbusAdapter modbusAdapter = new DummyModbusAdapter(
                writeSimulations: new (ushort startAddress, object simulatedOutcome)[]
                {
                    (101, new Exception()),
                }
            );
            var commandMap = CommandMap.Empty();
            var actuator = new FakeActuator(101);
            commandMap.Add(fake_urn._switch,(CommandActuator) actuator.SwitchToStateless);
            var slaveDefinition = new ModbusSlaveDefinition(_deviceNode, SlaveKind.Vendor)
            {
                Name = "",
                SettingsUrns = Array.Empty<Urn>(),
                CommandMap = commandMap
            };
            
            var slave = new BasicModbusSlave(slaveDefinition, ModbusSlaveSettings, modbusAdapter, Clock, new DriverStateKeeper());
            var result = slave.ExecuteCommand(fake_urn._switch, Position.A);
            var expectedError = CommandExecutionError.Create(slave.DeviceNode, new IDataModelValue[]
            {
                Property<MeasureStatus>.Create(fake_urn._switch.status, MeasureStatus.Failure, TimeSpan.Zero)
            });
            Check.That(result.IsError).IsTrue();
            Check.That(result.Error).IsInstanceOf<CommandExecutionError>();
            Check.That(((CommandExecutionError)result.Error).ErrorProperties).ContainsExactly(expectedError.ErrorProperties);
        }

        [Test]
        public void should_perform_retries_execute_command_with_failure()
        {
            DummyModbusAdapter modbusAdapter = new DummyModbusAdapter(
                writeSimulations: new (ushort startAddress, object simulatedOutcome)[]
                {
                    (101, new Exception()),
                }
            );
            var commandMap = CommandMap.Empty();
            var actuator = new FakeActuator(101);
            commandMap.Add(fake_urn._switch,(CommandActuator)  actuator.SwitchToStateless);
            var slaveDefinition = new ModbusSlaveDefinition(_deviceNode, SlaveKind.Vendor)
            {
                Name = "",
                SettingsUrns = Array.Empty<Urn>(),
                CommandMap = commandMap
            };
            
            var slave = new BasicModbusSlave(slaveDefinition, ModbusSlaveSettingsWith3Retries, modbusAdapter, Clock, new DriverStateKeeper());
            var result = slave.ExecuteCommand(fake_urn._switch, Position.A);
           
            Check.That(result.IsError).IsTrue();
            Check.That(result.Error).IsInstanceOf<CommandExecutionError>();
            Check.That(modbusAdapter.WriteCount).IsEqualTo(4);
        }
        
        private static IClock Clock = new StubClock();
        
        private static HardwareAndSoftwareDeviceNode _deviceNode = fake_urn.mcu_fake1;
        
        private static ModbusSlaveSettings ModbusSlaveSettings
        {
            get
            {
                ModbusSlaveSettings slaveSettings = new ModbusSlaveSettings()
                {
                    Id = 42,
                    ReadPaceInSystemTicks = 1,
                    Factory = "test",
                    TimeoutSettings = new TimeoutSettings()
                    {
                        Retries = 0,
                        Timeout = 50,
                    }
                };
                return slaveSettings;
            }
        }
        
        private static ModbusSlaveSettings ModbusSlaveSettingsWith3Retries
        {
            get
            {
                ModbusSlaveSettings slaveSettings = new ModbusSlaveSettings()
                {
                    Id = 42,
                    ReadPaceInSystemTicks = 1,
                    Factory = "test",
                    TimeoutSettings = new TimeoutSettings()
                    {
                        Retries = 3,
                        Timeout = 50,
                    }
                };
                return slaveSettings;
            }
        }
    }
}