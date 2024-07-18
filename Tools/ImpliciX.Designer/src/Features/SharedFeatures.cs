using System;
using System.Collections.Generic;
using ImpliciX.Designer.ViewModels;
using ReactiveUI;

namespace ImpliciX.Designer.Features;

public static class SharedFeatures
{
  public static MenuItemViewModel OpenDeviceDefinitionMenu(this IFeatures features)
    => new OpenDeviceDefinitionMenuItemViewModel(features);

  public static MenuItemViewModel CloseDeviceDefinitionMenu(this IFeatures features)
    => new CommandViewModel(
      "Close", 
      () => features.Window?.Close()
      );

  class OpenDeviceDefinitionMenuItemViewModel : MenuItemViewModel
  {
    public OpenDeviceDefinitionMenuItemViewModel(IFeatures features)
    {
      Text = "_Open Device Definition";
      StaticItems = new MenuItemViewModel[]
      {
        new CommandViewModel(
          "Open...",
          () => features.Window?.SelectAndLoadDeviceDefinition()
        ),
        new MenuSeparatorViewModel()
      };
      features
        .WhenAnyValue(f => f.Window)
        .Subscribe(window =>
        {
          if (window == null)
            return;
          LoadPreviousPaths(window.LatestPreviousDeviceDefinitionPaths, window);
          window.PreviousDeviceDefinitionPaths?
            .Subscribe(paths => { LoadPreviousPaths(paths, window); });
        });
    }

    private void LoadPreviousPaths(IEnumerable<string> paths, IMainWindow window)
    {
      while (Items.Count > 2)
        Items.RemoveAt(2);
      foreach (var path in paths)
      {
        Items.Add(new CommandViewModel(
          path,
          () => window.LoadDeviceDefinition(path)
        ));
      }
    }
  }
}