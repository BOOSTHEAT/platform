using System.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace ImpliciX.Designer
{
  static class Extensions
  {
    public static void DisplayOn(this ILogical userControl, string media)
    {
      ((Panel)userControl.LogicalChildren.First()).DisplayPanelOn(media);
    }
    public static void DisplayPanelOn(this Panel panel, string media)
    {
      foreach (var child in panel.Children)
      {
        child.IsVisible = child.Classes.Contains(media);
        if (child is Panel p)
          p.DisplayPanelOn(media);
      }
    }
  }
}


