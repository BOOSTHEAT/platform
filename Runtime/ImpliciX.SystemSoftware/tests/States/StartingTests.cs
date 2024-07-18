using System;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SystemSoftware.States;
using NFluent;
using NUnit.Framework;
using static ImpliciX.SystemSoftware.Tests.States.StatesHelper;

namespace ImpliciX.SystemSoftware.Tests.States
{
    public class StartingTests
    {
        [Test]
        public void should_initialize_the_context()
        {
          
            var context = new Context(Model().SoftwareMap)
            {
                FallbackReleaseManifestPath = "package_examples/fallback_release_dummy_manifest.json",
                CurrentReleaseManifestPath = "package_examples/current_release_dummy_manifest.json"
            };
            
            var runner = SystemSoftwareRunner.Create(Model(),context, (location, softwareDeviceMap)=>default, DomainEventFactory());
            var resultingEvents = runner.Activate();

            Check.That(context.GetFallbackVersion(dummy.app1).IsSome).IsTrue();
            Check.That(context.GetFallbackVersion(dummy.app1).GetValue()).IsEqualTo(SoftwareVersion.Create(2021,8,23,1));
            Check.That(resultingEvents.Any(evt => evt is StartingCompleted)).IsTrue();
        }


       
        [Test]
        public void when_starting_publish_the_current_release_version_from_the_manifest()
        {
            var context = new Context(Model().SoftwareMap)
            {
                FallbackReleaseManifestPath = "package_examples/fallback_release_dummy_manifest.json",
                CurrentReleaseManifestPath = "package_examples/current_release_dummy_manifest.json"
            };
            
            var runner = SystemSoftwareRunner.Create(Model(),context, (location, softwareDeviceMap)=>default, DomainEventFactory());
            
            var resultingEvents = runner.Activate();

            Check.That(resultingEvents).Contains(
                PropertiesChanged.Create(Model().ReleaseVersion, SoftwareVersion.FromString("2.3.4.613").Value, TimeSpan.Zero));
            Check.That(context.CurrentReleaseManifest.Revision).IsEqualTo("2.3.4.613");
        }
        
        [Test]
        public void should_transition_to_started()
        {
            var context = new Context(Model().SoftwareMap)
            {
                FallbackReleaseManifestPath = "package_examples/fallback_release_dummy_manifest.json",
                CurrentReleaseManifestPath = "package_examples/current_release_dummy_manifest.json"
            };
            
            var runner = SystemSoftwareRunner.Create(Model(),context, (location, softwareDeviceMap)=>default, DomainEventFactory());
            
            runner.Activate();
            var startingCompleted = new StartingCompleted(TimeSpan.Zero);
            Check.That(runner.CanHandle(startingCompleted)).IsTrue();

            runner.Handle(startingCompleted);

            Check.That(runner.CurrentState).IsInstanceOf<Ready>();

        }
    }
}