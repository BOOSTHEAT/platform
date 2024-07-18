using ImpliciX.Language.Core;
using ImpliciX.TimeMath.Computers;
using NUnit.Framework;

namespace ImpliciX.TimeMath.Tests;

public class WindowProcessorTests
{
  [Test]
  public void PushConsecutiveItemsIntoSize1()
  {
    var sut = new WindowProcessor<int>(1);
    Assert.That(
      sut.Push(18),
      Is.EqualTo((18, Option<int>.None()))
    );
    Assert.That(
      sut.Push(23),
      Is.EqualTo((23, Option<int>.Some(18)))
    );
    Assert.That(
      sut.Push(44),
      Is.EqualTo((44, Option<int>.Some(23)))
    );
  }
  
  [Test]
  public void PushConsecutiveItemsIntoSize3()
  {
    var sut = new WindowProcessor<int>(3);
    Assert.That(
      sut.Push(18),
      Is.EqualTo((18, Option<int>.None()))
    );
    Assert.That(
      sut.Push(23),
      Is.EqualTo((23+18, Option<int>.None()))
    );
    Assert.That(
      sut.Push(44),
      Is.EqualTo((44+23+18, Option<int>.None()))
    );
    Assert.That(
      sut.Push(52),
      Is.EqualTo((52+44+23, Option<int>.Some(18)))
    );
    Assert.That(
      sut.Push(93),
      Is.EqualTo((93+52+44, Option<int>.Some(23)))
    );
  }
  
}