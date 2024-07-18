using System;
using ImpliciX.Designer.ViewModels;
using ImpliciX.Language.Control;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels.System;

public class ControlCommandSubTreeTests : SystemViewModelTestsBase
{

  [Test]
  public void SingleSubSystem()
  {
    Definition.Setup(x => x.Name).Returns("FooApp");
    Definition.Setup(x => x.Version).Returns("6.6.6");
    Definition.Setup(x => x.SubSystemDefinitions).Returns(new ISubSystemDefinition[]
    {
      CreateSubSystemDefinition("foo")
    });
    CreateSut();
    Assert.That(Sut.Title, Is.EqualTo("FooApp 6.6.6"));
    AssertEqualTrees(SutModelsTree, CreateAppTree("FooApp 6.6.6", Group("foo")));
  }

  [Test]
  public void FailToLoadSubSystem()
  {
    Definition.Setup(x => x.Name).Returns("FooApp");
    Definition.Setup(x => x.Version).Returns("6.6.6");
    Definition.Setup(x => x.SubSystemDefinitions).Returns(new ISubSystemDefinition[]
    {
      CreateSubSystemDefinition("foo")
    });
    Sut = new SystemViewModel(null, null, def => throw new Exception("YOLO"));
    Sut.Device = Definition.Object;
    Assert.That(Sut.Title, Is.EqualTo("FooApp 6.6.6"));
    AssertEqualTrees(SutModelsTree, CreateAppTree("FooApp 6.6.6", Group("foo:YOLO")));
  }

  [Test]
  public void MultipleSubSystems()
  {
    Definition.Setup(x => x.Name).Returns("FooApp");
    Definition.Setup(x => x.Version).Returns("6.6.6");
    Definition.Setup(x => x.SubSystemDefinitions).Returns(new ISubSystemDefinition[]
    {
      CreateSubSystemDefinition("foo"),
      CreateSubSystemDefinition("bar"),
      CreateSubSystemDefinition("qix")
    });
    CreateSut();
    Assert.That(Sut.Title, Is.EqualTo("FooApp 6.6.6"));
    AssertEqualTrees(SutModelsTree, CreateAppTree("FooApp 6.6.6", Group("foo", "bar", "qix")));
  }

  [Test]
  public void SubSystemsWithSameName()
  {
    Definition.Setup(x => x.Name).Returns("FooApp");
    Definition.Setup(x => x.Version).Returns("6.6.6");
    Definition.Setup(x => x.SubSystemDefinitions).Returns(new ISubSystemDefinition[]
    {
      CreateSubSystemDefinition("foo"),
      CreateSubSystemDefinition("foo"),
      CreateSubSystemDefinition("bar"),
    });
    CreateSut();
    Assert.That(Sut.Title, Is.EqualTo("FooApp 6.6.6"));
    AssertEqualTrees(SutModelsTree, CreateAppTree("FooApp 6.6.6", Group("bar", "foo:ERROR duplicate definitions")));
  }

  [Test]
  public void TreeOfSubSystems()
  {
    Definition.Setup(x => x.Name).Returns("FooApp");
    Definition.Setup(x => x.Version).Returns("6.6.6");
    Definition.Setup(x => x.SubSystemDefinitions).Returns(new ISubSystemDefinition[]
    {
      CreateSubSystemDefinition("foo"),
      CreateSubSystemDefinition("bar"),
      CreateSubSystemDefinition("qix"),
      CreateSubSystemDefinition("foo:fizz"),
      CreateSubSystemDefinition("foo:buzz"),
    });
    CreateSut();
    Assert.That(Sut.Title, Is.EqualTo("FooApp 6.6.6"));
    AssertEqualTrees(SutModelsTree, CreateAppTree("FooApp 6.6.6", Group("foo", "foo:fizz", "foo:buzz", "bar", "qix")));
  }

  [Test]
  public void FailToLoadSomeSubSystemsInATreeOfSubSystems()
  {
    Definition.Setup(x => x.Name).Returns("FooApp");
    Definition.Setup(x => x.Version).Returns("6.6.6");
    Definition.Setup(x => x.SubSystemDefinitions).Returns(new ISubSystemDefinition[]
    {
      CreateSubSystemDefinition("foo"),
      CreateSubSystemDefinition("bar1"),
      CreateSubSystemDefinition("qix"),
      CreateSubSystemDefinition("foo:fizz1"),
      CreateSubSystemDefinition("foo:buzz"),
    });
    Sut = new SystemViewModel(null, null, def => def.ID.Value.EndsWith("1") ? throw new Exception($"Error in {def.ID.Value}") : CreateSubSystemViewModel(def));
    Sut.Device = Definition.Object;
    Assert.That(Sut.Title, Is.EqualTo("FooApp 6.6.6"));
    AssertEqualTrees(SutModelsTree, CreateAppTree("FooApp 6.6.6", Group("foo", "foo:fizz1:Error in foo:fizz1", "foo:buzz", "bar1:Error in bar1", "qix")));
  }

}