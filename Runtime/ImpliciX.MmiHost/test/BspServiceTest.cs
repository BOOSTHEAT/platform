using System;
using System.Threading.Tasks;
using ImpliciX.Data.Factory;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.MmiHost.DBusProxies;
using ImpliciX.MmiHost.Services;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using Moq;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.MmiHost.Tests
{
    
    public class BspServiceTest
    {
        [Test]
        public void dont_send_100_percent_progress_until_rauc_installer_operations_is_not_back_to_idle()
        {
            var modelFactory = new ModelFactory(this.GetType().Assembly);
            var domainEventFactory = new DomainEventFactory(modelFactory, () => TimeSpan.Zero);
            var model = new MmiHostModuleDefinition()
            {
                BspSoftwareDeviceNode = TestModel.BarSoftware
            };

            var raucService = new Mock<IRaucInstallProxy>();
            raucService.Setup(it => it.GetAsync<(int, string, int)>("Progress")).Returns(Task.FromResult((100, "Installing", 0)));
            raucService.Setup(it => it.GetAsync<string>("Operation")).Returns(Task.FromResult("install"));
            
            var sut = new BspService(model, domainEventFactory, raucService.Object, BspService.InstallState.Full);
            var resultingEvents = sut.Handle(SystemTicked.Create(1000, 1));
            Check.That(resultingEvents).IsEmpty();
        }
        
        [Test]
        public void send_100_percent_progress_when_rauc_installer_operations_is_back_to_idle()
        {
            var modelFactory = new ModelFactory(this.GetType().Assembly);
            var df = new DomainEventFactory(modelFactory, () => TimeSpan.Zero);
            var model = new MmiHostModuleDefinition()
            {
                BspSoftwareDeviceNode = TestModel.BarSoftware
            };

            var raucService = new Mock<IRaucInstallProxy>();
            raucService.Setup(it => it.GetAsync<(int, string, int)>("Progress")).Returns(Task.FromResult((100, "Installing", 0)));
            raucService.SetupSequence(it => it.GetAsync<string>("Operation"))
                .Returns(Task.FromResult("install"))
                .Returns(Task.FromResult("idle"));
            
            var sut = new BspService(model, df, raucService.Object, BspService.InstallState.InProgress);
            {
                var resultingEvents = sut.Handle(SystemTicked.Create(1000, 1));
                Check.That(resultingEvents).IsEmpty();
                Check.That(sut.CurrentState).IsEqualTo(BspService.InstallState.Full);
                
            }
            {
                var resultingEvents = sut.Handle(SystemTicked.Create(1000, 1));
                var expectedEvents = new []{df.PropertiesChanged(model.BspSoftwareDeviceNode.update_progress, Percentage.ONE)};
                Check.That(resultingEvents).IsEquivalentTo(expectedEvents);
                Check.That(sut.CurrentState).IsEqualTo(BspService.InstallState.Done);
            }
        }

        [Test]
        public void when_install_command_requested_current_state_is_Started()
        {
            var modelFactory = new ModelFactory(this.GetType().Assembly);
            var df = new DomainEventFactory(modelFactory, () => TimeSpan.Zero);
            var model = new MmiHostModuleDefinition()
            {
                BspSoftwareDeviceNode = TestModel.BarSoftware
            };

            var raucService = new Mock<IRaucInstallProxy>();
            var sut = new BspService(model, df, raucService.Object, forceRoPath:"dummy_ro.txt");
            sut.Handle(df.CommandRequested(model.BspSoftwareDeviceNode._update, new PackageContent(model.BspSoftwareDeviceNode, "1.1.1.1", Array.Empty<byte>())));
            Check.That(sut.CurrentState).IsEqualTo(BspService.InstallState.Started);
        }
        
        
        [Test]
        public void when_install_progress_is_between_1_and_99_percent_install_is_in_progress()
        {
            var modelFactory = new ModelFactory(this.GetType().Assembly);
            var df = new DomainEventFactory(modelFactory, () => TimeSpan.Zero);
            var model = new MmiHostModuleDefinition()
            {
                BspSoftwareDeviceNode = TestModel.BarSoftware
            };

            var raucService = new Mock<IRaucInstallProxy>();
            raucService.SetupSequence(x => x.GetAsync<(int, string, int)>("Progress"))
                .Returns(Task.FromResult((1,"install",1)))
                .Returns(Task.FromResult((99,"install",1)));
            
            var sut = new BspService(model, df, raucService.Object, BspService.InstallState.InProgress,  forceRoPath:"dummy_ro.txt");
            {
                var resultingEvents = sut.Handle(SystemTicked.Create(1000, 1));
                var expectedEvents = new []{df.PropertiesChanged(model.BspSoftwareDeviceNode.update_progress, Percentage.FromFloat(.01f).Value)};
                Check.That(resultingEvents).IsEquivalentTo(expectedEvents);
                Check.That(sut.CurrentState).IsEqualTo(BspService.InstallState.InProgress);
            }
            {
                var resultingEvents = sut.Handle(SystemTicked.Create(1000, 1));
                var expectedEvents = new []{df.PropertiesChanged(model.BspSoftwareDeviceNode.update_progress, Percentage.FromFloat(.99f).Value)};
                Check.That(resultingEvents).IsEquivalentTo(expectedEvents);
                Check.That(sut.CurrentState).IsEqualTo(BspService.InstallState.InProgress);
            }
        }
    }

 
}