using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Storage;
using Moq;
using NUnit.Framework;

namespace ImpliciX.PersistentStore.Tests
{
  [TestFixture]
  public class PersistentStoreInitializerTests
  {
            
    [Test]
    public void should_initialize_version_settings_db_when_empty()
    {
      ArrangeDbAsEmpty();
      PersistentStoreInitializer.InsertDefaultValuesIfEmpty(_settingKinds, _writerSpy.Object, _stubStorageReader.Object, _defaultVersionSettings, _initializedAt);
      _writerSpy.Verify(x => x.WriteHash(2, new HashValue(dummy.settings.my_percentage, "0.5", _initializedAt)), Times.Once);
      _writerSpy.Verify(x => x.WriteHash(2, new HashValue(dummy.settings.my_timeout, "1000", _initializedAt)), Times.Once);
      var fd = new[] {("a0", "1"), ("a1", "2"), ("a2", "3")};
      var h = new HashValue(dummy.settings.my_function, fd);
      h.SetAtField(_initializedAt);
      _writerSpy.Verify(x => x.WriteHash(2, h), Times.Once);
      _writerSpy.VerifyNoOtherCalls();
    }
    
    [Test]
    public void should_do_nothing_when_no_version_settings_are_defined()
    {
      ArrangeDbAsEmpty();
      PersistentStoreInitializer.InsertDefaultValuesIfEmpty(_settingKinds, _writerSpy.Object, _stubStorageReader.Object, new Dictionary<Urn, (string Name, string Value)[]>(), _initializedAt);
      _writerSpy.VerifyNoOtherCalls();
    }
            
    [Test]
    public void should_do_nothing_when_version_settings_are_not_defined()
    {
      ArrangeDbAsEmpty();
      PersistentStoreInitializer.InsertDefaultValuesIfEmpty(_settingKinds, _writerSpy.Object, _stubStorageReader.Object, null, _initializedAt);
      _writerSpy.VerifyNoOtherCalls();
    }
            
    [Test]
    public void should_not_initialize_version_settings_db_when_not_empty()
    {
      ArrangeDbWith(_defaultVersionSettings);
      PersistentStoreInitializer.InsertDefaultValuesIfEmpty(_settingKinds, _writerSpy.Object, _stubStorageReader.Object,_defaultVersionSettings, _initializedAt);
      _writerSpy.VerifyNoOtherCalls();
    }
    
    [Test]
    public void should_do_nothing_when_no_user_settings_are_defined()
    {
      ArrangeDbAsEmpty();
      PersistentStoreInitializer.AddMissingDefaultValues(_settingKinds, _writerSpy.Object, _stubStorageReader.Object,new Dictionary<Urn, (string Name, string Value)[]>(), _initializedAt);
      _writerSpy.VerifyNoOtherCalls();
    }
    
    [Test]
    public void should_do_nothing_when_user_settings_are_not_defined()
    {
      ArrangeDbAsEmpty();
      PersistentStoreInitializer.AddMissingDefaultValues(_settingKinds, _writerSpy.Object, _stubStorageReader.Object,null, _initializedAt);
      _writerSpy.VerifyNoOtherCalls();
    }
    
    [Test]
    public void should_initialize_user_settings_db_when_empty()
    {
      ArrangeDbAsEmpty();
      PersistentStoreInitializer.AddMissingDefaultValues(_settingKinds, _writerSpy.Object, _stubStorageReader.Object,_defaultUserSettings, _initializedAt);
      _writerSpy.Verify(x => x.WriteHash(1, new HashValue(dummy.settings.user_timeout, "2000", _initializedAt)), Times.Once);
      _writerSpy.VerifyNoOtherCalls();
    }
            
    [Test]
    public void should_add_missing_user_settings_when_db_not_empty()
    {
      ArrangeDbWith(_defaultUserSettings);
      PersistentStoreInitializer.AddMissingDefaultValues(_settingKinds, _writerSpy.Object, _stubStorageReader.Object,
        new Dictionary<Urn, (string Name, string Value)[]>
        {
          {dummy.settings.user_timeout, new[] {("value", "3000")}},
          {dummy.settings.other_timeout, new[] {("value", "3000")}}
        }, _initializedAt);
      _writerSpy.Verify(x => x.WriteHash(1, new HashValue(dummy.settings.other_timeout, "3000", _initializedAt)), Times.Once);
      _writerSpy.VerifyNoOtherCalls();
    }
    
    [SetUp]
    public void Init()
    {
      _writerSpy = new Mock<IWriteToStorage>();
      _stubStorageReader = new Mock<IReadFromStorage>();
      _settingKinds = new Dictionary<Type, int>()
      {
        {typeof(UserSettingUrn<>), 1},
        {typeof(VersionSettingUrn<>), 2},
        {typeof(FactorySettingUrn<>), 3}
      };
      _defaultVersionSettings = new Dictionary<Urn, (string Name, string Value)[]>
      {
        {dummy.settings.my_percentage, new[] {("value", "0.5")}},
        {dummy.settings.my_timeout, new[] {("value", "1000")}},
        {dummy.settings.my_function, new[] {("a0", "1"), ("a1", "2"), ("a2", "3")}}
      };
      _defaultUserSettings = new Dictionary<Urn, (string Name, string Value)[]>
      {
        {dummy.settings.user_timeout, new[] {("value", "2000")}}
      };
    }

    private void ArrangeDbAsEmpty()
    {
      _stubStorageReader.Setup(x => x.ReadAll(It.IsAny<int>())).Returns(Enumerable.Empty<HashValue>());
    }
    
    private void ArrangeDbWith(IDictionary<Urn, (string Name, string Value)[]> initialValues)
    {
      HashValue CreateHash(string key, IEnumerable<(string Name, string Value)> values) => new HashValue(key, values.Append(("at", TimeSpan.Zero.ToString())).ToArray());
      var allValues = initialValues.Select(kv => CreateHash(kv.Key,kv.Value)).ToArray();
      _stubStorageReader.Setup(x => x.ReadAll(It.IsAny<int>())).Returns(allValues);
    }
    
    private Mock<IWriteToStorage> _writerSpy;
    private Mock<IReadFromStorage> _stubStorageReader;
    private IDictionary<Type, int> _settingKinds;
    private IDictionary<Urn, (string Name, string Value)[]> _defaultVersionSettings;
    private IDictionary<Urn, (string Name, string Value)[]> _defaultUserSettings;
    private TimeSpan _initializedAt = TimeSpan.FromSeconds(1000);
  }
}