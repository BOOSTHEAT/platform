using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.Language.Store;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Storage;
using Moq;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.PersistentStore.Tests
{
    public class PersistentStoreCleanerTests
    {
        [Test]
        public void should_clean_the_corresponding_db()
        {
            var model = new PersistentStoreModuleDefinition() { CleanVersionSettings = dummy._clean_version_settings }; 
            var sut = new PersistentStoreCleaner(_cleanerSpy.Object, _domainFactoryStub.Object, model);
            var handle = sut.HandleCommandRequested(_settingKinds);
            var resultingEvents =  handle(CommandRequested.Create(dummy._clean_version_settings.command,default(NoArg), TimeSpan.Zero));
      
            _cleanerSpy.Verify(x => x.FlushDb(24), Times.Once);
            _cleanerSpy.VerifyNoOtherCalls();
            Check.That(resultingEvents).ContainsExactly(PropertiesChanged.Create(dummy._clean_version_settings.status, MeasureStatus.Success, TimeSpan.Zero));
        }
        
        [SetUp]
        public void Init()
        {
            _userSetting = UserSettingUrn<int>.Build(nameof(_userSetting));
            _versionSetting = VersionSettingUrn<int>.Build(nameof(_versionSetting));
            _factorySetting = FactorySettingUrn<int>.Build(nameof(_factorySetting));
            _settingKinds = new Dictionary<Type, int>()
            {
                {typeof(UserSettingUrn<>), 18},
                {typeof(VersionSettingUrn<>), 24}
            };
            _cleanerSpy = new Mock<ICleanStorage>();
            _cleanerSpy.Setup(x => x.FlushDb(24)).Returns(Result<Unit>.Create(new Unit()));
            _domainFactoryStub = new Mock<IDomainEventFactory>();
            _domainFactoryStub.Setup(x => x.NewEventResult(It.IsAny<Urn>(), It.IsAny<object>()))
                .Returns(Result<DomainEvent>.Create(PropertiesChanged.Create(dummy._clean_version_settings.status, MeasureStatus.Success, TimeSpan.Zero)));
        }
        
        private UserSettingUrn<int> _userSetting;
        private VersionSettingUrn<int> _versionSetting;
        private FactorySettingUrn<int> _factorySetting;
        private IDictionary<Type, int> _settingKinds;
        private Mock<ICleanStorage> _cleanerSpy;
        private Mock<IDomainEventFactory> _domainFactoryStub;
    }
}