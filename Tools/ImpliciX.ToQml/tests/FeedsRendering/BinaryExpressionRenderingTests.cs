using System;
using System.Collections.Generic;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.FeedsRendering;

public class BinaryExpressionRenderingTests
{
  private static TestCaseData[] _binaryExpressionsExpectations2 = new[]
  {
    new TestCaseData(
      new LowerThan { Left = new Feed1(), Right = new Feed2() },
      "Id_Feed1_is_lower_than_Id_Feed2",
      "ValueOf_Feed1_RawValue < ValueOf_Feed2_RawValue",
      "ValueOf_Feed1_foo_RawValue < ValueOf_Feed2_foo_RawValue"
    ),
    new TestCaseData(
      new GreaterThan { Left = new Feed1(), Right = new Feed2() },
      "Id_Feed1_is_greater_than_Id_Feed2",
      "ValueOf_Feed1_RawValue > ValueOf_Feed2_RawValue",
      "ValueOf_Feed1_foo_RawValue > ValueOf_Feed2_foo_RawValue"
    ),
    new TestCaseData(
      new EqualTo { Left = new Feed1(), Right = new Feed2() },
      "Id_Feed1_is_equal_to_Id_Feed2",
      "ValueOf_Feed1_RawValue == ValueOf_Feed2_RawValue",
      "ValueOf_Feed1_foo_RawValue == ValueOf_Feed2_foo_RawValue"
    ),
    new TestCaseData(
      new NotEqualTo { Left = new Feed1(), Right = new Feed2() },
      "Id_Feed1_is_not_equal_to_Id_Feed2",
      "ValueOf_Feed1_RawValue != ValueOf_Feed2_RawValue",
      "ValueOf_Feed1_foo_RawValue != ValueOf_Feed2_foo_RawValue"
    ),
  };
  [TestCaseSource(nameof(_binaryExpressionsExpectations2))]
  public void CheckId(BinaryExpression binaryExpression, string expectedId, string expectedValueInCache, string expectedValueOutOfCache)
  {
    Assert.That(_renderers2.Id(binaryExpression), Is.EqualTo(expectedId));
  }
    
  [TestCaseSource(nameof(_binaryExpressionsExpectations2))]
  public void CheckGetValue(BinaryExpression binaryExpression, string expectedId, string expectedValueInCache, string expectedValueOutOfCache)
  {
    Assert.That(_renderers2.GetValueOf(binaryExpression.InCache()), Is.EqualTo(expectedValueInCache));
    Assert.That(_renderers2.GetValueOf(binaryExpression.OutOfCache("foo")), Is.EqualTo(expectedValueOutOfCache));
  }
    
  [TestCaseSource(nameof(_binaryExpressionsExpectations2))]
  public void CheckSetValue(BinaryExpression binaryExpression, string expectedId, string expectedValueInCache, string expectedValueOutOfCache)
  {
    Assert.Throws<NotSupportedException>(() => _renderers2.SetValueOf(binaryExpression.InCache(), "whatever"));
    Assert.Throws<NotSupportedException>(() => _renderers2.SetValueOf(binaryExpression.OutOfCache("foo"), "whatever"));
  }

  [TestCaseSource(nameof(_binaryExpressionsExpectations2))]
  public void CheckDeclare2(BinaryExpression binaryExpression, string expectedId, string expectedValueInCache, string expectedValueOutOfCache)
  {
    Assert.That(
      _renderers2.Declare(binaryExpression.InCache()),
      Is.EqualTo($"property var {expectedId}: {expectedValueInCache}"));
    Assert.That(
      _renderers2.Declare(binaryExpression.OutOfCache("foo")),
      Is.EqualTo($"property var {expectedId}: {expectedValueOutOfCache}"));
  }

  [SetUp]
  public void Initialize()
  {
    _renderers2 = new Dictionary<Type, IRenderFeed>();

    _renderers2[typeof(LowerThan)] = new LowerThanRenderer(_renderers2);
    _renderers2[typeof(GreaterThan)] = new GreaterThanRenderer(_renderers2);
    _renderers2[typeof(EqualTo)] = new EqualToRenderer(_renderers2);
    _renderers2[typeof(NotEqualTo)] = new NotEqualToRenderer(_renderers2);
    _renderers2[typeof(Feed1)] = new FeedRenderingStub<Feed1>();
    _renderers2[typeof(Feed2)] = new FeedRenderingStub<Feed2>();
  }
  
  private Dictionary<Type, IRenderFeed> _renderers2;
}