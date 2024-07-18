using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace ImpliciX.DesktopServices.Helpers
{
  internal class Identity : IIdentity
  {
    private readonly IUser _user;
    private readonly IConsoleService _console;
    public string Name => Environment.UserName;

    public Identity(IUser user, IConsoleService console)
    {
      _user = user;
      _console = console;
    }
    
    public async Task<(PrivateKeyFile File, string Key)?> Create()
    {
      var passphrase = await InputPassphrase("Create Identity", "Create");
      if (passphrase == null)
        return null;
      var key = SshKeygenEd25519.Generate(passphrase);
      var keyFile = GetPrivateKeyFile(key, passphrase);
      UserSettings.Set(IdentitySetting, key);
      _console.WriteLine($"Identity created: {keyFile.ToPublic()}");
      return (keyFile, key);
    }

    public async Task<(PrivateKeyFile File, string Key)?> Import(string key)
    {
      var passphrase = await InputPassphrase("Import Identity", "Import");
      if (passphrase == null)
        return null;
      var keyFile = GetPrivateKeyFile(key, passphrase);
      UserSettings.Set(IdentitySetting, key);
      _console.WriteLine($"Identity imported: {keyFile.ToPublic()}");
      return (keyFile, key);
    }

    public async Task<(PrivateKeyFile File, string Key)?> Read()
    {
      try
      {
        var key = await ReadAsString();
        if (string.IsNullOrWhiteSpace(key))
        {
          throw new ApplicationException("No identity was defined");
        }
        var keyFile = GetPrivateKeyFile(key, await InputPassphrase("Confirm Identity", "Ok"));
        _console.WriteLine("Identity confirmed");
        return (keyFile, key);
      }
      catch (SshException e)
      {
        throw new ApplicationException($"Cannot read identity.\n{e.Message}");
      }
    }

    public Task<string> ReadAsString() => Task.FromResult(UserSettings.Read(IdentitySetting));

    private const string IdentitySetting = "Identity";

    private static PrivateKeyFile GetPrivateKeyFile(string key, string passphrase) =>
      new PrivateKeyFile(new MemoryStream(Encoding.ASCII.GetBytes(key)), passphrase);
    
    [ItemCanBeNull]
    private async Task<string> InputPassphrase(string title, string ok)
    {
      var box = new IUser.Box
      {
        Title = title,
        Message = "Passphrase",
        Icon = IUser.Icon.Setting,
        Buttons = IUser.ChoiceType.Ok.With(text:ok, isDefault:true)+IUser.ChoiceType.Cancel.With(isCancel:true)
      };
      var result = await _user.EnterPassword(box);
      if (result.Choice == IUser.ChoiceType.Cancel)
        return null;
      return result.Password;
    }
  }
}