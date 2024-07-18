using System;
using System.Collections.Generic;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Language;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.Motors.Controllers.Board;
using ImpliciX.Motors.Controllers.Definitions;
using ImpliciX.Motors.Controllers.Domain;
using ImpliciX.Motors.Controllers.Infrastructure;
using ImpliciX.Motors.Controllers.Settings;
using ImpliciX.Motors.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.TestsCommon;
using Moq;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Motors.Controllers.Tests.MotorsTestsUtils;

namespace ImpliciX.Motors.Controllers.Tests
{
    [TestFixture]
    public class MotorsSlaveTests
    {
        private static readonly Motor[] allMotors = MotorsSlave.CreateAllMotorNodes(test_model.motors.Nodes);
        private static readonly MotorsModuleDefinition MotorsModuleDefinition = new MotorsModuleDefinition();

        [Test]
        public void read_nominal_case()
        {
            var infrastructureMock = new Mock<IMotorsInfrastructure>();
            infrastructureMock.Setup(m => m.ReadSimpa(It.IsAny<MotorIds>())).Returns((MotorResponse.Create(MotorIds.M1)(CreateRegistersDictionary(MotorReadRegisters.BSP, "+120")),new CommunicationDetails(1,0)));
            
            var sut = new MotorsSlave(MotorsModuleDefinition, infrastructureMock.Object, new StubClock(), MotorSettings, MotorsTestDefinition);
            var result = sut.ReadProperties(MapKind.MainFirmware);
            var expected = new IDataModelValue[]
            {
                Property<RotationalSpeed>.Create(test_model.motors._1.mean_speed.measure, RotationalSpeed.FromFloat(120f).Value, TimeSpan.Zero),
                Property<MeasureStatus>.Create(test_model.motors._1.mean_speed.status, MeasureStatus.Success, TimeSpan.Zero),
            };
            Check.That(result.IsSuccess).IsTrue();
            Check.That(result.Value).ContainsExactly(expected);
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(3,0));
        }
        
        [Test]
        public void read_communication_error_case()
        {
            var infrastructureMock = new Mock<IMotorsInfrastructure>();
            infrastructureMock.Setup(m => m.ReadSimpa(It.IsAny<MotorIds>())).Returns((new SimpaApiError("boom"), new CommunicationDetails(0,1)));

            var sut = new MotorsSlave(MotorsModuleDefinition, infrastructureMock.Object, new StubClock(), MotorSettings, MotorsTestDefinition);
            var result = sut.ReadProperties(MapKind.MainFirmware);
            Check.That(result.IsError).IsTrue();
            Check.That(result.Error).IsInstanceOf<SlaveCommunicationError>();
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(0,1));
        }
        
        
        [Test]
        public void execute_command_nominal_case()
        {
            var infrastructureMock = new Mock<IMotorsInfrastructure>();
            infrastructureMock
                .Setup(m => m.WriteSimpa(It.IsAny<MotorIds>(), It.IsAny<float>()))
                .Returns((MotorIds motorId, float value)=>(MotorResponse.Create(motorId)(CreateRegistersDictionary(MotorReadRegisters.BSP, value.ToString("F")))
                    ,new CommunicationDetails(1,0)));

            var sut = new MotorsSlave(MotorsModuleDefinition, infrastructureMock.Object, new StubClock(), MotorSettings, MotorsTestDefinition);
            
            var result = sut.ExecuteCommand(test_model.motors._1._setpoint.command, RotationalSpeed.FromFloat(120f).Value);
            var expected = new IDataModelValue[]
            {
                Property<RotationalSpeed>.Create(test_model.motors._1._setpoint.measure, RotationalSpeed.FromFloat(120f).Value, TimeSpan.Zero),
                Property<MeasureStatus>.Create(test_model.motors._1._setpoint.status, MeasureStatus.Success, TimeSpan.Zero),
            };
        
            Check.That(result.IsSuccess).IsTrue();
            Check.That(result.Value).ContainsExactly(expected);
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(1,0));
        }
        
        [Test]
        public void execute_command_error_case()
        {
            var infrastructureMock = new Mock<IMotorsInfrastructure>();
            infrastructureMock
                .Setup(m => m.WriteSimpa(It.IsAny<MotorIds>(), It.IsAny<float>()))
                .Returns((new SimpaApiError("boom"),new CommunicationDetails(0,3)));

            var sut = new MotorsSlave(MotorsModuleDefinition, infrastructureMock.Object, new StubClock(), MotorSettings, MotorsTestDefinition);
            var result = sut.ExecuteCommand(test_model.motors._1._setpoint.command, RotationalSpeed.FromFloat(120f).Value);
            
            Check.That(result.IsError).IsTrue();
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(0,3));
        }
        
      
        private MotorsSlaveDefinition MotorsTestDefinition = new MotorsSlaveDefinition(
            test_model.software.fake_motor_board,
            new RegistersMap(allMotors, new List<ReadDefinition>()
            {
                new ReadDefinition()
                {
                    motorId = MotorIds.M1,
                    register = MotorReadRegisters.BSP,
                    decodeFunction = (s, at) => new IDataModelValue[]
                    {
                        Property<RotationalSpeed>.Create(test_model.motors._1.mean_speed.measure, RotationalSpeed.FromFloat(120f).Value, TimeSpan.Zero),
                        Property<MeasureStatus>.Create(test_model.motors._1.mean_speed.status, MeasureStatus.Success, TimeSpan.Zero),
                    },
                    statusUrn = test_model.motors._1.mean_speed.status
                },
            }), new CommandMap(allMotors), new []
            {
                test_model.software.fake_motor_board.presence
            });

        private readonly MotorsDriverSettings MotorSettings = new MotorsDriverSettings()
        {
            ReadPaceInSystemTicks = 1,
            TimeoutSettings = new TimeoutSettings()
            {
                Retries = 0, Timeout = 50
            }
        };
        
        private MotorsDriverSettings MotorSettingsWithRetries(int n) => new MotorsDriverSettings()
        {
            ReadPaceInSystemTicks = 1,
            TimeoutSettings = new TimeoutSettings()
            {
                Retries = n, Timeout = 50
            }
        };
    }
}