using System.Runtime.InteropServices;
using ImpliciX.DesktopServices.Services;
using ImpliciX.DesktopServices.Services.Project;
using NFluent;

namespace ImpliciX.DesktopServices.Tests.Services;


[Platform(Include = "Linux")]
public class EntryPointBuilderTests
{
  private const string HostUserName = LinuxEntryPointBuilder.HostUserName;

  [DllImport("libc")]
  private static extern uint geteuid();


  [Test]
  public void GivenLinux_WhenLinkUserNugetPackage()
  {
    var sut = new LinuxEntryPointBuilder();
    var cmd = sut
      .LinkUserNugetPackages()
      .SetCommand("dotnet restore")
      .Build();

    const string runAsUserCmd = $"mkdir -p ~/.nuget && ln -s {ProjectHelper.NugetLocalFeedUrl} ~/.nuget/packages && dotnet restore";

    var commandExpected = $"adduser {HostUserName} --uid {geteuid()} --gecos '' --disabled-password" +
                          " && " +
                          $"runuser -l {HostUserName} -c \"" +
                          runAsUserCmd +
                          "\"";

    Check.That(cmd).IsEqualTo(new[] {"/bin/sh", "-c", commandExpected});
  }

  [Test]
  public void GivenWindows_WhenLinkUserNugetPackage()
  {
    var sut = new WindowsEntryPointBuilder();
    var cmd = sut
      .LinkUserNugetPackages()
      .SetCommand("dotnet restore")
      .Build();

    const string commandExpected = $"mkdir -p /root/.nuget && ln -s {ProjectHelper.NugetLocalFeedUrl} /root/.nuget/packages && dotnet restore";
    Check.That(cmd).IsEqualTo(new[] {"/bin/sh", "-c", commandExpected});
  }

  [Test]
  public void GivenLinux_WhenLinkUserNugetPackage_NoBashCommand()
  {
    var sut = new LinuxEntryPointBuilder();
    var cmd = sut
      .LinkUserNugetPackages()
      .SetCommand("dotnet restore", false)
      .Build();

    const string runAsUserCmd = $"mkdir -p ~/.nuget && ln -s {ProjectHelper.NugetLocalFeedUrl} ~/.nuget/packages && dotnet restore";

    var commandExpected = $"adduser {HostUserName} --uid {geteuid()} --gecos '' --disabled-password" +
                          " && " +
                          $"runuser -l {HostUserName} -c \"" +
                          runAsUserCmd +
                          "\"";

    Check.That(cmd).IsEqualTo(new[] {commandExpected});
  }

  [Test]
  public void GivenWindows_WhenLinkUserNugetPackage_NoBashCommand()
  {
    var sut = new WindowsEntryPointBuilder();
    var cmd = sut
      .LinkUserNugetPackages()
      .SetCommand("dotnet restore", false)
      .Build();

    const string commandExpected = $"mkdir -p /root/.nuget && ln -s {ProjectHelper.NugetLocalFeedUrl} /root/.nuget/packages && dotnet restore";
    Check.That(cmd).IsEqualTo(new[] {commandExpected});
  }
}