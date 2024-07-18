using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Subjects;
using ImpliciX.Designer.Features;
using ImpliciX.Designer.Tests.Helpers;
using ImpliciX.Designer.ViewModels;
using Moq;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.Features;

public class SharedFeaturesTests
{
  [Test]
  public void OpenDeviceDefinitionMenuIsInitializedWithPreviousPaths()
  {
    var (paths, sut) = CreateOpenDeviceDefinitionMenu();

    Assert.That(
      sut.Items.Select(x => x.Text),
      Is.EqualTo(new[] {"Open...", "-", "latest", "paths"}));
  }

  [Test]
  public void OpenDeviceDefinitionMenuContainsPreviousPaths()
  {
    var (paths, sut) = CreateOpenDeviceDefinitionMenu();

    paths.OnNext(new[] {"foo", "bar"});

    Assert.That(
      sut.Items.Select(x => x.Text),
      Is.EqualTo(new[] {"Open...", "-", "foo", "bar"}));
  }

  [Test]
  public void OpenDeviceDefinitionMenuUpdatesPreviousPaths()
  {
    var (paths, sut) = CreateOpenDeviceDefinitionMenu();

    paths.OnNext(new[] {"foo", "bar"});
    paths.OnNext(new[] {"qix"});

    Assert.That(
      sut.Items.Select(x => x.Text),
      Is.EqualTo(new[] {"Open...", "-", "qix"}));
  }

  private static (Subject<IEnumerable<string>> paths, MenuItemViewModel sut) CreateOpenDeviceDefinitionMenu()
  {
    var feature = new Mock<ILocalFeature>();
    var window = new Mock<IMainWindow>();
    feature.Setup(x => x.Window).Returns(window.Object);
    window.Setup(x => x.LatestPreviousDeviceDefinitionPaths).Returns(new[] {"latest", "paths"});
    var paths = window.SetupObservable(x => x.PreviousDeviceDefinitionPaths);
    var sut = feature.Object.OpenDeviceDefinitionMenu();
    feature.NotifyPropertyChanged("Window");
    return (paths, sut);
  }

  public interface ILocalFeature : IFeatures, INotifyPropertyChanged
  {
  }
}
