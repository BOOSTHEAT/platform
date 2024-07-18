using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.SharedKernel.Storage;
using NFluent;
using NUnit.Framework;
using StackExchange.Redis;

namespace ImpliciX.SharedKernel.Redis.Tests
{
    [TestFixture(Category = "ExcludeFromCI")]
    [Ignore("RedisTests")]
    [NonParallelizable]
    public class ReaderTests
    {
        private Helper _helper;
        private IReadFromStorage _reader;
        
        [OneTimeSetUp]
        public static void StartContainer()
        {
            RedisTestContainer.Start();
        }
        

        [SetUp]
        public void Init()
        {
            _helper = new Helper();
            _reader = new RedisReader(_helper.ConnectionString);
        }

        private void InitRedisEntry(int db, string key, (string name, string value)[] values)
        {
            var entries = values.Select(v => new HashEntry(v.name, v.value)).ToArray();
            _helper.DB(db).HashSet(key, entries);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void read_hash(int db)
        {
            var storedValues = new (string name, string value)[] { ("p0", "p0value"), ("p1", "p1value") };
            var fooKey = "foo";
            InitRedisEntry(db, fooKey, storedValues);
            var expected = new HashValue(fooKey, storedValues);
            var readValue = _reader.ReadHash(db, fooKey);

            Check.That(readValue).IsEqualTo(expected);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void read_all_entries(int db)
        {
            var fooValues = new (string name, string value)[] { ("p0", "p0value"), ("p1", "p1value") };
            var fooKey = "foo";
            var barValues = new (string name, string value)[] { ("p2", "p2value"), ("p3", "p3value") };
            var barKey = "bar";
            InitRedisEntry(db, fooKey, fooValues);
            InitRedisEntry(db, barKey, barValues);
            var expected = new HashValue[]
            {
                new HashValue(fooKey, fooValues),
                new HashValue(barKey, barValues),
            };
            var readValues = _reader.ReadAll(db);
            Check.That(readValues).Contains(expected);
        }
    }
}