using System;
using System.Globalization;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.RuntimeFoundations.Tests.FactoryTests.ValueObjects
{
    [ValueObject]
    public readonly struct PowerConsumption
    {
        [ModelFactoryMethod]
        public static Result<PowerConsumption> FromString(string value)
        {
            var isfloat = Single.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pct);
            if (!isfloat) return new InvalidValueError($"{value} is not valid for {nameof(Intensity)}");
            return new PowerConsumption(pct);
        }

        public static Result<PowerConsumption> FromFloat(float value)
        {
            return new PowerConsumption(value);
        }

        private readonly float _value;

        private PowerConsumption(float value)
        {
            _value = value;
        }
        
        private bool Equals(PowerConsumption other)
        {
            return Math.Abs(_value - other._value) < Single.Epsilon;
        }

        public override bool Equals(object obj)
        {
            return obj is PowerConsumption other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return _value.ToString("F2",CultureInfo.InvariantCulture);
        }
    }
}