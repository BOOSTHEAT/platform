using System;
using NUnit.Framework;

namespace ImpliciX.TimeSeries.Test
{
    [TestFixture]
    public class TimeSpanExtensionTest
    {
        
        [Test]
        public void should_round_to_tenth_of_second()
        {
            var ts = TimeSpan.FromMilliseconds(1234);
            Assert.AreEqual(1200,ts.Round(TimespanExtension.Precision.TenthOfSecond).TotalMilliseconds);
        }
        
        [Test]
        public void should_round_to_second()
        {
            var ts = TimeSpan.FromMilliseconds(1234567879);
            Assert.AreEqual(1234567000,ts.Round(TimespanExtension.Precision.Second).TotalMilliseconds);
        }
        
        [Test]
        public void should_round_to_millisecond()
        {
            var ts = TimeSpan.FromTicks(1234567879);
            Assert.AreEqual(1234560000,ts.Round(TimespanExtension.Precision.Millisecond).Ticks);
        }
    }
}