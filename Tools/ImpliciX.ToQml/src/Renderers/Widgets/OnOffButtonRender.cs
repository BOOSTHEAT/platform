using System;
using System.Collections.Generic;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml.Renderers.Widgets;

internal sealed class OnOffButtonRender : IRenderWidget
{
  internal const string ValueToSendToTurnOff = "0";
  internal const string ValueToSendToTurnOn = "1";

  private readonly Dictionary<Type, IRenderFeed> _feedRenderers;

  public OnOffButtonRender(Dictionary<Type, IRenderFeed> feedRenderers)
  {
    _feedRenderers = feedRenderers ?? throw new ArgumentNullException(nameof(feedRenderers));
  }

  public void Render(WidgetRenderingContext context)
  {
    if (context == null) throw new ArgumentNullException(nameof(context));

    var widget = (OnOffButtonWidget) context.Widget;
    var targetUrn = ((PropertyFeed) widget.Value).Urn;

    var feedUse = widget.Value.OutOfCache(context.Cache);
    var currentValue = _feedRenderers.GetValueOf(feedUse.RawValue);

    context.Code
      .Open(context.Prefix + "OnOffButton")
      .Append($"x: {GetXPosition(context.Widget.X)}")
      .Append($"y: {GetYPosition(context.Widget.Y)}")
      .Append(@"title:""""")
      .Append($@"checked: {currentValue} != ""{ValueToSendToTurnOff}""")
      .Open("onToggled:")
      .Append("checked")
      .Append($@"? {context.Api}.sendProperty(""{targetUrn}"", ""{ValueToSendToTurnOn}"")")
      .Append($@": {context.Api}.sendProperty(""{targetUrn}"", ""{ValueToSendToTurnOff}"")")
      .Close()
      .Close();
  }

  public IEnumerable<Feed> FindFeeds(Widget widget) => new[] {((OnOffButtonWidget) widget).Value};

  private static string GetXPosition(SizeAndPosition xSizeAndPosition)
    => xSizeAndPosition.FromStart?.ToString() ??
       (xSizeAndPosition.ToEnd.HasValue
         ? $"parent.width - width - {xSizeAndPosition.ToEnd.Value}"
         : "0"
       );

  private static string GetYPosition(SizeAndPosition ySizeAndPosition)
    => ySizeAndPosition.FromStart?.ToString() ??
       (ySizeAndPosition.ToEnd.HasValue
         ? $"parent.height - height - {ySizeAndPosition.ToEnd.Value}"
         : "0"
       );
}