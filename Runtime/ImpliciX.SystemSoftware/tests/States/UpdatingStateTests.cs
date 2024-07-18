using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SystemSoftware.States;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.SystemSoftware.Tests.States.StatesHelper;

namespace ImpliciX.SystemSoftware.Tests.States
{
    public class UpdatingStateTests
    {

        [TestCase(false, 1)]
        [TestCase(true, 0)]
        public void should_update_software_only_when_target_app_is_allowed(bool isAllowed, int cancelEventCount)
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(isAllowed), context, FullLoader(bspVersion: "2021.6.15.1"),
                DomainEventFactory(), typeof(Ready));

            var resultingCommands = runner.Handle(CommandRequested.Create(GeneralUpdateCommand.command,
                StatesHelper.PackageLocation, TimeSpan.Zero)).FilterEvents<UpdateCanceled>();

            Check.That(resultingCommands).HasSize(cancelEventCount);
        }
        
        [Test]
        public void should_not_update_software_when_fallback_version_is_the_same_as_the_version_to_be_updated()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, FullLoader(bspVersion:"2021.6.15.1"), DomainEventFactory(), typeof(Ready));

            var resultingCommands = runner.Handle(CommandRequested.Create(GeneralUpdateCommand.command, StatesHelper.PackageLocation, TimeSpan.Zero))
                .FilterEvents<CommandRequested>()
                .Select(cr => cr.Urn);;
            
            var notExpected = new Urn[]
            {
                dummy.bsp._update.command
            };
            
            Check.That(resultingCommands).Not.Contains(notExpected);
        }
        
        [Test]
        public void should_update_software_when_supported_fallback_version_is_not_the_same_as_the_version_to_be_updated()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, FullLoader(appVersion:"2028.12.31.1", bspVersion:"2028.10.21.1", firmwareVersion:"2025.1.15.1"), DomainEventFactory(), typeof(Ready));

            var resultingCommands = runner.Handle(CommandRequested.Create(GeneralUpdateCommand.command, StatesHelper.PackageLocation, TimeSpan.Zero))
                .FilterEvents<CommandRequested>()
                .Select(cr => cr.Urn);
            
            var expected = new Urn[]
            {
                dummy.mcu1._update.command,
                dummy.mcu2._update.command,
                dummy.app1._update.command,
                dummy.bsp._update.command,
            };

            var notSupportedForUpdate = dummy.mcu3._update.command;
            
            Check.That(resultingCommands).Contains(expected);
            Check.That(resultingCommands).Not.Contains(notSupportedForUpdate);

        }

        [Test]
        public void should_update_software_disregarding_the_version_if_present_in_the_always_update_list()
        {
            var context = CreateContext();
            context.AlwaysUpdate.Add(dummy.bsp);
            var runner = SystemSoftwareRunner.Create(Model(), context, FullLoader(bspVersion:"2021.6.15.1"), DomainEventFactory(), typeof(Ready));

            var resultingCommands = runner.Handle(CommandRequested.Create(GeneralUpdateCommand.command, StatesHelper.PackageLocation, TimeSpan.Zero))
                .FilterEvents<CommandRequested>()
                .Select(cr => cr.Urn);;
            
            Check.That(resultingCommands).Contains(dummy.bsp._update.command);
        }

        [Test]
        public void when_nothing_to_update_cancel_the_current_update()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, FullLoader(appVersion:"2021.8.23.1", bspVersion:"2021.6.15.1", firmwareVersion:"2021.6.15.1"), DomainEventFactory(), typeof(Ready));

            var resultingEvents = runner.Handle(CommandRequested.Create(GeneralUpdateCommand.command, StatesHelper.PackageLocation, TimeSpan.Zero));

            Check.That(resultingEvents.Select(evt=>evt.GetType())).Contains(typeof(UpdateCanceled));

           
            
        }
        
        
        [Test]
        public void when_update_canceled_transition_to_started()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, FullLoader(appVersion:"2021.8.23.1", bspVersion:"2021.6.15.1", firmwareVersion:"2021.6.15.1"), DomainEventFactory(), typeof(Updating));
            var updateCanceled = new UpdateCanceled(TimeSpan.Zero);
            Check.That(runner.CanHandle(updateCanceled)).IsTrue();
            runner.Handle(updateCanceled);
            Check.That(runner.CurrentState).IsInstanceOf<Ready>();
        }
        
        
        [Test]
        public void should_update_software_when_fallback_version_is_not_known()
        {
            var context = CreateContextWithUnknownFallbackVersions();
            var runner = SystemSoftwareRunner.Create(Model(), context, FullLoader(), DomainEventFactory(), typeof(Ready));

            var resultingCommands = 
                runner.Handle(CommandRequested.Create(GeneralUpdateCommand.command, StatesHelper.PackageLocation, TimeSpan.Zero))
                .FilterEvents<CommandRequested>()
                .Select(cr => cr.Urn);;
            var expectedEvents = new Urn[]
            {
                dummy.mcu1._update.command,
                dummy.mcu2._update.command,
                dummy.app1._update.command,
                dummy.bsp._update.command,
            };
            Check.That(resultingCommands).Contains(expectedEvents);
        }
        
        [Test]
        public void should_send_update_commands_to_all_supported_devices_that_are_present_in_the_harmony_package_full()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, FullLoader(), DomainEventFactory(), typeof(Ready));

            var resultingCommands = runner.Handle(CommandRequested.Create(GeneralUpdateCommand.command, StatesHelper.PackageLocation, TimeSpan.Zero))
                .FilterEvents<CommandRequested>()
                .Select(cr => cr.Urn);

            
            var expected = new Urn[]
            {
                dummy.mcu1._update.command,
                dummy.mcu2._update.command,
                dummy.app1._update.command,
                dummy.bsp._update.command,
            };

            var notSupportedForUpdate = dummy.mcu3._update.command;
            
            Check.That(resultingCommands).Contains(expected);
            Check.That(resultingCommands).Not.Contains(notSupportedForUpdate);
        }


        
        [Test]
        public void when_all_devices_report_100_percent_progress_the_system_is_updated_full_package()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, FullLoader(), DomainEventFactory(), typeof(Updating));
            var updatingState = (Updating)runner.CurrentState;
            updatingState.SoftwareToBeUpdated = new HashSet<SoftwareDeviceNode>() { dummy.mcu1, dummy.mcu2, dummy.bsp, dummy.app1 };

            {
                runner.PlayEvents(
                    PropertiesChanged.Create(dummy.mcu1.update_progress, Percentage.FromFloat(0.5f).Value, TimeSpan.Zero),
                    PropertiesChanged.Create(dummy.mcu2.update_progress, Percentage.FromFloat(1f).Value, TimeSpan.Zero),
                    PropertiesChanged.Create(dummy.bsp.update_progress, Percentage.FromFloat(0.1f).Value, TimeSpan.Zero),
                    PropertiesChanged.Create(dummy.app1.update_progress, Percentage.FromFloat(0.2f).Value, TimeSpan.Zero)
                );
                Check.That(runner.CurrentState).IsInstanceOf<Updating>();
            }

            {
                runner.PlayEvents(
                    PropertiesChanged.Create(dummy.mcu1.update_progress, Percentage.FromFloat(1f).Value, TimeSpan.Zero),
                    PropertiesChanged.Create(dummy.bsp.update_progress, Percentage.FromFloat(1f).Value, TimeSpan.Zero),
                    PropertiesChanged.Create(dummy.app1.update_progress, Percentage.FromFloat(1f).Value, TimeSpan.Zero)
                );
                Check.That(runner.CurrentState).IsInstanceOf<Commiting>();
            }
        }

        [Test]
        public void when_all_devices_report_100_percent_progress_the_system_is_updated_partial_package()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, WithMcuOnlyLoader, DomainEventFactory(), typeof(Updating));
            var updatingState = (Updating)runner.CurrentState;
            updatingState.SoftwareToBeUpdated = new HashSet<SoftwareDeviceNode>() { dummy.mcu1, dummy.mcu2 };
            {
                runner.PlayEvents(
                    PropertiesChanged.Create(dummy.mcu1.update_progress, Percentage.FromFloat(0.5f).Value, TimeSpan.Zero),
                    PropertiesChanged.Create(dummy.mcu2.update_progress, Percentage.FromFloat(1f).Value, TimeSpan.Zero)
                );
                Check.That(runner.CurrentState).IsInstanceOf<Updating>();
            }

            {
                runner.PlayEvents(
                    PropertiesChanged.Create(dummy.mcu1.update_progress, Percentage.FromFloat(1f).Value, TimeSpan.Zero)
                );
                Check.That(runner.CurrentState).IsInstanceOf<Commiting>();
            }
        }
        
        [Test]
        public void can_handle_update_command()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(),context, (location, softwareDeviceMap)=>default, DomainEventFactory(), typeof(Ready));
            var can = runner.CanHandle(CommandRequested.Create(GeneralUpdateCommand.command, StatesHelper.PackageLocation, TimeSpan.Zero));
            Check.That(can).IsTrue();
        }

        [Test]
        public void can_handle_update_completed()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, WithMcuOnlyLoader, DomainEventFactory(), typeof(Updating));
            var can = runner.CanHandle(new UpdateCompleted(TimeSpan.Zero));

            Check.That(can).IsTrue();
        }

        [Test]
        public void can_handle_properties_changed_concerning_devices_to_update()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, StatesHelper.WithMcuOnlyLoader, DomainEventFactory(), typeof(Updating));
            var updatingState = (Updating)runner.CurrentState;
            updatingState.SoftwareToBeUpdated = new HashSet<SoftwareDeviceNode>() { dummy.mcu1, dummy.mcu2 };
            var progressDevice1 = PropertiesChanged.Create(dummy.mcu1.update_progress, Percentage.FromFloat(0.5f).Value, TimeSpan.Zero);
            var progressDevice2 = PropertiesChanged.Create(dummy.mcu2.update_progress, Percentage.FromFloat(1f).Value, TimeSpan.Zero);
            var progressDeviceNotUpdated =  PropertiesChanged.Create(dummy.bsp.update_progress, Percentage.FromFloat(1f).Value, TimeSpan.Zero);

            Check.That(runner.CanHandle(progressDevice1)).IsTrue();
            Check.That(runner.CanHandle(progressDevice2)).IsTrue();
            Check.That(runner.CanHandle(progressDeviceNotUpdated)).IsFalse();
        }


        [TearDown]
        public void TearDown()
        {
            if(File.Exists(UpdateManifestFilePath))
                File.Delete(UpdateManifestFilePath);
        }

        private static Context CreateContext()
        {
            return new Context(Model().SoftwareMap)
            {
                SupportedForUpdate = new HashSet<SoftwareDeviceNode>()
                {
                    dummy.mcu1,
                    dummy.mcu2,
                    dummy.app1,
                    dummy.bsp,
                },
                UpdateManifestPath = UpdateManifestFilePath,
                FallbackReleaseManifestPath = "package_examples/fallback_release_dummy_manifest.json",
                CurrentReleaseManifestPath = "package_examples/current_release_dummy_manifest.json"
            };
        }
        
        private static Context CreateContextWithUnknownFallbackVersions()
        {
            return new Context(Model().SoftwareMap)
            {
                SupportedForUpdate = new HashSet<SoftwareDeviceNode>()
                {
                    dummy.mcu1,
                    dummy.mcu2,
                    dummy.app1,
                    dummy.bsp,
                },
                UpdateManifestPath = UpdateManifestFilePath,
                CurrentReleaseManifestPath = "package_examples/current_release_dummy_manifest.json"
            };
        }

        
       

    }
}