using System.Collections.Generic;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Control;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Model;

namespace ImpliciX.DesktopServices;

public interface IDeviceDefinition
{
  string Path { get; }
  string Name { get; }
  string Version { get; }
  string EntryPoint { get; }
  ModelFactory ModelFactory { get; }
  MetricsModuleDefinition Metrics { get; }
  UserInterfaceModuleDefinition UserInterface { get; }
  IEnumerable<ISubSystemDefinition> SubSystemDefinitions { get; }
  IDictionary<string, Urn> Urns { get; }
  IEnumerable<Urn> UserSettings { get; }
  IEnumerable<Urn> VersionSettings { get; }
  IEnumerable<Urn> AllSettings { get; }
}