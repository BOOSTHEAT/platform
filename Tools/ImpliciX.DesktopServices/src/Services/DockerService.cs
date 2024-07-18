using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using ImpliciX.DesktopServices.Helpers;

namespace ImpliciX.DesktopServices.Services;

internal class DockerService : IDockerService
{
  private readonly DockerClient _client;

  private readonly IConsoleService _console;

  public DockerService(
    IConsoleService console
  )
  {
    _console = console;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      _client = new DockerClientConfiguration().CreateClient();
      return;
    }

    var clientSocket = GetClientUri();
    console.WriteLine("clientSocket = " + clientSocket);
    _client = new DockerClientConfiguration(new Uri(clientSocket)).CreateClient();

    string GetClientUri()
    {
      var podmanPath = $"/run/user/{geteuid()}/podman/podman.sock";
      return File.Exists(podmanPath)
        ? $"unix:{podmanPath}"
        : "unix:/var/run/docker.sock";
    }

    [DllImport("libc")]
    static extern uint geteuid();
  }

  public async Task Pull(
    string imageName,
    AuthConfig authConfig = null
  )
  {
    try
    {
      await _client.Images.CreateImageAsync(
        new ImagesCreateParameters { FromImage = imageName },
        authConfig,
        new Progress<JSONMessage>(m => _console.WriteLine(m.Status))
      );
    }
    catch (DockerContainerNotFoundException e)
    {
      _console.WriteError(e);
    }
  }

  public async Task Stop(
    string containerName
  )
  {
    try
    {
      await _client.Containers.StopContainerAsync(
        containerName,
        new ContainerStopParameters()
      );
    }
    catch (DockerContainerNotFoundException)
    {
    }
  }

  public async Task Wait(
    string containerName
  )
  {
    try
    {
      await _client.Containers.WaitContainerAsync(
        containerName,
        CancellationToken.None
      );
    }
    catch (DockerContainerNotFoundException)
    {
    }
  }

  public async Task Execute(
    string containerName,
    params string[] command
  )
  {
    try
    {
      var exec = await _client.Exec.ExecCreateContainerAsync(
        containerName,
        new ContainerExecCreateParameters
        {
          Cmd = command,
          Detach = false,
          AttachStdout = true,
          AttachStderr = true
        }
      );

      await _client.Exec.StartContainerExecAsync(exec.ID);
      while (!(await _client.Exec.InspectContainerExecAsync(exec.ID)).Running) Thread.Sleep(100);
    }
    catch (DockerContainerNotFoundException)
    {
    }
  }

  public async Task Launch(
    string imageName,
    string containerName,
    bool autoRemove,
    IDictionary<string, IList<PortBinding>> portBindings = null,
    IEnumerable<(string, string)> binds = null,
    IEnumerable<string> command = null,
    IEnumerable<(string, string)> environments = null
  )
  {
    try
    {
      var details = await _client.Containers.InspectContainerAsync(containerName);
      if (!details.State.Running)
        await _client.Containers.StartContainerAsync(
          containerName,
          null
        );
    }
    catch (DockerContainerNotFoundException)
    {
      var parameters = CreateCreateContainerParameters(
        imageName,
        containerName,
        autoRemove,
        portBindings,
        binds,
        command,
        environments
      );
      await CreateAndStart(
        parameters,
        false
      );
    }
  }

  public async Task Batch(
    CreateContainerParameters parameters
  )
  {
    parameters.HostConfig.AutoRemove = true;
    await CreateAndStart(
      parameters,
      true
    );
  }

  private CreateContainerParameters CreateCreateContainerParameters(
    string imageName,
    string containerName,
    bool autoRemove,
    IDictionary<string, IList<PortBinding>> portBindings = null,
    IEnumerable<(string, string)> binds = null,
    IEnumerable<string> command = null,
    IEnumerable<(string, string)> environment = null
  )
  {
    var parameters = new CreateContainerParameters
    {
      Name = containerName,
      Image = imageName,
      HostConfig = new HostConfig
      {
        AutoRemove = autoRemove,
        ExtraHosts = new List<string> { "host.docker.internal:host-gateway" },
      }
    };

    if (binds != null)
      parameters.HostConfig.Binds = binds.Select(v => $"{v.Item2}:{v.Item1}").ToList();

    if (portBindings == null)
    {
      parameters.HostConfig.NetworkMode = "host";
    }
    else
    {
      parameters.ExposedPorts = portBindings.ToDictionary(
        b => b.Key,
        _ => new EmptyStruct()
      );
      parameters.HostConfig.PortBindings = portBindings;
    }

    if (command != null)
      parameters.Cmd = command.ToList();

    if (environment != null)
      parameters.Env = environment.Select(x => $"{x.Item1}={x.Item2}").ToList();

    // await CreateAndStart(parameters);
    return parameters;
  }

  private async Task CreateAndStart(
    CreateContainerParameters parameters,
    bool waitContainerStopped
  )
  {
    if (parameters.Entrypoint is not null && parameters.Entrypoint.Count > 0)
      _console.WriteLine($"DOCKER ENTRYPOINT={string.Join(' ', parameters.Entrypoint)}");

    if (parameters.HostConfig?.Binds is not null)
      _console.WriteLine($"DOCKER BINDS={string.Join(" | ", parameters.HostConfig.Binds)}");

    var containerName = parameters.Name;
    _console.WriteLine($"Creating container {containerName}");
    var creation = await _client.Containers.CreateContainerAsync(parameters);
    creation.Warnings.ForEach(w => _console.WriteLine(w));
    _console.WriteLine($"Starting container {containerName}");
    var started = await _client.Containers.StartContainerAsync(
      containerName,
      null
    );
    _console.WriteLine($"Container {containerName} {(started ? "started" : "failed to start")}");
    var awaiter = _client.Containers.GetContainerLogsAsync(
      containerName,
      new ContainerLogsParameters { Follow = true, ShowStderr = true, ShowStdout = true },
      CancellationToken.None,
      new Progress<string>(
        m =>
        {
          if (m.Length > 8)
            _console.WriteLine(m.Substring(8));
        }
      )
    ).GetAwaiter();

    if (waitContainerStopped)
      await Wait(containerName);
  }
}
