using System;
using System.Collections.Generic;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml.Renderers.Widgets
{
    public class TextBoxRenderer : IRenderWidget
    {
        private readonly Dictionary<Type, IRenderFeed> _feedRenderers;

        public TextBoxRenderer(Dictionary<Type, IRenderFeed> feedRenderers)
        {
            _feedRenderers = feedRenderers;
        }
        
        public void Render(WidgetRenderingContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var widget = (TextBox)context.Widget;
            var feed = (PropertyFeed)widget.Value;
            context.Code.Open(context.Prefix + "TextBox")
                .Append($"x: {GetPosition(context.Widget.X)}")
                .Append($"y: {GetPosition(context.Widget.Y)}")
                .Append("runtime: root.runtime")
                .Append($"urn: '{feed.Urn.Value}'")
                .Append($"text: {_feedRenderers.GetValueOf(feed.OutOfCache(context.Cache).UseNeutralSettings.RawValue)}")
                .Append(widget.Width.HasValue, () => $"width: {widget.Width!.Value}");
            widget.Style.Fallback(context.Style).Render(context.Code);
            context.Code.Close();
        }

        public IEnumerable<Feed> FindFeeds(Widget widget)
        {
            var textBox = (TextBox)widget;
            yield return textBox.Value;
        }

        private static string GetPosition(SizeAndPosition sizeAndPosition)
            => sizeAndPosition.FromStart?.ToString() ??
               (sizeAndPosition.ToEnd.HasValue
                   ? $"{sizeAndPosition.ToEnd.Value}"
                   : "0"
               );
    }
}