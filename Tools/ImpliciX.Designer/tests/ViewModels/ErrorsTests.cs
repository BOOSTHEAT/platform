using System;
using System.Linq;
using System.Threading.Tasks;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using Moq;
using NUnit.Framework;
using static System.Environment;

namespace ImpliciX.Designer.Tests.ViewModels;

public class ErrorsTests
{
  private Mock<IUser> _user;
  private Errors _error;

  [Test]
  public async Task SingleException()
  {
    _user.Setup(u => u.Show(It.Is<IUser.Box>( b =>
      b.Title == "Failed"
      && b.Message == $"foo{NewLine}"
      && b.Buttons.Count() == 1
      && b.Buttons.Single().Type == IUser.ChoiceType.Ok
    ))).Returns(Task.FromResult(IUser.ChoiceType.Ok)).Verifiable();
    await _error.Display(new Exception("foo"));
    _user.Verify();
    _user.Verify(c=> c.IsConsoleWrittenToFile, Times.Once);
    _user.VerifyNoOtherCalls();
  }
  
  [Test]
  public async Task MultipleExceptionsNoDetails()
  {
    _user.Setup(u => u.Show(It.Is<IUser.Box>( b =>
      b.Title == "Failed"
      && b.Message == "foo"
      && b.Buttons.Count() == 2
      && b.Buttons.First().Type == IUser.ChoiceType.Ok
      && b.Buttons.Last().Type == IUser.ChoiceType.Custom1
      && b.Buttons.Last().Text == "Details"
    ))).Returns(Task.FromResult(IUser.ChoiceType.Ok)).Verifiable();
    await _error.Display(new Exception("foo", new Exception("bar", new Exception("qix"))));
    _user.Verify();
    _user.Verify(c=> c.IsConsoleWrittenToFile, Times.Once);
    _user.VerifyNoOtherCalls();
  }
  
  [Test]
  public async Task MultipleExceptionsWithDetails()
  {
    _user.Setup(u => u.Show(It.Is<IUser.Box>( b =>
      b.Title == "Failed"
      && b.Message == "foo"
      && b.Buttons.Count() == 2
      && b.Buttons.First().Type == IUser.ChoiceType.Ok
      && b.Buttons.Last().Type == IUser.ChoiceType.Custom1
      && b.Buttons.Last().Text == "Details"
    ))).Returns(Task.FromResult(IUser.ChoiceType.Custom1)).Verifiable();
    _user.Setup(u => u.Show(It.Is<IUser.Box>( b =>
      b.Title == "Failed"
      && b.Message == $"bar{NewLine}qix{NewLine}"
      && b.Buttons.Count() == 1
      && b.Buttons.First().Type == IUser.ChoiceType.Ok
    ))).Returns(Task.FromResult(IUser.ChoiceType.Ok)).Verifiable();
    _user.Setup(u=>u.IsConsoleWrittenToFile).Returns(false);  
    await _error.Display(new Exception("foo", new Exception("bar", new Exception("qix"))));
    _user.Verify();
    _user.Verify(c=> c.IsConsoleWrittenToFile, Times.Once);
    _user.VerifyNoOtherCalls();
  }

  [SetUp]
  public void Init()
  {
    _user = new Mock<IUser>();
    var concierge = ILightConcierge.Create(_user.Object);
    _error = new Errors(concierge);
  }
}