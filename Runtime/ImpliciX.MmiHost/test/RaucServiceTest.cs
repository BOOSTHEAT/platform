using ImpliciX.MmiHost.Services;
using NUnit.Framework;

namespace ImpliciX.MmiHost.Tests
{
    [TestFixture]
    public class RaucServiceTest
    {
        [TestCase("bootfs.0","bootfs.1")]
        [TestCase("bootfs.1","bootfs.0")]
        [TestCase("qsdqs","")]
        public void should_get_the_opposite_partition(string partitionActive,string expected)
        {
            var oppositePartition = RaucService.GetOppositePartition(partitionActive);
            Assert.AreEqual(expected,oppositePartition);
        }
    }
}