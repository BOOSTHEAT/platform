using System.Linq;
using DynamicData;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Control;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using Moq;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels.System;

public class SystemViewModelTestsBase
{
  [SetUp]
  public void Init()
  {
    Definition = new Mock<IDeviceDefinition>();
    _concierge = new Mock<ILightConcierge>();
    _app = new Mock<IRemoteDevice>();
    _store = new Mock<ISessionService>();
    _concierge.Setup(x => x.RemoteDevice).Returns(_app.Object);
    _concierge.Setup(x => x.Session).Returns(_store.Object);
    var appSourceCache = new SourceCache<ImpliciXProperty, string>(x => x.Urn);
    _app.Setup(x => x.Properties).Returns(appSourceCache);
    var storeCache = new SourceCache<ImpliciXProperty, string>(x => x.Urn);
    _store.Setup(x => x.Properties).Returns(storeCache);
  }
  
  [Test]
  public void EmptyApp()
  {
    Definition.Setup(x => x.Name).Returns("FooApp");
    Definition.Setup(x => x.Version).Returns("0.0.0");
    CreateSut();
    Assert.That(Sut.Title, Is.EqualTo("FooApp 0.0.0"));
    AssertEqualTrees(SutModelsTree, CreateAppTree("FooApp 0.0.0",Group()));
  }

  protected SystemViewModel Sut;
  protected NamedTree SutModelsTree => Sut.Models.First();
  protected Mock<IDeviceDefinition> Definition;
  private Mock<ILightConcierge> _concierge;
  private Mock<IRemoteDevice> _app;
  private Mock<ISessionService> _store;

  protected void CreateSut()
  {
    Sut = new SystemViewModel(_concierge.Object, new []{new NamedTree(null)}, CreateSubSystemViewModel);
    Sut.Device = Definition.Object;
  }

  protected IMetricDefinition CreateMetric(string urn)
  {
    var def = new Mock<IMetricDefinition>();
    var builder = new Mock<IMetricBuilder>();
    var metric = new Mock<IMetric>();
    metric.Setup(x => x.TargetUrn).Returns(Urn.BuildUrn(urn));
    builder.Setup(x => x.Build<IMetric>()).Returns(metric.Object);
    def.Setup(x => x.Builder).Returns(builder.Object);
    return def.Object;
  }

  protected ISubSystemDefinition CreateSubSystemDefinition(string urn)
  {
    var ssd = new Mock<ISubSystemDefinition>();
    ssd.Setup(x => x.ID).Returns(Urn.BuildUrn(urn));
    return ssd.Object;
  }

  protected SubSystemViewModel CreateSubSystemViewModel(ISubSystemDefinition def)
  {
    return new SubSystemViewModel(def.ID, _concierge.Object, (action, action1, arg3) => { });
  }

  protected NamedTree CreateAppTree(string appname, string[] subsystems = null, string[] metrics = null) =>
    CreateTreeOfTrees(
      appname, 
      CreateTree("Control & Command", subsystems ?? new string[]{}),
      CreateTree("Metrics", metrics ?? new string[]{})
      );

  protected string[] Group(params string[] subsystems) => subsystems;
  
  private NamedTree CreateTreeOfTrees(string name, params NamedTree[] subtrees) =>
    new NamedTree(new NamedModel(name), subtrees);
  
  private NamedTree CreateTree(string name, params string[] children) =>
    new NamedTree(new NamedModel(name), children.Select(c => new NamedModel(c)));

  protected void AssertEqualTrees(NamedTree t1, NamedTree t2)
  {
    Assert.That(t1.Parent.Name, Is.EqualTo(t2.Parent.Name), "Trees parent names");
    Assert.That(t1.Children.Count(), Is.EqualTo(t2.Children.Count()), "Trees children count");
    foreach (var pair in t1.Children.Zip(t2.Children))
      AssertEqualTrees(pair.First, pair.Second);
  }
}