using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ImpliciX.DesktopServices.Helpers;
using ImpliciX.Language;
using ImpliciX.Language.GUI;
using JetBrains.Annotations;

namespace ImpliciX.DesktopServices.Services.Project;

internal class DllProjectManager : IManageProject
{
  private readonly string _binPath;
  private readonly Action<IDeviceDefinition> _onMake;
  [CanBeNull] private ApplicationDefinition _appMain;
  private readonly ConcurrentDictionary<string, Assembly> _assemblies;
  private readonly IDockerService _docker;
  private readonly IFileSystemService _fileService;
  private readonly IProjectHelper _projectHelper;
  private readonly IDeviceDefinitionFactory _deviceDefFactory;
  private readonly IApplicationDefinitionFactory _appDefFactory;
  [CanBeNull] private readonly string _sourcePath;
  [NotNull] private readonly Action<string, IDictionary<string, Assembly>> _addAssembliesFromSource;
  private readonly IProjectOperation<LinkerInput, string> _linkerBuilder;

  public DllProjectManager([NotNull] string binPath, [NotNull] Action<IDeviceDefinition> onMake,
    [NotNull] IDockerService docker, [NotNull] IFileSystemService fileService, [NotNull] IProjectHelper projectHelper,
    [NotNull] IProjectOperation<LinkerInput, string> linkerBuilder,
    [NotNull] IDeviceDefinitionFactory deviceDefFactory, [NotNull] IApplicationDefinitionFactory appDefFactory,
    [CanBeNull] Action<string, IDictionary<string, Assembly>> customAddAssembliesFromSource = null,
    [CanBeNull] string sourcePath = null)
  {
    _docker = docker ?? throw new ArgumentNullException(nameof(docker));
    _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    _projectHelper = projectHelper ?? throw new ArgumentNullException(nameof(projectHelper));
    _deviceDefFactory = deviceDefFactory ?? throw new ArgumentNullException(nameof(deviceDefFactory));
    _appDefFactory = appDefFactory ?? throw new ArgumentNullException(nameof(appDefFactory));
    _sourcePath = sourcePath;
    _binPath = binPath ?? throw new ArgumentNullException(nameof(binPath));
    _onMake = onMake ?? throw new ArgumentNullException(nameof(onMake));
    _assemblies = new ConcurrentDictionary<string, Assembly>();
    _linkerBuilder = linkerBuilder;
    _addAssembliesFromSource = customAddAssembliesFromSource ?? AddAssembliesFromSource;
  }

  public string Path => _binPath;

  public Task<IDeviceDefinition> Make()
  {
    var device = _deviceDefFactory.Create(_sourcePath ?? _binPath,CreateApplicationInstance());
    _onMake(device);
    return Task.FromResult(device);
  }

  public bool CanMakeMultipleTimes => false;

  public async Task<FileInfo> CreatePackage(SystemInfo systemInfo)
  {
    if (_appMain is null)
      throw new InvalidOperationException($"Application instance is null : {nameof(Make)} must be call before {nameof(CreatePackage)}");

    var buildGui = _appMain.ModuleDefinitions.Any(m => m is UserInterfaceModuleDefinition);
    var builder = new BuildImpliciXPackage(_docker, _fileService, _projectHelper, _linkerBuilder, systemInfo, buildGui);
    var dd = _deviceDefFactory.Create(_binPath, _appMain);

    var packageFilename = await builder.Execute(new LinkerInput(
      dd.Name,
      dd.EntryPoint,
      dd.Version,
      new[] {"-a", $"/app/{System.IO.Path.GetFileName(_binPath)}".ToLinuxPath()},
      new[] {$"{System.IO.Path.GetDirectoryName(_binPath)}:/app"})
    );

    return new FileInfo(packageFilename);
  }

  public async Task RunGui()
  {
    var dd = _deviceDefFactory.Create(_binPath, _appMain);
    var runner = new RunGui(_docker, new ProjectHelper(), _linkerBuilder);
    await runner.Execute(new LinkerInput(
      dd.Name,
      dd.EntryPoint,
      dd.Version,
      new[] {"-a", $"/app/{System.IO.Path.GetFileName(_binPath)}".ToLinuxPath()},
      new[] {$"{System.IO.Path.GetDirectoryName(_binPath)}:/app"})
    );
  }

  private ApplicationDefinition CreateApplicationInstance()
  {
    if (_appMain != null)
      return _appMain;

    AppDomain.CurrentDomain.AssemblyResolve += InMemoryAssemblyResolve;

    _addAssembliesFromSource(_binPath, _assemblies);

    _appMain = _appDefFactory.CreateEntryPointFrom(_assemblies.Values.SelectMany(a => a.GetTypes()).ToArray(), _binPath);
    return _appMain;
  }

  private void AddAssembliesFromSource(string sourcePath, IDictionary<string, Assembly> assemblies)
  {
    var assembly = Assembly.LoadFile(sourcePath);
    assemblies.TryAdd(assembly.GetName().Name, assembly);

    if (assemblies.Count == 0)
      throw new FileNotFoundException($"No assembly found in {sourcePath}");

    var referencedAssemblies = assembly.GetReferencedAssemblies();
    foreach (var referencedAssembly in referencedAssemblies)
    {
      if (assemblies.ContainsKey(referencedAssembly.Name)) continue;
      var dependencyPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(sourcePath), referencedAssembly.Name + ".dll");
      if (!_fileService.FileExists(dependencyPath)) continue;
      var dependencyAssembly = Assembly.LoadFile(dependencyPath);
      assemblies.TryAdd(dependencyAssembly.GetName().Name, dependencyAssembly);
    }
  }

  private Assembly InMemoryAssemblyResolve(object sender, ResolveEventArgs args)
  {
    var assemblyName = new AssemblyName(args.Name!);
    return _assemblies.GetValueOrDefault(assemblyName.Name);
  }
}