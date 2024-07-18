using System;

namespace ImpliciX.TimeMath.Access;

public interface ITimeMathWriter
{
  void UpdateLastMetric(
    string rootUrn,
    string suffix,
    TimeSpan instant,
    float value
  );

  void RemoveLastMetric(
    string rootUrn,
    string suffix
  );

  void UpdateStartAt(
    string rootUrn,
    TimeSpan instant
  );

  void UpdateEndAt(
    string rootUrn,
    TimeSpan instant
  );

  void AddValueAtPublish(
    string rootUrn,
    string suffix,
    TimeSpan instant,
    float value
  );

  void SetupTimeSeries(
    string rootUrn,
    string[] suffixes,
    TimeSpan retentionPeriod
  );

  void SetupTimeSeries(string key, TimeSpan retentionPeriod);
  public void WriteTsAndApplyRetention(string key, TimeSpan at, float value);
  public void DeleteTs(string key, TimeSpan? from = null, TimeSpan? to = null);
}