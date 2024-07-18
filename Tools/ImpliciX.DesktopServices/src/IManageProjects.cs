using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;

namespace ImpliciX.DesktopServices;

public interface IManageApplicationDefinitions
{
  void Load(string sourcePath);
  void UnLoad();
  IObservable<IEnumerable<string>> PreviousPaths { get; }
  IEnumerable<string> LatestPreviousPaths { get; }
  IObservable<Option<IDeviceDefinition>> Devices { get; }
  Option<IDeviceDefinition> LatestDevice { get; }
}

public interface IManageProjects : IManageApplicationDefinitions
{
  IObservable<Option<IManageProject>> Projects { get; }
  Option<IManageProject> LatestProject { get; }
}
