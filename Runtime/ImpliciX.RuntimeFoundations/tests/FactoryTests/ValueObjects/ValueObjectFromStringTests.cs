using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.RuntimeFoundations.Tests.FactoryTests.ValueObjects
{
    [TestFixture]
    public class ValueObjectFromStringTests
    {
        [Test]
        public void rotational_from_string_ExpectedBehavior()
        {
            var expected = RotationalSpeed.FromFloat(45.4578f).Value;
            var result = RotationalSpeed.FromString("45.4578").Value;
            Check.That(expected).Equals(result);
        }

        [Test]
        public void functionDefinition_from_string_ExpectedBehavior()
        {
            var expected = FunctionDefinition.From(new[] { ("c0", 1.45f), ("x", 42) });
            var result = FunctionDefinition.FromString(new[] { ("c0", "1.45"), ("x", "42") });
            Check.That(expected).Equals(result);
        }

        [Test]
        public void timeout_from_string_ExpectedBehavior()
        {
            uint milliseconds = 35;
            var expected = Duration.FromFloat(milliseconds);
            var result = Duration.FromString("35");

            Check.That(expected).Equals(result);
        }

        [Test]
        public void temperature_from_string_ExpectedBehavior()
        {
            var expected = Temperature.Create(45.400f);
            var result = Temperature.FromString("45.400").Value;
            Check.That(expected).Equals(result);
        }

        [Test]
        public void softwareVersion_from_string_ExpectedBehavior()
        {
            var expected = SoftwareVersion.Create(45, 42, 43, 41);
            var result = SoftwareVersion.FromString("45.42.43.41").Value;
            Check.That(expected).Equals(result);
        }

        [TestCase("*", false)]
        [TestCase("1.*", false)]
        [TestCase("1.2.*", false)]
        [TestCase("1.2.3.*", false)]
        [TestCase("1.2.3.4", false)]
        [TestCase("1.2.3.4.5", true)]
        [TestCase("1.2.3", true)]
        [TestCase("1.*.3", true)]
        [TestCase("1.*.*", true)]
        public void softwareVersion_is_invalid_ExpectedBehavior(string value, bool expected)
        {
            var result = SoftwareVersion.IsInvalid(value);
            Check.That(result).Equals(expected);
        }

        [Test]
        public void percentage_from_string_ExpectedBehavior()
        {
            var expected = Percentage.FromFloat(0.200f).Value;
            var result = Percentage.FromString("0.200").Value;
            Check.That(expected).Equals(result);
        }
    }
}
