using System;
using System.Collections.Generic;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BHBoard;
using ImpliciX.RTUModbus.Controllers.Tests.Doubles;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.TestsCommon;
using NFluent;
using NModbus;
using NUnit.Framework;

namespace ImpliciX.RTUModbus.Controllers.Tests.Slaves
{
    [TestFixture]
    public class BHBoardSlaveTests
    {
        [SetUp]
        public void Init()
        {
            RegistersMap.Factory = () => new RegistersMapImpl();
            CommandMap.Factory = () => new CommandMapImpl();
        }
        
        [TestCase(typeof(SlaveException), typeof(ReadProtocolError))]
        [TestCase(typeof(TimeoutException), typeof(SlaveCommunicationError))]
        [TestCase(typeof(InvalidOperationException),typeof(SlaveCommunicationError))]
        [TestCase(typeof(Exception),typeof(SlaveCommunicationError))]
        public void should_catch_exceptions_when_reading_and_return_errors(Type exceptionType, Type expectedErrorType)
        {
            RegistersSegmentsDefinition[] registersSegmentsDefinitions = new[]
            {
                new RegistersSegmentsDefinition(RegisterKind.Holding){StartAddress = 0, RegistersToRead = 2},
            };
            
            IModbusAdapter modbusAdapter = new DummyModbusAdapter(
                new (ushort startAddress, object simulatedOutcome)[] {
                    (0, Activator.CreateInstance(exceptionType,new object[]{"boom"})),
                }
            );

            var allPropertiesMap =
                RegistersMap.Create()
                    .RegistersSegmentsDefinitions(new RegistersSegmentsDefinition(RegisterKind.Holding){StartAddress = 0, RegistersToRead = 2})
                    .For(fake_urn.temperature1)
                    .DecodeRegisters(0, 2, FakeDecoders.ProbeDecode(it => it, Temperature.FromFloat));

            var slaveDefinition = new ModbusSlaveDefinition(_deviceNode, SlaveKind.BH)
            {
                Name = "",
                SettingsUrns = Array.Empty<Urn>(),
                ReadPropertiesMaps = new Dictionary<MapKind, IRegistersMap>()
                {
                    [MapKind.MainFirmware] = allPropertiesMap
                }
            };
            var slave = new Slave(slaveDefinition, new ModbusSlaveModel(), ModbusSlaveSettings, modbusAdapter, Clock, new DriverStateKeeper());

            var result = slave.ReadProperties(MapKind.MainFirmware);
            Check.That(result.IsError).IsTrue();
            Check.That(result.Error.GetType()).Equals(expectedErrorType);
        }

        [TestCase("fake_urn:mcu_fake1:DO_IT", true)]
        [TestCase("fake_urn:SWITCH", true)]
        [TestCase("test_model:COMMIT_UPDATE", true)]
        [TestCase("test_model:ROLLBACK_UPDATE", true)]
        [TestCase("fake_urn:THROTTLE", false)]
        public void is_concerned_by_command(string urn, bool expected)
        {
            var commandMap = CommandMap.Empty();
            var actuator = new FakeActuator(101);
            commandMap.Add(fake_urn._switch, (CommandActuator) actuator.SwitchToStateless);
            var slaveDefinition = new ModbusSlaveDefinition(_deviceNode, SlaveKind.BH)
            {
                Name = "",
                SettingsUrns = Array.Empty<Urn>(),
                CommandMap = commandMap
            };

            var slaveModel = new ModbusSlaveModel
                {Commit = test_model._commit_update, Rollback = test_model._rollback_update};
            var slave = new Slave(slaveDefinition, slaveModel, ModbusSlaveSettings, default, Clock, new DriverStateKeeper());

            Check.That(slave.IsConcernedByCommandRequested(urn)).IsEqualTo(expected);
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
    }
}