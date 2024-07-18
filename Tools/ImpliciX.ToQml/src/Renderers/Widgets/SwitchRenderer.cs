using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml.Renderers.Widgets
{
  public class SwitchRenderer : IRenderWidget
  {
    private readonly Dictionary<Type, IRenderWidget> _wRender;
    private readonly Dictionary<Type, IRenderFeed> _fRender;

    public SwitchRenderer(Dictionary<Type, IRenderWidget> wRender, Dictionary<Type, IRenderFeed> fRender)
    {
      _wRender = wRender;
      _fRender = fRender;
    }
    public void Render(WidgetRenderingContext context)
    {
      var switcher = (SwitchWidget)context.Widget;
      context.Code.Open(context.Prefix+" Item");
      context.RenderBaseWithBaseChild("current");
      var cases = switcher.Cases.Select((c,i)
        => (_fRender.GetValueOf(c.When.OutOfCache(context.Cache)),c.Then,$"case{i}")).ToArray();
      foreach (var @case in cases)
        _wRender.Render(context.Override(widget:@case.Then, prefix:$"property var {@case.Item3}:"));
      _wRender.Render(context.Override(widget:switcher.Default, prefix:$"property var defaultCase:"));
      context.Code.Open("function chooseCase()");
      context.Code.Append($"if({cases.First().Item1}) return {cases.First().Item3};");
      foreach (var otherCase in cases.Skip(1))
        context.Code.Append($"else if({otherCase.Item1}) return {otherCase.Item3};");
      context.Code
        .Append($"else return defaultCase;")
        .Close()
        .Append($"property var current: chooseCase()")
        .Append($"data: [current]")
        .Close();
    }

    public IEnumerable<Feed> FindFeeds(Widget widget) => ((SwitchWidget)widget).Cases.SelectMany(FeedsForCase).Concat(_wRender.FindFeeds(((SwitchWidget)widget).Default));
    private IEnumerable<Feed> FeedsForCase(SwitchWidget.Case @case) => _wRender.FindFeeds(@case.Then).Concat(new[] {@case.When.Left, @case.When.Right});
  }
}