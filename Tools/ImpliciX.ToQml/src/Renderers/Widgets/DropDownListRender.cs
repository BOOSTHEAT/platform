using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml.Renderers.Widgets;

internal sealed class DropDownListRender : IRenderWidget
{
  private readonly Dictionary<Type, IRenderFeed> _feedRenderers;

  public DropDownListRender(Dictionary<Type, IRenderFeed> feedRenderers)
  {
    _feedRenderers = feedRenderers ?? throw new ArgumentNullException(nameof(feedRenderers));
  }

  public void Render(WidgetRenderingContext context)
  {
    if (context == null) throw new ArgumentNullException(nameof(context));

    var widget = (DropDownListWidget) context.Widget;
    var feed = (PropertyFeed) widget.Value;
    var enumType = feed.GetType().GenericTypeArguments.First();

    var feedUse = widget.Value.OutOfCache(context.Cache);
    var currentValue = _feedRenderers.GetValueOf(feedUse.RawValue);

    context.Code
      .Open(context.Prefix + "DropDownList");
    context.RenderBase();
    context.Code
      .Append($"runtime: {context.Runtime}")
      .Append("valueRole: 'value'")
      .Open("model:", "[")
      .ForEach(GetEnumItems(enumType), (i, c) =>
      {
        c.Append($"{{ key: '{i.Key}', text: '{i.Text}', value: {i.Value} }},");
      })
      .Close("]")
      .Append($"receivedValue: {currentValue}")
      .Append($"onActivated: runtime.api.sendProperty('{feed.Urn.Value}', currentValue)")
      .Close();
  }

  public class EnumItem
  {
    public string Key { get; }
    public string Text { get; }
    public int Value { get; }

    public EnumItem(Type enumType, object v)
    {
      Key = $"{enumType.Name}.{v}";
      Text = v.ToString();
      Value = (int)v;
    }
  }

  public static IEnumerable<EnumItem> GetEnumItems(Type enumType) =>
    Enum.GetValues(enumType)
      .Cast<object>()
      .Select(x => new EnumItem(enumType,x))
      .ToArray();

  public IEnumerable<Feed> FindFeeds(Widget widget) => new[] {((DropDownListWidget) widget).Value};
  
}