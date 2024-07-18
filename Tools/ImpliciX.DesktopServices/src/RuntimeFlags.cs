using System;
using ImpliciX.DesktopServices.Helpers;
using JetBrains.Annotations;

namespace ImpliciX.DesktopServices;

public class RuntimeFlags
{
  [NotNull] public const string RemoveLocalColdStorageDownloaded_EnvVarName = "IMPLICIX_DISABLE_REMOVE_LOCAL_COLD_STORAGE_DOWNLOAD";
  [NotNull] private readonly Func<string, string> _getEnvironmentVariable;

  public RuntimeFlags([CanBeNull] Func<string, string?> getEnvironmentVariable = null)
  {
    _getEnvironmentVariable = getEnvironmentVariable ?? Environment.GetEnvironmentVariable;
    RemoveLocalColdStorageDownloaded = DoesNotExist(RemoveLocalColdStorageDownloaded_EnvVarName);
  }

  public bool RemoveLocalColdStorageDownloaded { get; }

  private bool Exists(string envVariableName) => !DoesNotExist(envVariableName);

  private bool DoesNotExist(string envVariableName) => _getEnvironmentVariable(envVariableName)?.IsEmpty() ?? true;
}