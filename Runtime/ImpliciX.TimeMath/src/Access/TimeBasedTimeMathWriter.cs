using System;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.TimeMath.Computers;
using static ImpliciX.TimeMath.Access.ITimeSeriesSuffixes;

namespace ImpliciX.TimeMath.Access;

internal sealed class TimeBasedTimeMathWriter : ITimeMathWriter
{
  private readonly IWriteTimeSeries _tsWriter;

  public TimeBasedTimeMathWriter(
    IWriteTimeSeries tsWriter
  )
  {
    _tsWriter = tsWriter;
  }

  public void UpdateLastMetric(
    string rootUrn,
    string suffix,
    TimeSpan instant,
    float value
  )
  {
    WriteTsAndApplyRetention(
      ToTsName(
        rootUrn,
        suffix,
        ValueAtUpdateSuffix
      ),
      instant,
      value
    );
  }

  public void RemoveLastMetric(
    string rootUrn,
    string suffix
  )
  {
    throw new NotImplementedException();
    _tsWriter.ApplyRetentionPolicy();
  }

  public void UpdateStartAt(
    string rootUrn,
    TimeSpan instant
  )
  {
    WriteTsAndApplyRetention(
      ToTsName(
        rootUrn,
        StartSuffix
      ),
      instant,
      0
    );
  }

  public void UpdateEndAt(
    string rootUrn,
    TimeSpan instant
  )
  {
    WriteTsAndApplyRetention(
      ToTsName(
        rootUrn,
        EndSuffix
      ),
      instant,
      0
    );
  }

  public void AddValueAtPublish(
    string rootUrn,
    string suffix,
    TimeSpan instant,
    float value
  )
  {
    WriteTsAndApplyRetention(
      ToTsName(
        rootUrn,
        suffix,
        ValueAtPublishedSuffix
      ),
      instant,
      value
    );

    WriteTsAndApplyRetention(
      ToTsName(
        rootUrn,
        LastPublishedInstantSuffix
      ),
      instant,
      0
    );
  }

  public void SetupTimeSeries(
    string rootUrn,
    string[] suffixes,
    TimeSpan retentionPeriod
  )
  {
    _tsWriter
      .SetupTimeSeries(
        ToTsName(
          rootUrn,
          StartSuffix
        ),
        ITimeMathComputer.KeepLatestOnly
      );

    _tsWriter
      .SetupTimeSeries(
        ToTsName(
          rootUrn,
          EndSuffix
        ),
        ITimeMathComputer.KeepLatestOnly
      );

    _tsWriter
      .SetupTimeSeries(
        ToTsName(
          rootUrn,
          LastPublishedInstantSuffix
        ),
        ITimeMathComputer.KeepLatestOnly
      );

    suffixes
      .Select(
        suffix =>
          ToTsName(
            rootUrn,
            suffix,
            ValueAtPublishedSuffix
          )
      )
      .ForEach(
        tsName =>
          _tsWriter
            .SetupTimeSeries(
              tsName,
              retentionPeriod
            )
      )
      ;

    suffixes
      .Select(
        suffix =>
          ToTsName(
            rootUrn,
            suffix,
            ValueAtUpdateSuffix
          )
      )
      .ForEach(
        tsName =>
          _tsWriter
            .SetupTimeSeries(
              tsName,
              ITimeMathComputer.KeepLatestOnly
            )
      )
      ;
  }

  #region IWriteTimeSeries Decoration

  public void SetupTimeSeries(string key, TimeSpan retentionPeriod) => _tsWriter.SetupTimeSeries(key, retentionPeriod);

  public void WriteTsAndApplyRetention(string key, TimeSpan at, float value)
  {
    _tsWriter.Write(key, at, value);
    _tsWriter.ApplyRetentionPolicy();
  }

  public void DeleteTs(string key, TimeSpan? from = null, TimeSpan? to = null) => _tsWriter.Delete(key, TimeSpan.Zero, to);

  #endregion
}