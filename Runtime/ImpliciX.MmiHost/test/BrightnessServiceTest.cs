using ImpliciX.Language.Model;
using ImpliciX.MmiHost.Services;
using NUnit.Framework;

namespace ImpliciX.MmiHost.Tests
{
    [TestFixture]
    public class BrightnessServiceTest
    {
        [TestCase(0.0f, (ushort) 5)]
        [TestCase(1.0f, (ushort) 255)]
        [TestCase(0.5f, (ushort) 130)]
        public void test_convert_to_brightness(float input, ushort expected)
        {
            var percentage = Percentage.FromFloat(input).Value;
            var actual = BrightnessService.ComputeBrightNess(percentage);
            Assert.AreEqual(actual, expected);
        }
    }
}