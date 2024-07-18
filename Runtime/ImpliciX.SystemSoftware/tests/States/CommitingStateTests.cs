using System;
using System.Collections.Generic;
using System.IO;
using ImpliciX.Data;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SystemSoftware.States;
using NFluent;
using NUnit.Framework;
using static ImpliciX.SystemSoftware.Tests.States.StatesHelper;

namespace ImpliciX.SystemSoftware.Tests.States
{
    public class CommitingStateTests
    {
        [Test]
        public void should_send_commands_required_for_commit()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, (location, softwareDeviceMap)=>default, DomainEventFactory(), typeof(Updating));
            var resultingEvents = runner.Handle(new UpdateCompleted(TimeSpan.Zero));

            var expectedEvents = new DomainEvent[]
            {
                CommandRequested.Create(Model().CommitUpdateCommand, default(NoArg), TimeSpan.Zero),
                CommandRequested.Create(Model().CleanVersionSettings, default(NoArg), TimeSpan.Zero),
                PropertiesChanged.Create(Model().UpdateState, UpdateState.Commiting, TimeSpan.Zero),
            };
            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }        
        
        [Test]
        public void when_success_received_should_commit_is_completed()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, (location, softwareDeviceMap)=>default, DomainEventFactory(), typeof(Commiting));
            var resultingEvents = runner.PlayEvents(
                    PropertiesChanged.Create(Model().CleanVersionSettings.status, MeasureStatus.Success, TimeSpan.Zero)
                );

            Check.That(resultingEvents).ContainsExactly(CommandRequested.Create(Model().RebootCommand, default(NoArg), TimeSpan.Zero));
        }        
        
        [Test]
        public void should_copy_the_manifest_of_the_release_to_be_updated()
        {
            var context = CreateContext();
            context.SetCurrentUpdatePackage(CreatePackage());
            var runner = SystemSoftwareRunner.Create(Model(), context, (location, softwareDeviceMap)=>default, DomainEventFactory(), typeof(Updating));
            try
            {
                runner.Handle(new UpdateCompleted(TimeSpan.Zero));
                Check.That(File.Exists(context.UpdateManifestPath)).IsTrue();
            }
            finally
            {
                if (File.Exists(context.UpdateManifestPath))
                {
                    File.Delete(context.UpdateManifestPath);
                }
            }
        }

 

        [Test]
        public void when_failed_received_should_commit_is_completed()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, (location, softwareDeviceMap)=>default, DomainEventFactory(), typeof(Commiting));
            var resultingEvents = runner.PlayEvents(
                PropertiesChanged.Create(Model().CleanVersionSettings.status, MeasureStatus.Failure, TimeSpan.Zero)
            );

            Check.That(resultingEvents).Not.ContainsExactly(CommandRequested.Create(Model().RebootCommand, default(NoArg), TimeSpan.Zero));
        }

        [Test]
        public void can_handle_properties_changed()
        {
            var context = CreateContext();
            var runner = SystemSoftwareRunner.Create(Model(), context, (location, softwareDeviceMap)=>default, DomainEventFactory(), typeof(Commiting));

            var can = runner.CanHandle(PropertiesChanged.Create(Model().CleanVersionSettings.status, MeasureStatus.Failure, TimeSpan.Zero));
            Check.That(can).IsTrue();
        }
        
        private static Package CreatePackage()
        {
            return new Package(
                new Manifest()
                {
                    Device = "device",
                    SHA256 = "42",
                    Revision = "revision",
                    Date = DateTime.Now,
                    Content = new Manifest.ContentData()
                    {
                        APPS = new []
                        {
                            new Manifest.PartData()
                            {
                                Revision = "1.2.3.4",
                                Target = "app1",
                                FileName = "app1.zip"
                            }
                        }
                    }
                },
                new FileInfo[]
                {
                    new FileInfo("/some/path/app1.zip"),
                },
                k => new Dictionary<string, SoftwareDeviceNode>()
                {
                    ["app1"] = dummy.app1
                }[k]
            );
        }
       
    }
}