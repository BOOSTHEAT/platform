using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.SharedKernel.Tools;

public class UserInterfaceExplorer
{
  public static Information GetInformationFor(Func<GUI> getDefinition) => new (getDefinition);
  
  public class Information
  {
    public Information(Func<GUI> getDefinition)
    {
      if (getDefinition == null)
        throw new ArgumentException("Null GUI definition");
      var screens = getDefinition().ToSemanticModel().Screens.Values;
      Widgets = RecurseWidgets(screens.SelectMany(s => s.Widgets).ToArray());
      XSpans =
        from chart in Widgets.OfType<ChartXTimeYWidget>()
        from fd in chart.Content
        let feed = fd.Value
        where feed is Node
        let node = (Node) feed
        select (node.Urn, chart.XSpan);
    }

    public IEnumerable<Widget> Widgets { get; }
    public IEnumerable<(Urn,ChartXTimeSpan)> XSpans { get; }

    private static Widget[] RecurseWidgets(Widget[] ws) => ws.Any()
        ? ws.SelectMany(w => RecurseWidgets(ChildrenWidget(w).ToArray()).Prepend(w)).ToArray()
        : Array.Empty<Widget>();

    private static IEnumerable<Widget> ChildrenWidget(Widget w) => w switch
      {
        SwitchWidget sw => sw.Cases.Select(c => c.Then).Append(sw.Default),
        Composite cw => cw.Content,
        NavigatorWidget nw => new [] { nw.Visual, nw.OnTarget },
        _ => Enumerable.Empty<Widget>()
      };
  }
}