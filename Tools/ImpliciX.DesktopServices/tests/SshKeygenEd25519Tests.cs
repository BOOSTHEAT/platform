using System.Text;
using ImpliciX.DesktopServices.Helpers;
using Renci.SshNet;

namespace ImpliciX.DesktopServices.Tests;

[TestFixture]
public class SshKeygenEd25519Tests
{
  [Test]
  public void LoadGeneratedKey()
  {
    var passphrase = "ABCDE";
    var key = SshKeygenEd25519.Generate(passphrase);
    var loadedKey = new PrivateKeyFile(new MemoryStream(Encoding.ASCII.GetBytes(key)), passphrase);
    var publicKey = loadedKey.ToPublic();
    Assert.That(publicKey, Does.StartWith("ssh-ed25519 "));
  }
    
  [TestCase("", typeof(Renci.SshNet.Common.SshPassPhraseNullOrEmptyException))]
  [TestCase("X", typeof(Renci.SshNet.Common.SshException))]
  public void FailToLoadGeneratedKeyWithWrongPassphrase(string passphrase, Type expectedException)
  {
    var key = SshKeygenEd25519.Generate("ABCDE");
    Assert.Throws(expectedException, () =>
    {
      var privateKeyFile = new PrivateKeyFile(new MemoryStream(Encoding.ASCII.GetBytes(key)), passphrase);
    });
  }
}