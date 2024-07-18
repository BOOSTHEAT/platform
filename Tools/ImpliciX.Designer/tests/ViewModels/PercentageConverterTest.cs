using System;
using System.Globalization;
using Avalonia.Data;
using ImpliciX.Designer.ViewModels;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels;

[TestFixture]
public class PercentageConverterTest
{
  private readonly PercentageConverter converter = new ();

  [Test]
  public void InputNonNumerShouldThrowInvalidCastException()
  {
    var input = "a";
    var res = converter.ConvertBack(
      input,
      null,
      null,
      CultureInfo.InvariantCulture
    );
    Check.That(res).IsNotNull();
    Check.That(res).IsInstanceOf<BindingNotification>();
    Check.That((res as BindingNotification).Error).IsNotNull();
    var error = (res as BindingNotification).Error;
    //Check.That((res as BindingNotification).ErrorType).IsEqualTo(typeof(BindingErrorType));
    Check.That(error).IsInstanceOf<InvalidCastException>();
  }

  [Test]
  public void InputNumberValueShouldBeDivideBy100()
  {
    var input = "50.00 %";
    var res = converter.ConvertBack(
      input,
      null,
      null,
      CultureInfo.InvariantCulture
    );
    Check.That(res).IsNotNull();
    Check.That(res).IsEqualTo("0.50");
  }

  [Test]
  public void NumberValueShouldBeMultiplyBy100InInputString()
  {
    var input = "0.500";
    var res = converter.Convert(
      input,
      null,
      null,
      CultureInfo.InvariantCulture
    );
    Check.That(res).IsNotNull();
    Check.That(res).IsEqualTo("50.00 %");
  }
}
