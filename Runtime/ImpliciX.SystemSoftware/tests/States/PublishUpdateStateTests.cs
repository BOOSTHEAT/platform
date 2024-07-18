using System;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SystemSoftware.States;
using NFluent;
using NUnit.Framework;
using static ImpliciX.SystemSoftware.Tests.States.StatesHelper;

namespace ImpliciX.SystemSoftware.Tests.States
{
    public class PublishUpdateStateTests
    {
        [Test]
        public void should_publish_initial_state()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, (location, softwareDeviceMap) => default, DomainEventFactory(), typeof(Starting));
            var resultingEvents = runner.Activate();
            var expected = PropertiesChanged.Create(Model().UpdateState, UpdateState.Starting, TimeSpan.Zero);
            Check.That(resultingEvents).Contains(expected);
        }

        [Test]
        public void should_initialize_update_progress()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context,
                FullLoader(appVersion: "2028.12.31.1", bspVersion: "2028.10.21.1", firmwareVersion: "2025.1.15.1"), DomainEventFactory(), typeof(Ready));
            var resultingEvents = runner.Handle(CommandRequested.Create(GeneralUpdateCommand.command, StatesHelper.PackageLocation, TimeSpan.Zero));
            var expected = DomainEventFactory().NewEventResult(new (Urn urn, object value)[]
            {
                (dummy.mcu1.update_progress, Percentage.FromFloat(0.0f).Value),
                (dummy.mcu2.update_progress, Percentage.FromFloat(0.0f).Value),
                (dummy.app1.update_progress, Percentage.FromFloat(0.0f).Value),
                (dummy.bsp.update_progress, Percentage.FromFloat(0.0f).Value),
            });
            Check.That(resultingEvents).Contains(expected.Value);
        }


        [Test]
        public void should_publish_started_state()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, (location, softwareDeviceMap) => default, DomainEventFactory(), typeof(Updating));
            var resultingEvents = runner.Handle(new UpdateCanceled(TimeSpan.Zero));
            var expected = PropertiesChanged.Create(Model().UpdateState, UpdateState.Ready, TimeSpan.Zero);

            Check.That(resultingEvents).Contains(expected);
        }

        [Test]
        public void should_publish_commiting_state()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, (location, softwareDeviceMap) => default, DomainEventFactory(), typeof(Updating));
            var resultingEvents = runner.Handle(new UpdateCompleted(TimeSpan.Zero));
            var expected = PropertiesChanged.Create(Model().UpdateState, UpdateState.Commiting, TimeSpan.Zero);

            Check.That(resultingEvents).Contains(expected);
        }
    }
}