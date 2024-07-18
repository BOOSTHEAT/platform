namespace ImpliciX.TimeMath.Access;

public interface ITimeSeriesSuffixes
{
  internal static readonly string StartSuffix =  "$samplingStartAt";
  internal static readonly string EndSuffix = "$samplingEndAt";
  internal static readonly string ValueAtUpdateSuffix =  "$ValueAtUpdate";
  internal static readonly string LastPublishedInstantSuffix =  "$LastValuePublishedAt";
  internal static readonly string ValueAtPublishedSuffix =  "$valueAtPublished";

  internal static string ToTsName(
    string rootUrn,
    string suffix
  )
  {
    return rootUrn + "_" + suffix ;
  }

  internal static string ToTsName(
    string rootUrn,
    string tsSuffix,
    string suffix
  )
  {
    return ToTsName(
      rootUrn ,
      tsSuffix + "_" + suffix
    ) ;
  }
}
