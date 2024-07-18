using System;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RTUModbus.Controllers.VendorBoard;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.RTUModbus.Controllers.VendorBoard.Controller, ImpliciX.RTUModbus.Controllers.VendorBoard.State>;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.RTUModbus.Controllers.Tests.VendorBoard
{
    [TestFixture]
    public class KeepSettingsPropertiesChanged
    {
        [Test]
        public void should_keep_properties_changed_in_slave_settings_urn()
        {
            var stateKeeper = new DriverStateKeeper();
            var slaveController =
               DefineControllerInState(State.Regulation)
                   .ForSimulatedSlave(test_model.software.fake_daughter_board)
                   .WithStateKeeper(stateKeeper)
                   .WithSettingsUrns(new Urn[]
                   {
                       test_model.software.fake_daughter_board.presence
                   }) 
                   .BuildSlaveController();
           
           var presenceEnabled = EventPropertyChanged(test_model.software.fake_daughter_board.presence, Presence.Enabled, TimeSpan.Zero);

           slaveController.HandleDomainEvent(presenceEnabled);

           Check.That(stateKeeper.Read(test_model.software.fake_daughter_board.presence).GetValue<Presence>("value"))
               .IsEqualTo(Result<Presence>.Create(Presence.Enabled));
        } 
    }
}