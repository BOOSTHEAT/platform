using System.Linq;
using ImpliciX.SharedKernel.Storage;
using NFluent;
using NUnit.Framework;
using StackExchange.Redis;

namespace ImpliciX.SharedKernel.Redis.Tests
{
    
    [TestFixture(Category = "ExcludeFromCI")]
    [Ignore("RedisTests")]
    [NonParallelizable]
    public class CleanerTests
    {
        
        private Helper _helper;
        private ICleanStorage _cleaner;
        private RedisReader _reader;

        [OneTimeSetUp]
        public static void StartContainer()
        {
            RedisTestContainer.Start();
        }
        
        [SetUp]
        public void Init()
        {
            _helper = new Helper();
            _cleaner = new RedisCleaner(_helper.ConnectionString);
            _reader = new RedisReader(_helper.ConnectionString);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void clean_test(int db)
        {
            var storedValues = new (string name, string value)[] { ("p0", "p0value"), ("p1", "p1value") };
            InitRedisEntry(db, "foo", storedValues);
            InitRedisEntry(db, "bar", storedValues);
            InitRedisEntry(db, "fizz", storedValues);
            
            _cleaner.FlushDb(db);
           
            Check.That(_reader.ReadAll(db)).CountIs(0);
        }   
        
        
        private void InitRedisEntry(int db, string key, (string name, string value)[] values)
        {
            var entries = values.Select(v => new HashEntry(v.name, v.value)).ToArray();
            _helper.DB(db).HashSet(key, entries);
        }
        
    }
}