using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Feeds;
using ImpliciX.ToQml.Renderers.Widgets;

namespace ImpliciX.ToQml.Catalog.CustomWidgets;

public class MeasureSimulator
{
  public static void AddTo(QmlRenderer qmlRenderer) => qmlRenderer.AddRenderer<W>(new R(qmlRenderer.FeedRenderers));
  
  public static Block DefineBlock(ModelNode measureNode)
  {
    var measureType = measureNode.GetType().GetGenericArguments().First();
    return (Block) Activator.CreateInstance(typeof(B<>).MakeGenericType(measureType),measureNode)!;
  }

  public class B<T> : Block
  {
    private readonly MeasureNode<T> _measure;

    public B(MeasureNode<T> measure)
    {
      _measure = measure;
    }

    public override Widget CreateWidget() =>
      new W
      {
        Value = MeasureFeed.Subscribe(_measure),
      };
  }

  public class W : Widget
  {
    public Feed Value;
  }

  public static W D(Widget w) => (W)w;

  public class R : IRenderWidget
  {
    private readonly Dictionary<Type, IRenderFeed> _feedRenderers;

    public R(Dictionary<Type, IRenderFeed> feedRenderers)
    {
      _feedRenderers = feedRenderers;
    }
    
    public void Render(WidgetRenderingContext context)
    {
      var feedUse = D(context.Widget).Value.OutOfCache(context.Cache);
      var urn = ((Node)D(context.Widget).Value).Urn.Value;
      var id = $"sim_{_feedRenderers.Id(feedUse.Feed).Replace('$','_')}";
      context.Code.Open(context.Prefix + " Column");
      context.RenderBase();
      context.Code
        .Open("Text")
        .Append($"text: '{urn}'")
        .Close();
      context.Code
        .Open("TextField")
        .Open("onTextEdited:").Append("root.runtime.notifyUserAction()").Close()
        .Open("onFocusChanged:").Append($"IOContext.editTextBox(this)").Close()
        .Append($"id: {id}")
        .Append($"property string helper: '>>> {id}'")
        .Open("function initialize()")
        .Append($"var init = {_feedRenderers.GetValueOf(feedUse.RawValue)} ?? screenSelector.getDefaultValue('{urn}');")
        .Append($"if(init) {{ text = init; editingFinished(); }}")
        .Close()
        .Open("onEditingFinished:")
        .Append(_feedRenderers.SetValueOf(feedUse, "[text,'Success']"))
        .Close()
        .Close();
      context.Code
        .Open("RuntimeEvents")
        .Append("runtime: root.runtime")
        .Open("onEnterScreen:")
        .Append($"{id}.initialize();")
        .Close()
        .Close();
      context.Code.Close();
    }

    public IEnumerable<Feed> FindFeeds(Widget widget) => new Feed[] {};
  }
  
}

