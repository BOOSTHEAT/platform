using System;
using System.Globalization;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.RuntimeFoundations.Tests.FactoryTests.ValueObjects
{
    [ValueObject]
    public readonly struct Intensity
    {
        [ModelFactoryMethod]
        public static Result<Intensity> FromString(string value)
        {
            var isfloat = Single.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pct);
            if (!isfloat) return new InvalidValueError($"{value} is not valid for {nameof(Intensity)}");
            return new Intensity(pct);
        }

        public static Result<Intensity> FromFloat(float value)
        {
            return new Intensity(value);
        }

        private readonly float _value;

        private Intensity(float value)
        {
            _value = value;
        }

        private bool Equals(Intensity other)
        {
            return Math.Abs(_value - other._value) < Single.Epsilon;
        }

        public override bool Equals(object obj)
        {
            return obj is Intensity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return _value.ToString("F5",CultureInfo.InvariantCulture);
        }
    }
}