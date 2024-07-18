using System.Collections.Generic;
using ImpliciX.Data;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Tools;

namespace ImpliciX.SystemSoftware
{
  public class Context
  {
    private readonly IDictionary<string, SoftwareDeviceNode> _softwareMap;
    private readonly Dictionary<SoftwareDeviceNode, SoftwareVersion> _fallbackVersions;
    private string _fallbackReleaseManifestPath;
    private string _currentReleaseManifestPath;

    public Context(IDictionary<string, SoftwareDeviceNode> softwareMap)
    {
      _softwareMap = softwareMap;
      _fallbackVersions = new Dictionary<SoftwareDeviceNode, SoftwareVersion>();
      SupportedForUpdate = new HashSet<SoftwareDeviceNode>();
      AlwaysUpdate = new HashSet<SoftwareDeviceNode>();
      CurrentUpdatePackage = Option<Package>.None();
    }
    public HashSet<SoftwareDeviceNode> SupportedForUpdate { get; set; }

    public string CurrentReleaseManifestPath
    {
      get => _currentReleaseManifestPath;
      set
      {
        _currentReleaseManifestPath = value;
        var parsedResult = Manifest.FromFile(_currentReleaseManifestPath)
          .LogWhenError("[SystemSoftware] Can't load current slot manifest.");
        if (parsedResult.IsSuccess)
        {
          CurrentReleaseManifest = parsedResult.Value;
        }
      }
    }

    public string FallbackReleaseManifestPath
    {
      get => _fallbackReleaseManifestPath;
      set
      {
        _fallbackReleaseManifestPath = value;
        var parsedResult = Manifest.FromFile(_fallbackReleaseManifestPath)
          .LogWhenError("[SystemSoftware] Can't load fallback slot manifest.");
        if (parsedResult.IsSuccess)
        {
          FallbackReleaseManifest = parsedResult.Value;
          foreach (var partManifest in FallbackReleaseManifest.AllPartsManifests())
          {
            var parsedManifestVersion = SoftwareVersion.FromString(partManifest.Revision);
            if (parsedManifestVersion.IsSuccess && _softwareMap.TryGetValue(partManifest.Target, out var device))
            {
              _fallbackVersions[device] = parsedManifestVersion.Value;
            }
          }
        }
        
      }
    }

    public string UpdateManifestPath { get; set; }
    public Manifest CurrentReleaseManifest { get; private set; }
    private Manifest FallbackReleaseManifest { get; set; }
    public HashSet<SoftwareDeviceNode> AlwaysUpdate { get; set; }

    public Option<SoftwareVersion> GetFallbackVersion(SoftwareDeviceNode device)
    {
       if (_fallbackVersions.TryGetValue(device, out var version))
       {
         return Option<SoftwareVersion>.Some(version);
       }
       return Option<SoftwareVersion>.None();
    }
    
    public Option<Package> CurrentUpdatePackage { get; private set; }

    public Unit SetCurrentUpdatePackage(Package package)
    {
      CurrentUpdatePackage = Option<Package>.Some(package);
      return default(Unit);
    }
  }
}