#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using ImpliciX.DesktopServices.Services.Project;
using ImpliciX.Language.Core;

namespace ImpliciX.DesktopServices.Services;

internal static class EntryPointBuilderFactory
{
  public static EntryPointBuilder Create()
    => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
      ? new WindowsEntryPointBuilder()
      : new LinuxEntryPointBuilder();
}

internal abstract class EntryPointBuilder
{
  protected bool UseBashCommand { get; private set; } = true;
  protected Option<string> ExecCommand { get; private set; } = Option<string>.None();

  public abstract EntryPointBuilder LinkUserNugetPackages();

  public EntryPointBuilder SetCommand(string command, bool useBashCommand = true)
  {
    UseBashCommand = useBashCommand;
    ExecCommand = command;
    return this;
  }

  public abstract IList<string> Build();

  protected static string GetLinkUserNugetPackagesCommand(string localPath)
  {
    var rootDirPath = Path.GetDirectoryName(localPath);
    return $"mkdir -p {rootDirPath} && ln -s {ProjectHelper.NugetLocalFeedUrl} {localPath}";
  }
}

internal sealed class WindowsEntryPointBuilder : EntryPointBuilder
{
  private Option<string> _linkNugetCommand = Option<string>.None();

  public override EntryPointBuilder LinkUserNugetPackages()
  {
    _linkNugetCommand = GetLinkUserNugetPackagesCommand("/root/.nuget/packages");
    return this;
  }

  public override IList<string> Build()
  {
    var commands = new List<string>();

    _linkNugetCommand.Tap(o => commands.Add(o));
    ExecCommand.Tap(o => commands.Add(o));

    var command = string.Join(" && ", commands);
    return UseBashCommand
      ? new[] {"/bin/sh", "-c", command}
      : new[] {command};
  }
}

internal sealed class LinuxEntryPointBuilder : EntryPointBuilder
{
  [DllImport("libc")]
  private static extern uint geteuid();

  public const string HostUserName = "host";
  private Option<string> _linkNugetCommand = Option<string>.None();

  private static string _addLinuxUserCommand => $"adduser {HostUserName} --uid {geteuid()} --gecos '' --disabled-password";

  private static string GetRunAsHostUser(IEnumerable<string> commands)
  {
    var command = string.Join(" && ", commands);
    return $"runuser -l {HostUserName} -c \"{command}\"";
  }

  public override EntryPointBuilder LinkUserNugetPackages()
  {
    _linkNugetCommand = GetLinkUserNugetPackagesCommand("~/.nuget/packages");
    return this;
  }

  public override IList<string> Build()
  {
    var runAsCmd = new List<string>();
    _linkNugetCommand.Tap(o => runAsCmd.Add(o));
    ExecCommand.Tap(o => runAsCmd.Add(o));

    var commands = new[]
    {
      _addLinuxUserCommand,
      GetRunAsHostUser(runAsCmd)
    };

    var command = string.Join(" && ", commands);
    return UseBashCommand
      ? new[] {"/bin/sh", "-c", command}
      : new[] {command};
  }
}