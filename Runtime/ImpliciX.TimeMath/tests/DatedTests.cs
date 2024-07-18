using System;
using ImpliciX.TimeMath.Computers;
using NUnit.Framework;

namespace ImpliciX.TimeMath.Tests;

public class DatedTests
{
  [Test]
  public void HasValueAndStartAndEndDates()
  {
    var sut = new Dated<int>(23, TimeSpan.FromDays(1), TimeSpan.FromDays(2));
    Assert.That(sut.Value, Is.EqualTo(23));
    Assert.That(sut.Start, Is.EqualTo(TimeSpan.FromDays(1)));
    Assert.That(sut.End, Is.EqualTo(TimeSpan.FromDays(2)));
  }

  [Test]
  public void AdditionHasValueAndStartAndEndDates()
  {
    var d1 = new Dated<int>(23, TimeSpan.FromDays(1), TimeSpan.FromDays(2));
    var d2 = new Dated<int>(49, TimeSpan.FromDays(2), TimeSpan.FromDays(3));
    var sut = d1 + d2;
    Assert.That(sut.Value, Is.EqualTo(23 + 49));
    Assert.That(sut.Start, Is.EqualTo(TimeSpan.FromDays(1)));
    Assert.That(sut.End, Is.EqualTo(TimeSpan.FromDays(3)));
  }

  [Test]
  public void CannotAddIfStartAndEndDatesDoNotMatch()
  {
    var d1 = new Dated<int>(23, TimeSpan.FromDays(1), TimeSpan.FromDays(2));
    var d2 = new Dated<int>(49, TimeSpan.FromDays(3), TimeSpan.FromDays(4));
    Assert.Throws<ArgumentException>(() =>
    {
      var shouldFail = d1 + d2;
    }, "Incompatible end 2.00:00:00 and start 3.00:00:00 dates");
  }

  [Test]
  public void SubtractionHasValueAndStartAndEndDates()
  {
    var d1 = new Dated<int>(23, TimeSpan.FromDays(1), TimeSpan.FromDays(2));
    var d2 = new Dated<int>(49, TimeSpan.FromDays(1), TimeSpan.FromDays(3));
    var sut = d2 - d1;
    Assert.That(sut.Value, Is.EqualTo(49 - 23));
    Assert.That(sut.Start, Is.EqualTo(TimeSpan.FromDays(2)));
    Assert.That(sut.End, Is.EqualTo(TimeSpan.FromDays(3)));
  }

  [Test]
  public void CannotSubtractIfStartDatesDoNotMatch()
  {
    var d1 = new Dated<int>(23, TimeSpan.FromDays(1), TimeSpan.FromDays(2));
    var d2 = new Dated<int>(49, TimeSpan.FromDays(2), TimeSpan.FromDays(3));

    Assert.Throws<ArgumentException>(() =>
    {
      var shouldFail = d2 - d1;
    }, "Incompatible start 1.00:00:00 and end 2.00:00:00 dates");
  }
}