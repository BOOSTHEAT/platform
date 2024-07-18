using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ImpliciX.Designer.ViewModels;

public interface IMainWindow : INotifyPropertyChanged
{
  IMainWindowContent Workspace { get; }
  LiveConnectViewModel LiveConnectViewModel { get; }
  bool AutoConnect { get; set; }
  Action<string> SaveAsPdf { get; set; }
  void SelectAndLoadDeviceDefinition();
  void LoadDeviceDefinition(string path);
  void ConnectTo(string connection);
  void Close();
  IObservable<IEnumerable<string>> PreviousDeviceDefinitionPaths { get; }
  IEnumerable<string> LatestPreviousDeviceDefinitionPaths { get; }
  void SelectOutputFileAndSaveAllDiagramsAsPdf();
  void ExitApp();
}