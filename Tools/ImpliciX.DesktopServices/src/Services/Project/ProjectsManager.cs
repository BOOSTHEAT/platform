using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using ImpliciX.DesktopServices.Helpers;
using ImpliciX.Language.Core;

namespace ImpliciX.DesktopServices.Services.Project;

internal sealed class ProjectsManager : IManageProjects
{
    public IObservable<Option<IManageProject>> Projects => _projects;
    public IObservable<Option<IDeviceDefinition>> Devices => _devices;

    private readonly Subject<Option<IManageProject>> _projects;
    private readonly Subject<Option<IDeviceDefinition>> _devices;
    private readonly IDockerService _docker;
    private readonly IConsoleService _console;
    private readonly BuildLinker _linkerBuilder;

    public Option<IManageProject> LatestProject { get; private set; }
    public Option<IDeviceDefinition> LatestDevice { get; private set; }

    public ProjectsManager(IDockerService docker, IConsoleService console)
    {
        _docker = docker;
        _console = console;
        _projects = new Subject<Option<IManageProject>>();
        _devices = new Subject<Option<IDeviceDefinition>>();
        _linkerBuilder = new BuildLinker(_docker);
        LatestProject = Option<IManageProject>.None();
        LatestDevice = Option<IDeviceDefinition>.None();
    }

    public void Load(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        OnLoad(extension switch
        {
            ".dll" => new DllProjectManager(sourcePath, OnMake, _docker, 
                new FileSystemService(), new ProjectHelper(), _linkerBuilder, new DeviceDefinitionFactory(), new ApplicationDefinitionFactory()),
            ".nupkg" => new NupkgProjectManager(sourcePath, OnMake, _docker, _console,
                new FileSystemService(), new ProjectHelper(), _linkerBuilder, new DeviceDefinitionFactory()),
            ".csproj" => new CsProjectManager(sourcePath, OnMake, _docker, _console,
                new FileSystemService(), new ProjectHelper(), _linkerBuilder, new DeviceDefinitionFactory(), new ApplicationDefinitionFactory()),
            _ => throw new NotSupportedException(),
        });
    }

    public void UnLoad()
    {
        LatestProject = Option<IManageProject>.None();
        _projects.OnNext(LatestProject);
        LatestDevice = Option<IDeviceDefinition>.None();
        _devices.OnNext(LatestDevice);
    }

    public IObservable<IEnumerable<string>> PreviousPaths => _previousPaths.Subject;
    public IEnumerable<string> LatestPreviousPaths => _previousPaths;
    private readonly PathHistory _previousPaths = new();

    public void OnLoad(IManageProject project)
    {
        LatestProject = Option<IManageProject>.Some(project);
        _projects.OnNext(LatestProject);
        _previousPaths.Record(project.Path);
        project.Make();
    }

    public void OnMake(IDeviceDefinition dd)
    {
        if (dd == null)
            return;

        LatestDevice = Option<IDeviceDefinition>.Some(dd);
        _devices.OnNext(LatestDevice);
    }
}