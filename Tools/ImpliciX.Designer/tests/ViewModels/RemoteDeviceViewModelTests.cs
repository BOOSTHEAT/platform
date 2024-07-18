using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using Moq;
using NFluent;
using NUnit.Framework;
using Check = NFluent.Check;

namespace ImpliciX.Designer.Tests.ViewModels;

public class RemoteDeviceViewModelTests
{
  private Mock<ILightConcierge> _concierge;
  private Subject<ITargetSystem> _targetSystems;
  private Subject<IRemoteDeviceDefinition> _deviceDefinitions;
  private RemoteDeviceViewModel _sut;
  private List<string> _propertyChanges;
  private Mock<IUser> _user;
  private string _requestedNewPermanentSetup;
  private string _requestedNewTemporarySetup;
  private IUser.Box _box;

  [Test]
  public void DisplayTargetSystemInformation()
  {
    _targetSystems.OnNext(CreateTargetSystem("the_board", OS.linux, Architecture.arm, "RPi "));
    Check.That(_propertyChanges).Contains(nameof(_sut.BoardName), nameof(_sut.OperatingSystem), nameof(_sut.Architecture));
    Check.That(_sut.BoardName).IsEqualTo("the_board");
    Check.That(_sut.OperatingSystem).IsEqualTo("linux");
    Check.That(_sut.Architecture).IsEqualTo("arm RPi");
  }

  [Test]
  public void DisplayTargetSystemInformationWithoutSpecificHardware()
  {
    _targetSystems.OnNext(CreateTargetSystem("the_board", OS.linux, Architecture.x64, null));
    Check.That(_propertyChanges).Contains(nameof(_sut.BoardName), nameof(_sut.OperatingSystem), nameof(_sut.Architecture));
    Check.That(_sut.BoardName).IsEqualTo("the_board");
    Check.That(_sut.OperatingSystem).IsEqualTo("linux");
    Check.That(_sut.Architecture).IsEqualTo("x64");
  }

  [Test]
  public void DisplayMissingTargetSystemInformation()
  {
    _targetSystems.OnNext(CreateTargetSystem("the_board", OS.linux, Architecture.arm, "RPi "));
    _propertyChanges.Clear();
    _targetSystems.OnNext(null);
    Check.That(_propertyChanges).Contains(nameof(_sut.BoardName), nameof(_sut.OperatingSystem), nameof(_sut.Architecture));
    Check.That(_sut.BoardName).IsEqualTo("");
    Check.That(_sut.OperatingSystem).IsEqualTo("");
    Check.That(_sut.Architecture).IsEqualTo("");
  }

  [Test]
  public void DisplayDeviceDefinitionInformation()
  {
    _deviceDefinitions.OnNext(CreateDeviceDefinition("the_app", "1.2.3", "prod"));
    Check.That(_propertyChanges).Contains(nameof(_sut.Name), nameof(_sut.Version), nameof(_sut.Setup));
    Check.That(_sut.Name).IsEqualTo("the_app");
    Check.That(_sut.Version).IsEqualTo("1.2.3");
    Check.That(_sut.Setup).IsEqualTo("prod");
  }

  [Test]
  public void DisplayMissingDeviceDefinitionInformation()
  {
    _deviceDefinitions.OnNext(CreateDeviceDefinition("the_app", "1.2.3", "prod"));
    _propertyChanges.Clear();
    _deviceDefinitions.OnNext(null);
    Check.That(_propertyChanges).Contains(nameof(_sut.Name), nameof(_sut.Version), nameof(_sut.Setup));
    Check.That(_sut.Name).IsEqualTo("");
    Check.That(_sut.Version).IsEqualTo("");
    Check.That(_sut.Setup).IsEqualTo("");
  }

  [Test]
  public void DisplaySetupChangeTool()
  {
    _targetSystems.OnNext(CreateTargetSystem("", OS.linux, Architecture.x64, null, true, true));
    _deviceDefinitions.OnNext(CreateDeviceDefinition("", "", "prod", "dev", "preprod"));
    Check.That(_propertyChanges).Contains(
      nameof(_sut.Setups), nameof(_sut.NextSetup), nameof(_sut.CanChangeSetup),
      nameof(_sut.CanChangeSetupForever), nameof(_sut.CanChangeSetupUntilNextReboot));
    Check.That(_sut.Setups).IsEquivalentTo("dev", "prod", "preprod");
    Check.That(_sut.NextSetup).IsEqualTo("prod");
    Check.That(_sut.CanChangeSetup).IsTrue();
    Check.That(_sut.CanChangeSetupForever).IsTrue();
    Check.That(_sut.CanChangeSetupUntilNextReboot).IsTrue();
  }

