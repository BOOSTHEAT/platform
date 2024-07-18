using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using ImpliciX.DesktopServices.Helpers;
using ImpliciX.Language;
using ImpliciX.Language.Core;

namespace ImpliciX.DesktopServices.Services;

internal class ApplicationsManager : IManageApplicationDefinitions
{
  private readonly IConsoleService _console;
  private readonly Func<string, ApplicationDefinition> _builder;
  private readonly Subject<Option<IDeviceDefinition>> _devices;
  private readonly IDeviceDefinitionFactory _factory;

  public ApplicationsManager(IConsoleService console)
  : this(console, x => NupkgLoader.CreateApplication(x).App, new DeviceDefinitionFactory())
  {
  }
  
  public ApplicationsManager(IConsoleService console, Func<string,ApplicationDefinition> builder, IDeviceDefinitionFactory ddFactory)
  {
    _console = console;
    _builder = builder;
    _devices = new Subject<Option<IDeviceDefinition>>();
    LatestDevice = Option<IDeviceDefinition>.None();
    _factory = ddFactory;
  }
  
  public void Load(string sourcePath)
  {
    _console.WriteLine($"Loading {sourcePath}");
    var app = _builder(sourcePath);
    var device = _factory.Create(sourcePath, app);
    LatestDevice = Option<IDeviceDefinition>.Some(device);
    _devices.OnNext(LatestDevice);
    _previousPaths.Record(sourcePath);
    _console.WriteLine($"Loading {sourcePath} complete.");
  }

  public void UnLoad()
  {
    LatestDevice = Option<IDeviceDefinition>.None();
    _devices.OnNext(LatestDevice);
  }

  public IObservable<IEnumerable<string>> PreviousPaths => _previousPaths.Subject;
  public IEnumerable<string> LatestPreviousPaths => _previousPaths;
  private readonly PathHistory _previousPaths = new();

  public IObservable<Option<IDeviceDefinition>> Devices => _devices;
  public Option<IDeviceDefinition> LatestDevice { get; private set; }
}

internal class PathHistory : History<string>
{
  private const int DefaultSize = 10;
  internal const string PersistenceKey = "PathHistory";
  public PathHistory() : base(DefaultSize, PersistenceKey)
  {
  }
}