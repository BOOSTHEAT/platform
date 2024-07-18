using System;
using System.Collections.Generic;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Renderers.Feeds
{
  public class MeasureRenderer : IRenderFeed
  {
    public string Id(Feed feed) => PropertyRenderer.Encode(((MeasureFeed)feed).Urn);

    public string Declare(FeedUse feedUse)
    {
      var name = Id(feedUse.Feed);
      return $@"property ModelProperty {name}$measure: ModelProperty {{}}
          property ModelProperty {name}$status: ModelProperty {{}}
          property Measure {name}: Measure {{
              measure: {name}$measure
              status: {name}$status
          }}";
    }

    public string GetValueOf(FeedUse feedUse)
    {
      var measure = (MeasureFeed)feedUse.Feed;
      var converter = feedUse.IsLocalSettings && _unitForType.TryGetValue(measure.Type, out string conversion)
        ? conversion : "Unit.none";
      var getter = feedUse.IsFormatted ? $"getFormattedValue({converter},true)" : $"getValue({converter})";
      return feedUse.PrependCacheIfNeeded($"{Id(measure)}.{getter}");
    }

    public string SetValueOf(FeedUse feedUse, string value) =>
      $"{feedUse.PrependCacheIfNeeded($"{Id(feedUse.Feed)}")}.setValue({value});";

    private static Dictionary<Type, string> _unitForType;

    static MeasureRenderer()
    {
      _unitForType = new Dictionary<Type, string>
      {
        [typeof(Temperature)] = "Unit.toCelsius",
        [typeof(Pressure)] = "Unit.toBar",
        [typeof(Power)] = "Unit.toKw",
        [typeof(Energy)] = "Unit.toKwh",
        [typeof(Percentage)] = "Unit.toPercentage",
      };
    }
  }
}