using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ImpliciX.Designer.ViewModels;

public class FloatConverter : IValueConverter
{
  //value to textBox
  public virtual object Convert(
    object value,
    Type targetType,
    object parameter,
    CultureInfo culture
  )
  {
    if (value == null || string.IsNullOrWhiteSpace(value.ToString())) return value;
    if (value is not string sourceText)
      return new BindingNotification(
        new InvalidCastException(value + " not valid"),
        BindingErrorType.Error
      );
    if ((parameter ?? "0.00####") is not string formatString)
      return new BindingNotification(
        new InvalidCastException(parameter + " not valid"),
        BindingErrorType.Error
      );
    if (string.IsNullOrWhiteSpace(sourceText)) return value;
    var formattingString = formatString.Replace(
      "’",
      ""
    ) ;

    var f = ValueToFloat(sourceText);
    return f.ToString(
      formattingString,
      culture
    ) ;
  }

  //textBox to value
  public virtual object ConvertBack(
    object value,
    Type targetType,
    object parameter,
    CultureInfo culture
  )
  {
    if (value == null ) return null;
    var s = value.ToString();
    if ( string.IsNullOrWhiteSpace(s)) return value;
    try
    {
      var f = InputToFloat(s);
      var res = f.ToString(
        "0.00####",
        CultureInfo.InvariantCulture
      );
      return res;
    }
    catch (Exception e)
    {
      return new BindingNotification(
        new InvalidCastException(e.Message),
        BindingErrorType.Error
      );
    }
  }

  private  float ValueToFloat(
    string value
  )
  {
    return float.Parse(
      value,
      CultureInfo.InvariantCulture
    );
  }

  protected private virtual float InputToFloat(
    string s
  )
  {
    return float.Parse(
      s,
      CultureInfo.InvariantCulture
    );
  }
}
