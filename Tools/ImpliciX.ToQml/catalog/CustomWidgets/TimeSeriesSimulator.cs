using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Feeds;
using ImpliciX.ToQml.Renderers.Widgets;

namespace ImpliciX.ToQml.Catalog.CustomWidgets;

public class TimeSeriesSimulator
{
  public static void AddTo(QmlRenderer qmlRenderer) => qmlRenderer.AddRenderer<W>(new R(qmlRenderer.FeedRenderers));

  public static Block DefineBlock(Urn timeSeriesUrn)
  {
    return new B(timeSeriesUrn);
  }

  public class B : Block
  {
    private readonly Urn _timeSeries;

    public B(Urn timeSeries)
    {
      _timeSeries = timeSeries;
    }

    public override Widget CreateWidget() =>
      new W
      {
        Value = TimeSeriesFeed.Subscribe(_timeSeries),
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
      context.Code.Open(context.Prefix+" Column");
      context.RenderBase();
      context.Code
        .Open("Text")
        .Append($"text: '{urn}'")
        .Close();
      context.Code
        .Open("TextArea")
        .Open("onTextChanged:").Append("root.runtime.notifyUserAction()").Close()
        .Open("onFocusChanged:").Append($"IOContext.editTextField({id},'{id}')").Close()
        .Append($"id: {id}")
        .Open("function initialize()")
        .Append($"var init = screenSelector.getDefaultValue('{urn}');")
        .Append($"if(init) {{ text = init; editingFinished(); }}")
        .Close()
        .Open("onEditingFinished:")
        .Append(_feedRenderers.SetValueOf(feedUse, "text"))
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
  
  public static string MakeJson(params (float Value,string At)[] timeSeries)
  {
    var data = timeSeries.Select(x => new Item(x.Value, x.At)).ToArray();
    var str = new MemoryStream();
    var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(data.GetType());
    ser.WriteObject(str, data);
    str.Position = 0;  
    var sr = new StreamReader(str);  
    var json = sr.ReadToEnd().Replace("},","},\\n").Replace("\"","\\\"");
    return $"'{json}'";
  }

  [System.Runtime.Serialization.DataContractAttribute]
  class Item
  {
    [System.Runtime.Serialization.DataMemberAttribute]
    public float Value;
    [System.Runtime.Serialization.DataMemberAttribute]
    public string At;

    public Item(float value, string at)
    {
      Value = value;
      At = at;
    }
  }
  
}

