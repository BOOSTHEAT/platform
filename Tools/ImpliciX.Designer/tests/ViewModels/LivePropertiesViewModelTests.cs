using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Subjects;
using DynamicData;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using Moq;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels;

public class LivePropertiesViewModelTests
{
  private Subject<Option<IDeviceDefinition>> _devices;
  private SourceCache<ImpliciXProperty, string> _properties;
  private LivePropertiesViewModel _sut;

  [SetUp]
  public void Init()
  {
    var remoteDevice = new Mock<IRemoteDevice>();
    remoteDevice.Setup(x => x.IsConnected).Returns(new Subject<bool>());
    var session = new Mock<ISessionService>();
    _properties = new SourceCache<ImpliciXProperty, string>(x => x.Urn);
    session.Setup(x => x.Properties).Returns(_properties);
    var applications = new Mock<IManageApplicationDefinitions>();
    _devices = new Subject<Option<IDeviceDefinition>>();
    applications.Setup(x => x.Devices).Returns(_devices);
    var concierge = new Mock<ILightConcierge>();
    concierge.Setup(x => x.Session).Returns(session.Object);
    concierge.Setup(x => x.RemoteDevice).Returns(remoteDevice.Object);
    concierge.Setup(x => x.Applications).Returns(applications.Object);
    _sut = new LivePropertiesViewModel(concierge.Object);
  }

  [Test]
  public void NoPropertiesDisplayedBeforeAnythingIsLoaded()
  {
    Assert.That(
      _sut.Items,
      Is.Empty
    );
  }

  private static (string Value, string Summary, bool IsEditable ) PropertyNameAndValue(
    LivePropertyViewModel p
  )
  {
    return (
      p.Urn.Value,
      ((LiveSingleDataViewModel) p).Summary,
      p.IsEditable
    );
  }

  private static IDeviceDefinition MockDeviceDefinition(
    string[] urns
  )
  {
    var dd = new Mock<IDeviceDefinition>();
    var dic = urns.ToDictionary(
      x => x,
      x => (Urn) UserSettingUrn<Text10>.Build(x)
    );
    dd.Setup(x => x.Urns).Returns(dic);
    return dd.Object;
  }

  #region device definition without remote session

  [Test]
  public void PropertiesAreDisplayedWhenDeviceDefinitionIsLoaded()
  {
    var urns = new[] { "foo", "bar" };
    var dd = MockDeviceDefinition(urns);

    _devices.OnNext(Option<IDeviceDefinition>.Some(dd));

    Assert.That(
      _sut.Items.Select(PropertyNameAndValue),
      Is.EquivalentTo(new (string, string, bool)[] { ("foo", null, true), ("bar", null, true) })
    );
  }

  [Test]
  public void PropertiesAreHiddenWhenDeviceDefinitionIsUnLoaded()
  {
    var urns = new[] { "foo", "bar" };
    var dd = MockDeviceDefinition(urns);

    _devices.OnNext(Option<IDeviceDefinition>.Some(dd));
    _devices.OnNext(Option<IDeviceDefinition>.None());

    Assert.That(
      _sut.Items,
      Is.Empty
    );
  }

  [Test]
  public void PropertiesAreFiltered()
  {
    var urns = new[] { "foo", "bar", "fizz", "buzz" };
    var dd = MockDeviceDefinition(urns);

    _devices.OnNext(Option<IDeviceDefinition>.Some(dd));
    _sut.Search = "f";

    Assert.That(
      _sut.Items.Select(p => p.Urn.Value),
      Is.EquivalentTo(new[] { "foo", "fizz" })
    );
  }

  [Test]
  public void PropertiesAreFilteredAgain()
  {
    var urns = new[] { "foo", "bar", "fizz", "buzz" };
    var dd = MockDeviceDefinition(urns);

    _devices.OnNext(Option<IDeviceDefinition>.Some(dd));
    _sut.Search = "f";
    _sut.Search = "zz";

    Assert.That(
      _sut.Items.Select(p => p.Urn.Value),
      Is.EquivalentTo(new[] { "fizz", "buzz" })
    );
  }

  [Test]
  public void PropertiesAreUnFiltered()
  {
    var urns = new[] { "foo", "bar", "fizz", "buzz" };
    var dd = MockDeviceDefinition(urns);

    _devices.OnNext(Option<IDeviceDefinition>.Some(dd));
    _sut.Search = "f";
    _sut.Search = "zz";
    _sut.Search = "";

    Assert.That(
      _sut.Items.Select(p => p.Urn.Value),
      Is.EquivalentTo(urns)
    );
  }

  [Test]
  public void PropertiesAreDynamicallyFiltered()
  {
    var urns = new[] { "foo", "bar", "fizz", "buzz" };
    var dd = MockDeviceDefinition(urns);

    _devices.OnNext(Option<IDeviceDefinition>.Some(dd));
    _sut.ContextFilter = s => s[0] == 'f';

    Assert.That(
      _sut.Items.Select(p => p.Urn.Value),
      Is.EquivalentTo(new[] { "foo", "fizz" })
    );
  }

