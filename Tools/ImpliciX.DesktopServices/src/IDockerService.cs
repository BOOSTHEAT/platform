using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace ImpliciX.DesktopServices;

public interface IDockerService
{
  Task Pull(
    string imageName,
    AuthConfig authConfig = null
  );

  Task Launch(
    string imageName,
    string containerName,
    bool autoRemove,
    IDictionary<string, IList<PortBinding>> portBindings = null,
    IEnumerable<(string, string)> binds = null,
    IEnumerable<string> command = null,
    IEnumerable<(string, string)> environments = null
  );

  Task Batch(
    CreateContainerParameters parameters
  );

  Task Stop(
    string containerName
  );

  Task Wait(
    string containerName
  );

  Task Execute(
    string containerName,
    params string[] command
  );

  public static IDictionary<string, IList<PortBinding>> DefinePortBindings(
    params (string, string, string)[] portBindings
  )
  {
    return portBindings.ToDictionary(
      x => x.Item1,
      x => (IList<PortBinding>) new List<PortBinding> { new ()  { HostIP = x.Item2, HostPort = x.Item3 } }
    );
  }
}
