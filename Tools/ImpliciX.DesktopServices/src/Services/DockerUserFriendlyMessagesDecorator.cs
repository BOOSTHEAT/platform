using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace ImpliciX.DesktopServices.Services;

internal sealed class DockerUserFriendlyMessagesDecorator : IDockerService
{
  private readonly IDockerService _decorated;

  public DockerUserFriendlyMessagesDecorator(
    IDockerService decorated
  )
  {
    _decorated = decorated;
  }

  public async Task Pull(
    string imageName,
    AuthConfig authConfig = null
  )
  {
    await UserFriendlyMessages(
      () => _decorated.Pull(
        imageName,
        authConfig
      )
    );
  }

  public async Task Stop(
    string containerName
  )
  {
    await UserFriendlyMessages(() => _decorated.Stop(containerName));
  }

  public async Task Wait(
    string containerName
  )
  {
    await UserFriendlyMessages(() => _decorated.Wait(containerName));
  }

  public async Task Execute(
    string containerName,
    params string[] command
  )
  {
    await UserFriendlyMessages(
      () => _decorated.Execute(
        containerName,
        command
      )
    );
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
    await UserFriendlyMessages(
      () => _decorated.Launch(
        imageName,
        containerName,
        autoRemove,
        portBindings,
        binds,
        command,
        environments
      )
    );
  }

  public async Task Batch(
    CreateContainerParameters createContainerParameters
  )
  {
    await UserFriendlyMessages(() => _decorated.Batch( createContainerParameters));
  }

  private async Task UserFriendlyMessages(
    Func<Task> action
  )
  {
    try
    {
      await action();
    }
    catch (HttpRequestException e)
    {
      throw new ApplicationException(
        "A Docker operation has timed out.\nPlease check your Docker install and configuration.",
        e
      );
    }
  }

  // public async Task CreateAndStart(
  //   CreateContainerParameters parameters,
  //   bool waitContainerStopped = true
  // )
  // {
  //   await UserFriendlyMessages(
  //     () => _decorated.CreateAndStart(
  //       parameters,
  //       waitContainerStopped: waitContainerStopped
  //     )
  //   );
  // }
}