  [Test]
  public void PropertiesAreDynamicallyFilteredAgain()
  {
    var urns = new[] { "foo", "bar", "fizz", "buzz" };
    var dd = MockDeviceDefinition(urns);

    _devices.OnNext(Option<IDeviceDefinition>.Some(dd));
    _sut.ContextFilter = s => s[0] == 'f';
    _sut.ContextFilter = s => new string(s.Skip(2).ToArray()) == "zz";

    Assert.That(
      _sut.Items.Select(p => p.Urn.Value),
      Is.EquivalentTo(new[] { "fizz", "buzz" })
    );
  }

  [Test]
  public void PropertiesAreDynamicallyUnFiltered()
  {
    var urns = new[] { "foo", "bar", "fizz", "buzz" };
    var dd = MockDeviceDefinition(urns);

    _devices.OnNext(Option<IDeviceDefinition>.Some(dd));
    _sut.ContextFilter = s => s[0] == 'f';
    _sut.ContextFilter = s => new string(s.Skip(2).ToArray()) == "zz";

    Assert.That(
      _sut.Items.Select(p => p.Urn.Value),
      Is.EquivalentTo(new[] { "fizz", "buzz" })
    );
  }

  [Test]
  public void PropertiesAreContextuallyFiltered()
  {
    var urns = new[] { "foo", "bar", "fizz", "buzz" };
    var dd = MockDeviceDefinition(urns);

    _devices.OnNext(Option<IDeviceDefinition>.Some(dd));
    _sut.ContextURNs = new[] { "foo", "fizz", "flop" };

    Assert.That(
      _sut.Items.Select(p => p.Urn.Value),
      Is.EquivalentTo(new[] { "foo", "fizz" })
    );
  }

  [Test]
  public void PropertiesAreContextuallyUnFiltered()
  {
    var urns = new[] { "foo", "bar", "fizz", "buzz" };
    var dd = MockDeviceDefinition(urns);

    _devices.OnNext(Option<IDeviceDefinition>.Some(dd));
    _sut.ContextURNs = new[] { "foo", "fizz", "flop" };
    _sut.ContextURNs = new[] { "qozz", "fizz", "buzz" };
    _sut.ContextURNs = Enumerable.Empty<string>();

    Assert.That(
      _sut.Items.Select(p => p.Urn.Value),
      Is.EquivalentTo(urns)
    );
  }

  [Test]
  public void PropertiesFilterIsAdjustedWhenDeviceDefinitionChanges()
  {
    _devices.OnNext(Option<IDeviceDefinition>.Some(MockDeviceDefinition(new[] { "foo", "bar", "fizz", "buzz" })));
    _sut.Search = "f";
    _devices.OnNext(Option<IDeviceDefinition>.Some(MockDeviceDefinition(new[] { "foo", "bar", "fazz", "bozz" })));

    Assert.That(
      _sut.Items.Select(p => p.Urn.Value),
      Is.EquivalentTo(new[] { "foo", "fazz" })
    );
  }

  #endregion

  #region remote session without device definition

  [Test]
  public void PropertiesAreDisplayedWithValueWhenRemoteSessionIsActive()
  {
    var urns = new[] { "foo", "bar" };

    _properties.AddOrUpdate(
      urns.Select(
        u => new ImpliciXProperty(
          u,
          $"{u}_value"
        )
      )
    );

    Assert.That(
      _sut.Items.Select(PropertyNameAndValue),
      Is.EquivalentTo(new[] { ("foo", "foo_value", false), ("bar", "bar_value", false) })
    );
  }

  [Test]
  public void PropertiesAreHiddenWhenRemoteSessionIsInactive()
  {
    var urns = new[] { "foo", "bar" };

    _properties.AddOrUpdate(
      urns.Select(
        u => new ImpliciXProperty(
          u,
          $"{u}_value"
        )
      )
    );
    _properties.Clear();

    Assert.That(
      _sut.Items,
      Is.Empty
    );
  }

  [Test]
  public void PropertiesAreUpdatedWithValueWhenRemoteSessionIsActive()
  {
    var urns = new[] { "foo", "bar" };

    _properties.AddOrUpdate(
      urns.Select(
        u => new ImpliciXProperty(
          u,
          $"{u}_value"
        )
      )
    );
    _properties.Edit(
      updater =>
      {
        foreach (var item in updater.Items)
          updater.AddOrUpdate(
            new ImpliciXProperty(
              item.Urn,
              item.Value + "2"
            )
          );
      }
    );

    Assert.That(
      _sut.Items.Select(PropertyNameAndValue),
      Is.EquivalentTo(new[] { ("foo", "foo_value2", false), ("bar", "bar_value2", false) })
    );
  }

  [Test]
  public void PropertiesAreFilteredWithRemoteSession()
  {
    var urns = new[] { "foo", "bar", "fizz", "buzz" };

    _properties.AddOrUpdate(
      urns.Select(
        u => new ImpliciXProperty(
          u,
          $"{u}_value"
        )
      )
    );
    _sut.Search = "f";

    Assert.That(
      _sut.Items.Select(PropertyNameAndValue),
      Is.EquivalentTo(new[] { ("foo", "foo_value", false), ("fizz", "fizz_value", false) })
    );
  }

