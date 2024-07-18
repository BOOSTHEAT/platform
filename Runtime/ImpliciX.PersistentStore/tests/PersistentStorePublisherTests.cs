using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Storage;
using ImpliciX.TestsCommon;
using Moq;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.PersistentStore.Tests
{
    public class PersistentStorePublisherTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void should_send_all_configuration_on_start(int db)
        {
            InitReader(db,
                new TimeSpan(1, 2, 3),
                new Dictionary<string, IEnumerable<(string Name, string Value)>>
                {
                    {dummy.settings.my_percentage.ToString(), new []{("value", "0.5")}},
                    {dummy.settings.my_timeout.ToString(), new []{("value", "1000")}},
                    {dummy.settings.my_function.ToString(), new []{("a0","1"), ("a1","2"),("a2","3")}},
                }, _emptyValues);
            var publishTime = new TimeSpan(4, 5, 6);
            var sut = new PersistentStorePublisher(typeof(VersionSettingUrn<>), db, _spyEventBus.Publish, _stubStorageReader.Object, new ModelInstanceBuilder(_modelFactory).Create, _stubExternalBus.Object, () => publishTime);
            
            var expectedConfig = new IDataModelValue[]
            {
                Property<Percentage>.Create(dummy.settings.my_percentage, Percentage.FromFloat(0.5f).Value, publishTime),
                Property<Duration>.Create(dummy.settings.my_timeout, Duration.FromFloat(1000).GetValueOrDefault(), publishTime),
                Property<FunctionDefinition>.Create(dummy.settings.my_function, FunctionDefinition.From(new[]{("a0",1f),("a1",2f),("a2",3f)}).GetValueOrDefault(), publishTime),
            };

            Check.That(((PropertiesChanged)_spyEventBus.RecordedPublications[0]).ModelValues).ContainsExactly(expectedConfig);
            Check.That(((PropertiesChanged) _spyEventBus.RecordedPublications[0]).ModelValues.All(mv => mv.At == publishTime)).IsTrue();
        }
        
        [Test]
        public void should_remove_backward_compatibility_duplicates_on_start()
        {
            var backwardCompatibility = new Dictionary<string, Urn>
            {
                {"my_older_app.temperature", dummy.settings.my_temperature},
                {"my_old_app.temperature", dummy.settings.my_temperature},
                {"my_old_app.percentage", dummy.settings.my_percentage},
                {"my_old_app.timeout", dummy.settings.my_timeout}
            };
            _modelFactory = new ModelFactory(typeof(dummy).Assembly, backwardCompatibility);

            InitReader(1,
                new TimeSpan(1, 2, 3),
                new Dictionary<string, IEnumerable<(string Name, string Value)>>
                {
                    {"my_old_app.temperature", new []{("value", "323.5")}},
                    {"my_older_app.temperature", new []{("value", "313.5")}},
                    {dummy.settings.my_percentage.ToString(), new []{("value", "0.5")}},
                    {"my_old_app.percentage", new []{("value", "0.3")}},
                    {"my_old_app.timeout", new []{("value", "500")}},
                    {dummy.settings.my_timeout.ToString(), new []{("value", "1000")}},
                    {dummy.settings.my_function.ToString(), new []{("a0","1"), ("a1","2"),("a2","3")}},
                }, _emptyValues);
            var publishTime = new TimeSpan(4, 5, 6);
            var sut = new PersistentStorePublisher(typeof(VersionSettingUrn<>), 1, _spyEventBus.Publish, _stubStorageReader.Object, new ModelInstanceBuilder(_modelFactory).Create, _stubExternalBus.Object, () => publishTime);
            
            var expectedConfig = new IDataModelValue[]
            {
                Property<Temperature>.Create(dummy.settings.my_temperature, Temperature.FromFloat(323.5f).Value, publishTime),
                Property<Percentage>.Create(dummy.settings.my_percentage, Percentage.FromFloat(0.5f).Value, publishTime),
                Property<Duration>.Create(dummy.settings.my_timeout, Duration.FromFloat(1000).GetValueOrDefault(), publishTime),
                Property<FunctionDefinition>.Create(dummy.settings.my_function, FunctionDefinition.From(new[]{("a0",1f),("a1",2f),("a2",3f)}).GetValueOrDefault(), publishTime),
            };

            Check.That(((PropertiesChanged)_spyEventBus.RecordedPublications[0]).ModelValues).ContainsExactly(expectedConfig);
            Check.That(((PropertiesChanged) _spyEventBus.RecordedPublications[0]).ModelValues.All(mv => mv.At == publishTime)).IsTrue();
        }
        
        
        [Test]
        public void should_check_setting_kind_on_start()
        {
            InitReader(0,
                new TimeSpan(1, 2, 3),
                new Dictionary<string, IEnumerable<(string Name, string Value)>>
                {
                    {dummy.settings.my_percentage.ToString(), new []{("value", "0.5")}},
                    {dummy.settings.user_timeout.ToString(), new []{("value", "1000")}},
                    {dummy.settings.my_function.ToString(), new []{("a0","1"), ("a1","2"),("a2","3")}}
                }, _emptyValues);
            Check.ThatCode(() =>
            {
                var sut = new PersistentStorePublisher(typeof(VersionSettingUrn<>), 0, _spyEventBus.Publish,
                    _stubStorageReader.Object, new ModelInstanceBuilder(_modelFactory).Create, _stubExternalBus.Object, () => TimeSpan.Zero);
            }).Throws<Exception>().WithMessage("Unexpected settings (should be VersionSettingUrn): dummy:settings:user_timeout");
        }
        
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void should_send_configuration_updates(int db)
        {
            InitReader(db, TimeSpan.Zero, _emptyValues, new Dictionary<string, IEnumerable<(string Name, string Value)>>
            {
                {dummy.settings.my_timeout.ToString(), new []{("value", "2000")}}
            });
            _stubExternalBus
                .Setup(x => x.SubscribeAllKeysModification(db, It.IsAny<Action<string>>()))
                .Callback<int,Action<string>>((d, a) => a(dummy.settings.my_timeout.ToString()));
            var publishTime = new TimeSpan(4, 5, 6);
            var sut = new PersistentStorePublisher(typeof(VersionSettingUrn<>), db, _spyEventBus.Publish, _stubStorageReader.Object, new ModelInstanceBuilder(_modelFactory).Create, _stubExternalBus.Object, () => publishTime);
            
            sut.Run();
            
            _stubExternalBus.Verify(x => x.SubscribeAllKeysModification(db,It.IsAny<Action<string>>()), Times.Once);
            _stubExternalBus.VerifyNoOtherCalls();
            Check.That(((PropertiesChanged) _spyEventBus.RecordedPublications[0]).ModelValues).IsEmpty();
            Check.That(((PropertiesChanged)_spyEventBus.RecordedPublications[1]).ModelValues).ContainsExactly(new IDataModelValue[]
            {
                Property<Duration>.Create(dummy.settings.my_timeout, Duration.FromFloat(2000).GetValueOrDefault(), publishTime)
            });
        }
        
        private void InitReader(int db, TimeSpan recordedTime, IDictionary<string, IEnumerable<(string Name, string Value)>> initialValues, IDictionary<string, IEnumerable<(string Name, string Value)>> nextValues)
        {
            HashValue CreateHash(string key, IEnumerable<(string Name, string Value)> values) => new HashValue(key, values.Append(("at", recordedTime.ToString())).ToArray());
            var allValues = initialValues.Select(kv => CreateHash(kv.Key,kv.Value)).ToArray();
            _stubStorageReader.Setup(x => x.ReadAll(db)).Returns(allValues);
            var newValues = nextValues.ToDictionary(kv => kv.Key, kv => CreateHash(kv.Key,kv.Value));
            var currentKey = string.Empty;
            _stubStorageReader
                .Setup(x => x.ReadHash(db, It.IsAny<string>()))
                .Callback<int,string>((d,k) => currentKey = k)
                .Returns(() => newValues[currentKey]);
        }

        [SetUp]
        public void Init()
        {
            _spyEventBus = new SpyEventBus();
            _stubExternalBus = new Mock<IExternalBus>();
            _stubStorageReader = new Mock<IReadFromStorage>();
            _modelFactory = new ModelFactory(typeof(dummy).Assembly);
            _emptyValues = new Dictionary<string, IEnumerable<(string Name, string Value)>>();
        }

        private SpyEventBus _spyEventBus;
        private Mock<IReadFromStorage> _stubStorageReader;
        private Mock<IExternalBus> _stubExternalBus;
        private ModelFactory _modelFactory;
        private IDictionary<string, IEnumerable<(string Name, string Value)>> _emptyValues;
    }
}