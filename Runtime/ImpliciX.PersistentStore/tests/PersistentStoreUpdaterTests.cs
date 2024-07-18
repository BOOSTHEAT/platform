using System;
using System.Collections.Generic;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Storage;
using Moq;
using NUnit.Framework;

namespace ImpliciX.PersistentStore.Tests
{
    [TestFixture]
    public class PersistentStoreUpdaterTests
    {
        private UserSettingUrn<int> _userSetting;
        private VersionSettingUrn<int> _versionSetting;
        private FactorySettingUrn<int> _factorySetting;
        private IDictionary<Type, int> _settingKinds;
        private Mock<IWriteToStorage> _writerSpy;
        
        [SetUp]
        public void Init()
        {
            _userSetting = UserSettingUrn<int>.Build(nameof(_userSetting));
            _versionSetting = VersionSettingUrn<int>.Build(nameof(_versionSetting));
            _factorySetting = FactorySettingUrn<int>.Build(nameof(_factorySetting));
            _settingKinds = new Dictionary<Type, int>()
            {
                { typeof(UserSettingUrn<>), 18 },
                { typeof(VersionSettingUrn<>), 24 },
                { typeof(PersistentCounterUrn<>), 32 },
            };
            _writerSpy = new Mock<IWriteToStorage>();
            _writerSpy.Setup(x => x.WriteHash(It.IsAny<int>(), It.IsAny<HashValue>())).Returns(Result<Unit>.Create(new Unit()));
        }

        [Test]
        public void should_write_in_corresponding_db()
        {
            var sut = new PersistentStoreUpdater(_writerSpy.Object);
            var handle = sut.HandlePersistentChangeRequested(_settingKinds);
            handle(PersistentChangeRequest.Create(new[]
            {
                Property<int>.Create(_userSetting, 180, TimeSpan.Zero),
                Property<int>.Create(_versionSetting, 240, TimeSpan.Zero)
            }, TimeSpan.Zero));

            _writerSpy.Verify(x => x.WriteHash(18, new HashValue(nameof(_userSetting), "180", TimeSpan.Zero)), Times.Once);
            _writerSpy.Verify(x => x.WriteHash(24, new HashValue(nameof(_versionSetting), "240", TimeSpan.Zero)), Times.Once);
            _writerSpy.VerifyNoOtherCalls();
        }

        [Test]
        public void should_skip_when_no_corresponding_db()
        {
            var sut = new PersistentStoreUpdater(_writerSpy.Object);
            var handle = sut.HandlePersistentChangeRequested(_settingKinds);
            handle(PersistentChangeRequest.Create(new[]
            {
                Property<int>.Create(_userSetting, 180, TimeSpan.Zero),
                Property<int>.Create(_factorySetting, 240, TimeSpan.Zero)
            }, TimeSpan.Zero));

            _writerSpy.Verify(x => x.WriteHash(18, new HashValue(nameof(_userSetting), "180", TimeSpan.Zero)), Times.Once);
            _writerSpy.VerifyNoOtherCalls();
        }
    }
}