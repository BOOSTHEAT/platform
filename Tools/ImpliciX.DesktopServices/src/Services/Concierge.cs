using System;
using System.Threading.Tasks;
using ImpliciX.DesktopServices.Helpers;
using ImpliciX.DesktopServices.Services.Project;
using ImpliciX.DesktopServices.Services.SshInfrastructure;
using ImpliciX.DesktopServices.Services.WebsocketInfrastructure;

namespace ImpliciX.DesktopServices.Services;

internal class BaseConcierge : IBaseConcierge, IAsyncDisposable
{
  public BaseConcierge(IUser user)
  {
    User = user;
    Export = new ExportService(Console.WriteLine);
    Identity = new Identity(User, Console);
    var sshAdapter = new SshNetAdapter();
    RemoteDevice = new RemoteDevice(
      Console,
      Identity,
      sshAdapter,
      new WebSocketClientWrapper(),
      async (sshClientFactory, connectionString, consoleOutputSlice)
        => await TargetSystem.Create(this, sshClientFactory, connectionString, consoleOutputSlice));

    if (user is not { IsConsoleWrittenToFile: true }) return;
    this.AddFileLogger(User);
  }

  private IFileLogger FileLogger { get; set; }

  public async ValueTask DisposeAsync()
  {
    await FileLogger.DisposeAsync();
  }

  public IConsoleService Console { get; } = new ConsoleService();
  public IIdentity Identity { get; }
  public IRemoteDevice RemoteDevice { get; }
  public IOperatingSystem OperatingSystem { get; } = new OperatingSystem();
  public IUser User { get; }
  public IFileSystemService FileSystemService { get; } = new FileSystemService();
  public IExport Export { get; }

  public RuntimeFlags RuntimeFlags { get; } = new();

  private void AddFileLogger(IUser user)
  {
    FileLogger = new FileLogger(user.ConsoleFolderPath, user.AppName);
    Console.LineWritten += (sender, line) => FileLogger.WriteAsync(line);
    Console.Errors += (sender, ex) => FileLogger.WriteAsync(ex.Message);
  }
}

internal class LightConcierge : BaseConcierge, ILightConcierge
{
  public LightConcierge(IUser user) : base(user)
  {
    System.Console.WriteLine("\ud83d\udd51 LightConcierge");
    Applications = new ApplicationsManager(Console);
    Session = new SessionService(RemoteDevice, Applications.Devices);
  }

  public IManageApplicationDefinitions Applications { get; }
  public ISessionService Session { get; }
}

internal class Concierge : BaseConcierge, IConcierge
{
  public Concierge(IUser user) : base(user)
  {
    System.Console.WriteLine("\ud83d\udd51 Concierge");
    Docker = new DockerUserFriendlyMessagesDecorator(new DockerService(Console));
    ProjectsManager = new ProjectsManager(Docker, Console);
    Applications = ProjectsManager;
    Session = new SessionService(RemoteDevice, ProjectsManager.Devices);
    System.Console.WriteLine("\u2713 Concierge");
  }

  public IDockerService Docker { get; }
  public IManageApplicationDefinitions Applications { get; }
  public ISessionService Session { get; }
  public IManageProjects ProjectsManager { get; }
}
