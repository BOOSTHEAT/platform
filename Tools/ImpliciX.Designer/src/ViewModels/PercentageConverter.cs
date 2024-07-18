using System;
using System.Globalization;

namespace ImpliciX.Designer.ViewModels;

public class PercentageConverter : FloatConverter
{
  public override object Convert(
    object value,
    Type targetType,
    object parameter,
    CultureInfo culture
  )
  {
    return base.Convert(
      value,
      targetType,
      parameter ?? "0.00#### %",
      culture
    ) ;
  }

  public override object ConvertBack(
    object value,
    Type targetType,
    object parameter,
    CultureInfo culture
  )
  {
    if (value == null ) return null;
    var s = value.ToString()!.Replace(
      "%",
      ""
    );
    return  base.ConvertBack(
      s,
      targetType,
      parameter,
      culture
    );
  }

  protected private override float InputToFloat(
    string s
  )
  {
    return base.InputToFloat(s) / 100;
  }
}
