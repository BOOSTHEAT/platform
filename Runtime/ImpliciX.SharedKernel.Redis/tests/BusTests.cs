using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ImpliciX.SharedKernel.Storage;
using NFluent;
using NUnit.Framework;
using StackExchange.Redis;

namespace ImpliciX.SharedKernel.Redis.Tests
{
    [TestFixture(Category = "ExcludeFromCI")]
    [Ignore("RedisTests")]
    [NonParallelizable()]
    public class BusTests
    {
        private Helper _helper;
        private Dictionary<string, string> _values;
        private IExternalBus _bus;
        private AutoResetEvent _waitHandle;

        [OneTimeSetUp]
        public static void StartContainer()
        {
            RedisTestContainer.Start();
        }
        
        
        
        [SetUp]
        public void Init()
        {
            _helper = new Helper();
            _bus = new RedisBus(_helper.ConnectionString);
            _values = new Dictionary<string, string>();
            _waitHandle = new AutoResetEvent(false);
        }

        [Test]
        public void subscribe_channel_matching_pattern()
        {
            var channel1 = "!pim";
            var channel2 = "!pam";
            _bus.SubscribeChannel((notifiedUrn, notifiedValue) =>
            {
                _values.Add(notifiedUrn, notifiedValue);
                if(notifiedUrn==channel2) 
                    _waitHandle.Set();
            },"!*");
            var expected = new Dictionary<string, string>() { { channel1, "hello" },{channel2,"hi"} };
            _helper.Subscriber.Publish(channel1, "hello");
            _helper.Subscriber.Publish(channel2, "hi");
            _waitHandle.WaitOne(1000);
            Check.That(_values).ContainsExactly(expected);
        }
        
        
        [Test]
        public void should_subscribe_all_keys()
        {
            var remaining = 3;
            for (var i = 0; i < 3; i++)
            {
                var db = i;
                _bus.SubscribeAllKeysModification(db, key =>
                {
                    _values.Add(key, _helper.DB(db).HashGet(key,"field").ToString());
                    if(--remaining == 0)
                        _waitHandle.Set();
                });
            }
            _helper.DB(0).HashSet("keytest0", new[] { new HashEntry("field", "valuetest0") });
            _helper.DB(1).HashSet("keytest1", new[] { new HashEntry("field", "valuetest1") });
            _helper.DB(2).HashSet("keytest2", new[] { new HashEntry("field", "valuetest2") });
            _waitHandle.WaitOne();
            Check.That(_values.OrderBy(x => x.Key)).ContainsExactly(
                new Dictionary<string, string>() { { "keytest0", "valuetest0" },{ "keytest1", "valuetest1" },{ "keytest2", "valuetest2" } }
                );
        }
    }
}