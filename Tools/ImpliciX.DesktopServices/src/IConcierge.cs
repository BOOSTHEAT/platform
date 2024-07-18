using ImpliciX.DesktopServices.Services;

namespace ImpliciX.DesktopServices;

public interface IBaseConcierge
{
  IConsoleService Console { get; }
  IIdentity Identity { get; }
  IRemoteDevice RemoteDevice { get; }
  IOperatingSystem OperatingSystem { get; }
  IUser User { get; }
  IFileSystemService FileSystemService { get; }
  IExport Export { get; }
  RuntimeFlags RuntimeFlags { get; }
}

public interface ILightConcierge : IBaseConcierge
{
  IManageApplicationDefinitions Applications { get; }
  ISessionService Session { get; }
  static ILightConcierge Create(IUser user = null) => new LightConcierge(user);
}

public interface IConcierge : ILightConcierge
{
  IDockerService Docker { get; }
  IManageProjects ProjectsManager { get; }
  new static IConcierge Create(IUser user = null) => new Concierge(user);
}