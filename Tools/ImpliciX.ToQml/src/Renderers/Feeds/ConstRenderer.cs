using System;
using System.Globalization;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Renderers.Feeds
{
  public class ConstRenderer : IRenderFeed
  {
    public string Id(Feed feed)
    {
      dynamic constFeed = (Const)feed;
      dynamic constValue = MeaningfulError(() => constFeed.Value, feed);
      string strValue = constValue.ToString(CultureInfo.InvariantCulture);
      return strValue.Replace(".", "_");
    }

    public string Declare(FeedUse feedUse) => String.Empty;

    public string GetValueOf(FeedUse feedUse) => MeaningfulError(() => AsString(feedUse), feedUse.Feed);

    public string SetValueOf(FeedUse feedUse, string value) =>
      throw new NotSupportedException("Constant feed cannot be assigned");

    public static string AsString(FeedUse feedUse)
    {
      var feed = feedUse.Feed;
      dynamic constFeed = (Const)feed;
      dynamic constValue = constFeed.Value;

      if (constValue is string strConst)
      {
        var str = "\"" + strConst + "\"";
        return feed.Translate ? feedUse.PrependCacheIfNeeded($"translate({str})") : str;
      }

      if (constValue is float floatConst)
        return floatConst.ToString(CultureInfo.InvariantCulture);
      if (constValue is double doubleConst)
        return doubleConst.ToString(CultureInfo.InvariantCulture);
      if (constValue.GetType().IsEnum)
        return ((int)constValue).ToString();
      return string.Empty;
    }

    private T MeaningfulError<T>(Func<T> getter, Feed feed)
    {
      try
      {
        return getter();
      }
      catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
      {
        throw new Exception($"{feed.GetType().GenericTypeArguments[0].FullName} shall be public.", e);
      }
    }
  }
}