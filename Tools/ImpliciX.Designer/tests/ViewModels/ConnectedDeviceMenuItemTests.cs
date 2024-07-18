using System;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ImpliciX.Designer.Features;
using ImpliciX.DesktopServices;
using Moq;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels;

public class ConnectedDeviceMenuItemTests
{
  [Test]
  public void NominalCase()
  {
    var (concierge, capability, execution) = CreateConcierge();
    var askConfirmation = UserChoice(concierge, b => b.Title=="The Menu Title", IUser.ChoiceType.Ok);
    OpenMenu(concierge, "The Menu Title", capability);
    askConfirmation.Verify();
    execution.Verify(x => x.AndWriteResultToConsole());
    execution.VerifyNoOtherCalls();
  }

  [Test]
  public void UserCancel()
  {
    var (concierge, capability, execution) = CreateConcierge();
    var askConfirmation = UserChoice(concierge, b => b.Title=="The Menu Title", IUser.ChoiceType.Cancel);
    OpenMenu(concierge, "The Menu Title", capability);
    askConfirmation.Verify();
    capability.VerifyNoOtherCalls();
    execution.VerifyNoOtherCalls();
  }

  private static Mock<IUser> UserChoice(Mock<ILightConcierge> concierge,
    Expression<Func<IUser.Box, bool>> cond, IUser.ChoiceType choice)
  {
    var user = new Mock<IUser>();
    user.Setup(x => x.Show(It.Is(cond))).Returns(Task.FromResult(choice)).Verifiable();
    concierge.Setup(x => x.User).Returns(user.Object);
    return user;
  }

  private static void OpenMenu(Mock<ILightConcierge> concierge, string title, Mock<ITargetSystemCapability> capability)
  {
    var sut = IFeatures.TargetSystemMenuItem(
      concierge.Object, title, _ => capability.Object, "The Question"
    );
    sut.Open();
  }

  private static (Mock<ILightConcierge> concierge, Mock<ITargetSystemCapability> capability, Mock<ITargetSystemCapability.IExecution> execution)
    CreateConcierge()
  {
    var concierge = new Mock<ILightConcierge>();
    var app = new Mock<IRemoteDevice>();
    var connectedDevices = new Subject<ITargetSystem>();
    app.Setup(x => x.TargetSystem).Returns(connectedDevices);
    concierge.Setup(x => x.RemoteDevice).Returns(app.Object);
    var device = new Mock<ITargetSystem>();
    connectedDevices.OnNext(device.Object);
    var capability = new Mock<ITargetSystemCapability>();
    var capabilityExecution = new Mock<ITargetSystemCapability.IExecution>();
    capability.Setup(x => x.Execute()).Returns(capabilityExecution.Object).Verifiable();
    return (concierge, capability, capabilityExecution);
  }
}