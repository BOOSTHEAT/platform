using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaUI.PrintToPDF;
using ImpliciX.Designer.ViewModels;

namespace ImpliciX.Designer.Views;

public class SystemView : UserControl
{
  private static readonly Size MaximumPageSize = new(5000, 5000);

  public SystemView()
  {
    this.InitializeComponent();
  }

  private SystemViewModel System => ((SystemViewModel)DataContext)!;

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public void SaveAsPdf(string filename)
  {
    var allSubSystems = System.OrderedSubSystems.ToList();
    allSubSystems.ForEach(s => s.ShowTransitions = true);
    Print.ToFile(filename, this.DisplayDiagrams(allSubSystems));
  }

  private IEnumerable<Visual> DisplayDiagrams(IEnumerable<SubSystemViewModel> subSystems)
  {
    foreach (var ss in subSystems)
    {
      System.Select(ss);
      ss.View.DisplayOn("pdf");
      new[] { ss.View }.Layout(MaximumPageSize);
      yield return ss.View;
      ss.View.DisplayOn("screen");
    }
  }

  private void TreeView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    if (e.AddedItems.Count > 0)
    {
      var namedItem = (e.AddedItems[0] as NamedTree)?.Parent;
      System.Select(namedItem);
    }
  }
}
