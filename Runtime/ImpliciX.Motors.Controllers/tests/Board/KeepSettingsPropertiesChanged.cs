using System;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Model;
using ImpliciX.Motors.Controllers.Tests.Model;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Motors.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.Motors.Controllers.Board.SlaveController, ImpliciX.Motors.Controllers.Board.State>;
using static ImpliciX.Motors.Controllers.Board.State;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Motors.Controllers.Tests.Board
{
    [TestFixture]
    public class KeepSettingsPropertiesChanged
    {
        [Test]
        public void should_keep_properties_changed_in_slave_settings_urn()
        {
            var stateKeeper = new DriverStateKeeper();
            var slaveController =
               DefineControllerInState(Started)
                   .ForSimulatedSlave(test_model.software.fake_motor_board)
                   .WithStateKeeper(stateKeeper)
                   .WithSettingsUrns(new Urn[]
                   {
                       test_model.software.fake_motor_board.software_version.measure
                   }) 
                   .BuildSlaveController();
           
           var settingUrn = EventPropertyChanged(test_model.software.fake_motor_board.software_version.measure, "1.2.3.4", TimeSpan.Zero);

           slaveController.HandleDomainEvent(settingUrn);

           Check.That(stateKeeper.Read(test_model.software.fake_motor_board.software_version.measure).GetValue<SoftwareVersion>("value").Value.ToString())
               .IsEqualTo("1.2.3.4");
        } 
    }
}