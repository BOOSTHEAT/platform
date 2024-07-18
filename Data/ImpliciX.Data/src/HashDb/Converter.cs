using System;
using System.Globalization;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.HashDb;

public static class Converter
{
  public static Result<object> FromString(Type targetType, string strValue)
  {
    try
    {
      if (targetType.IsAssignableTo(typeof(Enum)))
        return Enum.Parse(targetType, strValue);
      if (targetType.IsAssignableTo(typeof(float)) || targetType.IsAssignableTo(typeof(IFloat)))
        return float.Parse(strValue, CultureInfo.InvariantCulture);
      throw new NotSupportedException($"cannot convert string to {targetType}");
    }
    catch (Exception e)
    {
      return Result<object>.Create(new Error(nameof(FromString), e.Message));
    }
  }

  public static Result<string> ToString(Type sourceType, object value)
  {
    try
    {
      if (sourceType.IsAssignableTo(typeof(Enum)))
        return Enum.GetName(sourceType, Convert.ToInt32(value));
      if (sourceType.IsAssignableTo(typeof(float)) || sourceType.IsAssignableTo(typeof(IFloat)))
        return ((float)value).ToString(CultureInfo.InvariantCulture);
      throw new NotSupportedException($"Cannot convert {sourceType} to string");
    }
    catch (Exception e)
    {
      return Result<string>.Create(new Error(nameof(ToString), e.Message));
    }
  }
}