  #endregion

  #region with device definition and remote session

  [Test]
  public void PropertiesFromRemoteSessionNotPresentInDeviceDefinitionAreDisplayedButNotEditable()
  {
    _devices.OnNext(Option<IDeviceDefinition>.Some(MockDeviceDefinition(new[] { "foo", "fizz" })));
    _properties.AddOrUpdate(
      new[] { "fizz", "buzz" }.Select(
        u => new ImpliciXProperty(
          u,
          $"{u}_value"
        )
      )
    );

    Assert.That(
      _sut.Items.Select(PropertyNameAndValue),
      Is.EquivalentTo(new[] { ("fizz", "fizz_value", true), ("buzz", "buzz_value", false), ("foo", null, true) })
    );
  }

  [Test]
  public void PropertiesStayWithoutValuesWhenRemoteSessionIsNoLongerActive()
  {
    _devices.OnNext(Option<IDeviceDefinition>.Some(MockDeviceDefinition(new[] { "foo", "fizz" })));
    _properties.AddOrUpdate(
      new[] { "fizz", "buzz" }.Select(
        u => new ImpliciXProperty(
          u,
          $"{u}_value"
        )
      )
    );
    _properties.Clear();

    Assert.That(
      _sut.Items.Select(PropertyNameAndValue),
      Is.EquivalentTo(new (string, string, bool)[] { ("fizz", null, true), ("foo", null, true) })
    );
  }

  [Test]
  public void OnlyPropertiesFromDeviceDefinitionStayWhenRemoteSessionIsInactive()
  {
    _properties.AddOrUpdate(
      new[] { "fizz", "buzz" }.Select(
        u => new ImpliciXProperty(
          u,
          $"{u}_value"
        )
      )
    );
    _devices.OnNext(Option<IDeviceDefinition>.Some(MockDeviceDefinition(new[] { "foo", "fizz" })));
    _properties.Clear();

    Assert.That(
      _sut.Items.Select(PropertyNameAndValue),
      Is.EquivalentTo(new (string, string, bool)[] { ("fizz", null, true), ("foo", null, true) })
    );
  }

  [Test]
  public void OnlyPropertiesFromRemoteSessionStayWhenDeviceDefinitionIsUnloaded()
  {
    _devices.OnNext(Option<IDeviceDefinition>.Some(MockDeviceDefinition(new[] { "foo", "fizz" })));
    _properties.AddOrUpdate(
      new[] { "fizz", "buzz" }.Select(
        u => new ImpliciXProperty(
          u,
          $"{u}_value"
        )
      )
    );
    _devices.OnNext(Option<IDeviceDefinition>.None());

    Assert.That(
      _sut.Items.Select(PropertyNameAndValue),
      Is.EquivalentTo(new [] { ("fizz", "fizz_value", false), ("buzz", "buzz_value", false) })
    );
  }

  [Test]
  public void PercentagePropertiesShouldBeMultiplyByOneHundred()
  {
    var deviceDefinitionMock = new Mock<IDeviceDefinition>();
    var percentageUrn = PropertyUrn<Percentage>.Build(
      "fizz",
      "progress"
    );
    _properties.AddOrUpdate(
      new[]
      {
        new ImpliciXProperty(
          percentageUrn,
          "0.50"
        )
      }
    );
    var dic = new Dictionary<string, Urn>();
    dic.Add(
      percentageUrn.ToString(),
      percentageUrn
    );
    deviceDefinitionMock
      .Setup(x => x.Urns).Returns(dic);
    _devices.OnNext(Option<IDeviceDefinition>.Some(deviceDefinitionMock.Object));

    Assert.That(
      _sut.Items.Select
        // <ValueTuple<string , string , bool >>
        (
          p =>
          {
            return (
              p.Urn.Value,
              float.Parse(
                ((LiveSingleDataViewModel) p).Summary,
                CultureInfo.InvariantCulture
              ).ToString(
                "0.00### %",
                CultureInfo.InvariantCulture
              ),
              p.IsEditable
            );
          }
        ),
      Is.EquivalentTo(new [] { (percentageUrn.ToString(), "50.00 %", false) })
    );
  }

  [Test]
  public void PropertiesFromREmoteSessionBecomeEditableWhenDeviceDefinitionIsLoaded()
  {
    _properties.AddOrUpdate(
      new[] { "fizz", "buzz" }.Select(
        u => new ImpliciXProperty(
          u,
          $"{u}_value"
        )
      )
    );
    _devices.OnNext(Option<IDeviceDefinition>.Some(MockDeviceDefinition(new[] { "foo", "fizz" })));

    Assert.That(
      _sut.Items.Select(PropertyNameAndValue),
      Is.EquivalentTo(new [] { ("fizz", "fizz_value", true), ("buzz", "buzz_value", false), ("foo", null, true) })
    );
  }

  #endregion
}
