using ImpliciX.DesktopServices.Helpers;
using Moq;
using NFluent;

namespace ImpliciX.DesktopServices.Tests
{
  [TestFixture]
  public class IdentityTests
  {
    [Test]
    public async Task created_and_read_identity_are_identical()
    {
      var createdIdentity = await RunInputBox("12345").Create();
      var readIdentity = await RunInputBox("12345").Read();
      Check.That(readIdentity!.Value.Key).IsEqualTo(createdIdentity!.Value.Key);
      Check.That(readIdentity!.Value.File.ToPublic()).IsEqualTo(createdIdentity!.Value.File.ToPublic());
      
      var createdIdentityAgain = await RunInputBox("123456").Create();
      var readIdentityAgain = await RunInputBox("123456").Read();
      Check.That(readIdentityAgain!.Value.Key).IsEqualTo(createdIdentityAgain!.Value.Key);
      Check.That(readIdentityAgain!.Value.File.ToPublic()).IsEqualTo(createdIdentityAgain!.Value.File.ToPublic());
    }
    
    [Test]
    public Task cannot_read_unexisting_identity()
    {
      var identity = new Identity(null, null);
      UserSettings.Set("Identity", "");
      Check.ThatAsyncCode(async () => await identity.Read())
        .Throws<ApplicationException>().WithMessage("No identity was defined");
      return Task.CompletedTask;
    }
    
    [Test]
    public async Task cannot_read_identity_with_wrong_passphrase()
    {
      var createdIdentity = await RunInputBox("12345").Create();
      Check.ThatAsyncCode(async () =>  await RunInputBox("123456").Read())
        .Throws<ApplicationException>()
        .WithMessage("Cannot read identity.\nThe checkints differed, the openssh key was not correctly decoded.");
    }

    private static IIdentity RunInputBox(string password)
    {
      var console = new Mock<IConsoleService>();
      var user = new Mock<IUser>();
      var identity = new Identity(user.Object, console.Object);
      user.Setup(x => x.EnterPassword(It.IsAny<IUser.Box>())).Returns(Task.FromResult((IUser.ChoiceType.Ok, password)));
      return identity;
    }
    
  }
}