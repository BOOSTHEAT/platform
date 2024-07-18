using System;
using System.IO;
using System.Runtime.InteropServices;
using ImpliciX.DesktopServices;
using Mono.Unix.Native;

namespace ImpliciX.Designer.ViewModels.LiveMenu
{
  public class OpenSshTerminal : ActionMenuViewModel<ILightConcierge>
  {
    public OpenSshTerminal(ILightConcierge concierge) : base(concierge)
    {
      Text = "Open SSH Terminal";
    }

    public override async void Open()
    {
      await BusyWhile(async () =>
      {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
          await Errors.Display("Not Implemented",
            $"SSH terminal is not yet available on\n{RuntimeInformation.OSDescription}");
          return;
        }
        try
        {
          var privateKey = await Concierge.Identity.ReadAsString();
          if (privateKey == null)
            throw new ApplicationException("Cannot find identity");
          var privateKeyFilePath = Path.Combine(Path.GetTempPath(), $"{Concierge.Identity.Name}.identity");
          await File.WriteAllTextAsync(privateKeyFilePath, privateKey);
          MakeFileOnlyAccessibleToUser(privateKeyFilePath);
          await Concierge.OperatingSystem.OpenTerminal(
            $"ssh -o IdentitiesOnly=yes -i \"{privateKeyFilePath}\" -o LogLevel=ERROR -o StrictHostKeyChecking=no -o GlobalKnownHostsFile=/dev/null -o UserKnownHostsFile=/dev/null -p 9222 root@127.0.0.1"
          );
         }
        catch (Exception e)
        {
          await Errors.Display(e);
        }
      });
    }

    private void MakeFileOnlyAccessibleToUser(string privateKeyFilePath)
    {
      if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        Syscall.chmod(privateKeyFilePath, FilePermissions.S_IRUSR | FilePermissions.S_IWUSR);
    }
  }
}