  [Test]
  public void SetupChangeToolPermanentOnly()
  {
    _targetSystems.OnNext(CreateTargetSystem("", OS.linux, Architecture.x64, null, true, false));
    _deviceDefinitions.OnNext(CreateDeviceDefinition("", "", "prod", "dev", "preprod"));
    Check.That(_propertyChanges).Contains(
      nameof(_sut.Setups), nameof(_sut.NextSetup), nameof(_sut.CanChangeSetup),
      nameof(_sut.CanChangeSetupForever), nameof(_sut.CanChangeSetupUntilNextReboot));
    Check.That(_sut.Setups).IsEquivalentTo("dev", "prod", "preprod");
    Check.That(_sut.NextSetup).IsEqualTo("prod");
    Check.That(_sut.CanChangeSetup).IsTrue();
    Check.That(_sut.CanChangeSetupForever).IsTrue();
    Check.That(_sut.CanChangeSetupUntilNextReboot).IsFalse();
  }

  [Test]
  public void SetupChangeToolTemporaryOnly()
  {
    _targetSystems.OnNext(CreateTargetSystem("", OS.linux, Architecture.x64, null, false, true));
    _deviceDefinitions.OnNext(CreateDeviceDefinition("", "", "prod", "dev", "preprod"));
    Check.That(_propertyChanges).Contains(
      nameof(_sut.Setups), nameof(_sut.NextSetup), nameof(_sut.CanChangeSetup),
      nameof(_sut.CanChangeSetupForever), nameof(_sut.CanChangeSetupUntilNextReboot));
    Check.That(_sut.Setups).IsEquivalentTo("dev", "prod", "preprod");
    Check.That(_sut.NextSetup).IsEqualTo("prod");
    Check.That(_sut.CanChangeSetup).IsTrue();
    Check.That(_sut.CanChangeSetupForever).IsFalse();
    Check.That(_sut.CanChangeSetupUntilNextReboot).IsTrue();
  }

  [Test]
  public void NoSetupChangeToolWhenNoDeviceDefinition()
  {
    _targetSystems.OnNext(CreateTargetSystem("", OS.linux, Architecture.x64, null, true, true));
    _deviceDefinitions.OnNext(CreateDeviceDefinition("", "", "prod", "dev", "preprod"));
    _propertyChanges.Clear();
    _deviceDefinitions.OnNext(null);
    Check.That(_propertyChanges).Contains(
      nameof(_sut.Setups), nameof(_sut.NextSetup), nameof(_sut.CanChangeSetup),
      nameof(_sut.CanChangeSetupForever), nameof(_sut.CanChangeSetupUntilNextReboot));
    Check.That(_sut.Setups).IsEmpty();
    Check.That(_sut.NextSetup).IsEqualTo("");
    Check.That(_sut.CanChangeSetup).IsFalse();
    Check.That(_sut.CanChangeSetupForever).IsTrue();
    Check.That(_sut.CanChangeSetupUntilNextReboot).IsTrue();
  }
  
  [Test]
  public async Task ChangeSetupPermanently()
  {
    _targetSystems.OnNext(CreateTargetSystem("", OS.linux, Architecture.x64, null, true, true));
    _deviceDefinitions.OnNext(CreateDeviceDefinition("", "", "prod", "dev", "preprod"));
    _sut.NextSetup = "dev";
    UserReply(IUser.ChoiceType.Yes);
    await _sut.ChangeSetupForever();
    Check.That(_box.Title).IsEqualTo("Change setup");
    Check.That(_box.Message).IsEqualTo("Do you want to change the application setup to dev permanently?");
    Check.That(_box.Icon).IsEqualTo(IUser.Icon.Stop);
    Check.That(_requestedNewPermanentSetup).IsEqualTo("dev");
    Check.That(_requestedNewTemporarySetup).IsEqualTo(null);
  }
  
  [Test]
  public async Task CancelChangeSetupPermanently()
  {
    _targetSystems.OnNext(CreateTargetSystem("", OS.linux, Architecture.x64, null, true, true));
    _deviceDefinitions.OnNext(CreateDeviceDefinition("", "", "prod", "dev", "preprod"));
    _sut.NextSetup = "dev";
    UserReply(IUser.ChoiceType.No);
    await _sut.ChangeSetupForever();
    Check.That(_requestedNewPermanentSetup).IsEqualTo(null);
    Check.That(_requestedNewTemporarySetup).IsEqualTo(null);
  }
  
