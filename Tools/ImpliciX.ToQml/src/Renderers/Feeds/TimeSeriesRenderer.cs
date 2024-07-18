using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Renderers.Feeds;

public class TimeSeriesRenderer : IRenderFeed
{
  internal const string CacheUrnPrefix = "timeSeries";
  public static string Encode(Urn urn) => urn.Value.Replace(":", "$");

  public string Id(Feed feed) => CacheUrnPrefix + "$" + Encode(((TimeSeriesFeed)feed).Urn);
  
  public string Declare(FeedUse feedUse) => $@"property TimeSeries {Id(feedUse.Feed)}: TimeSeries {{}}";

  private string GetBase(FeedUse feedUse) => feedUse.PrependCacheIfNeeded(Id(feedUse.Feed));

  public string GetValueOf(FeedUse feedUse) =>
    GetBase(feedUse) + (feedUse.IsFormatted ? ".getFormattedValues()" : ".values");

  public string SetValueOf(FeedUse feedUse, string value) => $"{GetBase(feedUse)}.setDataPointsFromString({value});";
}