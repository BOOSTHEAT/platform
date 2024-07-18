using System;
using System.Globalization;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.TestsCommon;

[ValueObject]
public readonly struct FloatValue : IFloat
{
    private readonly float _v;

    public FloatValue(float v)
    {
        _v = v;
    }

    [ModelFactoryMethod]
    public static Result<FloatValue> FromString(string value)
    {
        float result;
        return !float.TryParse(value, NumberStyles.Float, (IFormatProvider) CultureInfo.InvariantCulture, out result)
            ? Result<FloatValue>.Create(new InvalidValueError(value + " is not valid for FloatValue"))
            : Result<FloatValue>.Create(new FloatValue(result));
    }

    public static Result<FloatValue> FromFloat(float value) => new FloatValue(value);
    public float ToFloat() => _v;
    
    public override string ToString() => _v.ToString("F", (IFormatProvider) CultureInfo.InvariantCulture);
}