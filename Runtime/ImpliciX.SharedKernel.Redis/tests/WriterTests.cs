using System;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.SharedKernel.Storage;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Redis.Tests
{
    [TestFixture(Category = "ExcludeFromCI")]
    [Ignore("RedisTests")]
    [NonParallelizable]
    public class WriterTests
    {
        private Helper _helper;
        private TimeSpan _currentTime;
        private string _fooKey;
        private (string, string)[] _storedValues;
        private IWriteToStorage _writer;

        [OneTimeSetUp]
        public static void StartContainer()
        {
            RedisTestContainer.Start();
        }

        
        [SetUp]
        public void Init()
        {
            _fooKey = "foo";
            _storedValues = new (string name, string value)[] { ("p0", "p0value"), ("p1", "p1value"), ("at", _currentTime.ToString())};
            _currentTime = new TimeSpan(10, 15, 50);
            _helper = new Helper();
            _writer = new RedisWriter(_helper.ConnectionString);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void write_hash(int db)
        {
            var result = _writer.WriteHash(db, new HashValue(_fooKey, _storedValues));

            result.CheckIsSuccessAnd(
                    _ =>
                    {
                        var hashGetAll = _helper.DB(db).HashGetAll(_fooKey);
                        var valueTuples = hashGetAll.Select(h => (h.Name.ToString(), h.Value.ToString())).ToArray();
                        Check.That(valueTuples).ContainsExactly(_storedValues);
                    }
                )
            ;
        }
    }
}