using ImpliciX.Language.Metrics;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels.System;

public class MetricsSubTreeTests : SystemViewModelTestsBase
{
  [Test]
  public void SingleMetric()
  {
    Definition.Setup(x => x.Name).Returns("FooApp");
    Definition.Setup(x => x.Version).Returns("1.2.3");
    Definition.Setup(x => x.Metrics).Returns(  new MetricsModuleDefinition
    {
      Metrics = new []
      {
        CreateMetric("foo")
      }
    });
    CreateSut();
    Assert.That(Sut.Title, Is.EqualTo("FooApp 1.2.3"));
    AssertEqualTrees(SutModelsTree, CreateAppTree("FooApp 1.2.3", metrics:Group("foo")));
  }
  
  
  [Test]
  public void MultipleMetrics()
  {
    Definition.Setup(x => x.Name).Returns("FooApp");
    Definition.Setup(x => x.Version).Returns("1.2.3");
    Definition.Setup(x => x.Metrics).Returns(  new MetricsModuleDefinition
    {
      Metrics = new []
      {
        CreateMetric("foo"),
        CreateMetric("bar"),
        CreateMetric("qix")
      }
    });
    CreateSut();
    Assert.That(Sut.Title, Is.EqualTo("FooApp 1.2.3"));
    AssertEqualTrees(SutModelsTree, CreateAppTree("FooApp 1.2.3", metrics:Group("foo", "bar", "qix")));
  }

  [Test]
  public void TreeOfMetrics()
  {
    Definition.Setup(x => x.Name).Returns("FooApp");
    Definition.Setup(x => x.Version).Returns("1.2.3");
    Definition.Setup(x => x.Metrics).Returns(  new MetricsModuleDefinition
    {
      Metrics = new []
      {
        CreateMetric("foo"),
        CreateMetric("bar"),
        CreateMetric("qix"),
        CreateMetric("foo:fizz"),
        CreateMetric("foo:buzz")
      }
    });
    CreateSut();
    Assert.That(Sut.Title, Is.EqualTo("FooApp 1.2.3"));
    AssertEqualTrees(SutModelsTree, CreateAppTree("FooApp 1.2.3", metrics:Group("foo", "foo:fizz", "foo:buzz", "bar", "qix")));
  }

}