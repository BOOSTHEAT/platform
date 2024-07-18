using System;
using System.Collections.Generic;
using Avalonia.Controls;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.Features;

public interface IFeatures : IDisposable
{
  IMainWindow Window { get; }
  ILightConcierge Concierge { get; }
  string Title { get; }
  NamedModel Home { get; }
  MenuItemViewModel[] MenuItems { get; }
  List<IUser.FileSelectionFilter> Filters { get; }
  bool ShallLoadOnStartup { get; }
  void CompleteInitialization(IMainWindow window);
  void RegisterUserOn(TopLevel topLevel);

  public static ConditionalMenuViewModel<ITargetSystem> TargetSystemMenuItem(
    ILightConcierge concierge,
    string title,
    Func<ITargetSystem, ITargetSystemCapability> capabilityFinder,
    string question) =>
    new(concierge, $"{title}...",
      concierge.RemoteDevice.TargetSystem,
      d => capabilityFinder(d).IsAvailable,
      async (m, d) =>
      {
        if (await m.Ask(title, question))
          await capabilityFinder(d).Execute().AndWriteResultToConsole();
      });
}