  [Test]
  public async Task ChangeSetupTemporarily()
  {
    _targetSystems.OnNext(CreateTargetSystem("", OS.linux, Architecture.x64, null, true, true));
    _deviceDefinitions.OnNext(CreateDeviceDefinition("", "", "prod", "dev", "preprod"));
    _sut.NextSetup = "preprod";
    UserReply(IUser.ChoiceType.Yes);
    await _sut.ChangeSetupUntilNextReboot();
    Check.That(_box.Title).IsEqualTo("Change setup");
    Check.That(_box.Message).IsEqualTo("Do you want to change the application setup to preprod until next reboot?");
    Check.That(_box.Icon).IsEqualTo(IUser.Icon.Stop);
    Check.That(_requestedNewPermanentSetup).IsEqualTo(null);
    Check.That(_requestedNewTemporarySetup).IsEqualTo("preprod");
  }
  
  [Test]
  public async Task CancelChangeSetupTemporarily()
  {
    _targetSystems.OnNext(CreateTargetSystem("", OS.linux, Architecture.x64, null, true, true));
    _deviceDefinitions.OnNext(CreateDeviceDefinition("", "", "prod", "dev", "preprod"));
    _sut.NextSetup = "preprod";
    UserReply(IUser.ChoiceType.No);
    await _sut.ChangeSetupUntilNextReboot();
    Check.That(_requestedNewPermanentSetup).IsEqualTo(null);
    Check.That(_requestedNewTemporarySetup).IsEqualTo(null);
  }


  private IRemoteDeviceDefinition CreateDeviceDefinition(string name, string version, string setup, params string[] otherSetups)
  {
    var m = new Mock<IRemoteDeviceDefinition>();
    m.Setup(x => x.Name).Returns(name);
    m.Setup(x => x.Version).Returns(version);
    m.Setup(x => x.Setup).Returns(setup);
    m.Setup(x => x.Setups).Returns(otherSetups.Append(setup).ToArray());
    return m.Object;
  }

  private ITargetSystem CreateTargetSystem(string name, OS os, Architecture arch, string hardware = null,
    bool hasNewSetup = false, bool hasNewTempSetup = false)
  {
    var m = new Mock<ITargetSystem>();
    m.Setup(x => x.Name).Returns(name);
    m.Setup(x => x.SystemInfo).Returns(new SystemInfo(os,arch,hardware));
    m.Setup(x => x.NewSetup)
      .Returns(CreateCapability( hasNewSetup ? s => _requestedNewPermanentSetup = s : null));
    m.Setup(x => x.NewTemporarySetup)
      .Returns(CreateCapability( hasNewTempSetup ? s => _requestedNewTemporarySetup = s : null));
    return m.Object;
  }

  private ITargetSystemCapability CreateCapability(Action<string> changeSetup)
  {
    var isAvailable = changeSetup != null;
    var action = isAvailable ? changeSetup : _ => { };
    var c = new Mock<ITargetSystemCapability>();
    c.Setup(x => x.IsAvailable).Returns(isAvailable);
    var ex = new Mock<ITargetSystemCapability.IExecution>();
    c.Setup(x => x.Execute(It.IsAny<string[]>()))
      .Returns(ex.Object)
      .Callback<string[]>(a => action(a[0]));
    return c.Object;
  }

  [SetUp]
  public void Init()
  {
    _concierge = new Mock<ILightConcierge>();
    _targetSystems = new Subject<ITargetSystem>();
    _deviceDefinitions = new Subject<IRemoteDeviceDefinition>();
    var rd = new Mock<IRemoteDevice>();
    rd.Setup(x => x.TargetSystem).Returns(_targetSystems);
    rd.Setup(x => x.DeviceDefinition).Returns(_deviceDefinitions);
    _concierge.Setup(x => x.RemoteDevice).Returns(rd.Object);
    _user = new Mock<IUser>();
    _concierge.Setup(x => x.User).Returns(_user.Object);
    _sut = new RemoteDeviceViewModel(_concierge.Object);
    _propertyChanges = new List<string>();
    _sut.PropertyChanged += (sender, args) =>
    {
      _propertyChanges.Add(args.PropertyName);
    };
  }

  private void UserReply(IUser.ChoiceType reply)
  {
    _user.Setup(x => x.Show(It.IsAny<IUser.Box>()))
      .Returns(Task.FromResult(reply))
      .Callback<IUser.Box>(b => { _box = b; });
    _requestedNewPermanentSetup = null;
    _requestedNewTemporarySetup = null;
  }
}