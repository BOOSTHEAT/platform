using System;
using System.Collections.Generic;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.Tests.Doubles;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.FakeDecoders;

namespace ImpliciX.RTUModbus.Controllers.Tests.SlaveBase
{
    [TestFixture]
    public class CommunicationCountersTests
    {
        [SetUp]
        public void Init()
        {
            RegistersMap.Factory = () => new RegistersMapImpl();
            Language.Modbus.CommandMap.Factory = () => new CommandMapImpl();
        }

        [Test]
        public void read_with_no_communication_error()
        {
            IModbusAdapter modbusAdapter = new DummyModbusAdapter(
                new (ushort startAddress, object simulatedOutcome)[]
                {
                    (0, new ushort[] {0, 17096}),
                    (5, new ushort[]{0,17056, 0, 17042}),
                }
            );
            var slave = new BasicModbusSlave(SlaveDefinition, ModbusSlaveSettings, modbusAdapter, Clock, new DriverStateKeeper());
            var result = slave.ReadProperties(MapKind.MainFirmware);
            Check.That(result.IsSuccess).IsTrue();
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(2,0));
        }

        [Test]
        public void read_with_communication_error_on_first_segment()
        {
            IModbusAdapter modbusAdapter = new DummyModbusAdapter(
                new (ushort startAddress, object simulatedOutcome)[]
                {
                    (0,new Exception("boom")),
                    (5, new ushort[]{0,17056, 0, 17042}),
                }
            );

            var slave = new BasicModbusSlave(SlaveDefinition, ModbusSlaveSettings, modbusAdapter, Clock, new DriverStateKeeper());
            var result = slave.ReadProperties(MapKind.MainFirmware);

            Check.That(result.IsError).IsTrue();
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(0,1));
        }
        
        [Test]
        public void read_with_communication_error_on_second_segment()
        {
            IModbusAdapter modbusAdapter = new DummyModbusAdapter(
                new (ushort startAddress, object simulatedOutcome)[]
                {
                    (0,new ushort[]{0,17056}),
                    (5, new Exception("boom")),
                }
            );

            var slave = new BasicModbusSlave(SlaveDefinition, ModbusSlaveSettings, modbusAdapter, Clock, new DriverStateKeeper());
            var result = slave.ReadProperties(MapKind.MainFirmware);

            Check.That(result.IsError).IsTrue();
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(1,1));
        }
        
        [Test]
        public void execute_command_with_no_communication_errors()
        {
            DummyModbusAdapter modbusAdapter = new DummyModbusAdapter(
                writeSimulations: new (ushort startAddress, object simulatedOutcome)[]
                {
                    (101, default(Unit)),
                }
            );

            var slave = new BasicModbusSlave(SlaveDefinition, ModbusSlaveSettings, modbusAdapter, Clock, new DriverStateKeeper());
            var result = slave.ExecuteCommand(fake_urn._switch, Position.A);

            Check.That(result.IsSuccess).IsTrue();
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(1,0));
        }
        
        [Test]
        public void execute_command_with_communication_errors()
        {
            DummyModbusAdapter modbusAdapter = new DummyModbusAdapter(
                writeSimulations: new (ushort startAddress, object simulatedOutcome)[]
                {
                    (101, new Exception("boom")),
                }
            );
            var slave = new BasicModbusSlave(SlaveDefinition, ModbusSlaveSettings3Retries, modbusAdapter, Clock, new DriverStateKeeper());
            var result = slave.ExecuteCommand(fake_urn._switch, Position.A);

            Check.That(result.IsError).IsTrue();
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(0,4));
        }

        
        private static IClock Clock = new StubClock();
        private static HardwareAndSoftwareDeviceNode _deviceNode = fake_urn.mcu_fake1;

        public static ModbusSlaveDefinition SlaveDefinition { get; } = new ModbusSlaveDefinition(_deviceNode, SlaveKind.Vendor)
        {
            Name = "",
            SettingsUrns = Array.Empty<Urn>(),
            ReadPropertiesMaps = new Dictionary<MapKind, IRegistersMap>()
            {
                [MapKind.MainFirmware] =
                    RegistersMap.Create()
                        .RegistersSegmentsDefinitions(
                            new RegistersSegmentsDefinition(RegisterKind.Holding) {StartAddress = 0, RegistersToRead = 2},
                            new RegistersSegmentsDefinition(RegisterKind.Holding) {StartAddress = 5, RegistersToRead = 2})
                        .For(fake_urn.temperature1).DecodeRegisters(0, 2, ProbeDecode(it => it, Temperature.FromFloat))
                        .For(fake_urn.temperature2).DecodeRegisters(5, 2, ProbeDecode(it => it, Temperature.FromFloat))
            }, 
            CommandMap = CommandMap
        };

        public static ICommandMap CommandMap {
            get
            {
                var commandMap = Language.Modbus.CommandMap.Empty();
                var actuator = new FakeActuator(101);
                commandMap.Add(fake_urn._switch,(CommandActuator) actuator.SwitchToStateless);
                return commandMap;
            }
        }
        
        private static ModbusSlaveSettings ModbusSlaveSettings3Retries
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
    }
}