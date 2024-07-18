using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.TimeMath.Access;

namespace ImpliciX.TimeMath.Tests.Helpers;

public class FakeTimeMathPersistence : ITimeMathWriter, ITimeMathReader
{
  private readonly Dictionary<string, TimeSpan> _end = new ();
  private readonly Dictionary<string, TimeSpan> _lastPublishInstant = new ();
  private readonly Dictionary<string, FloatValueAt> _lastUpdateValue = new ();
  private readonly Dictionary<string, Dictionary<TimeSpan, FloatValueAt>> _publishedValues = new ();
  private readonly Dictionary<string, TimeSpan> _start = new ();
  private TimeSpan _retentionPeriod;

  public TimeSpan ReadStartAt(
    string rootUrn
  )
  {
    return _start.GetValueOrDefault(
      rootUrn,
      TimeSpan.Zero
    );
  }

  public Option<FloatValueAt> ReadFirstValueAtPublish(
    string rootUrn,
    string suffix,
    TimeSpan start
  )
  {
    var urn = MetricUrn(
      rootUrn,
      suffix
    );

    if (_publishedValues.TryGetValue(
          urn,
          out var publishedValues
        ))
    {
      var res =
          publishedValues
            .Where(pair => pair.Key <= start)
            .ToArray()
        ;
      /*
      if (_end.TryGetValue(
            rootUrn,
            out var end
          ))
          {
      */
      /*
      publishedValues
        .Keys
        .Where(publishedAt => publishedAt < end - _retentionPeriod)
        .ForEach(
          publishedAt =>
            publishedValues.Remove(publishedAt)
        );
      */
      /*
        publishedValues
         .Keys
         .Where(publishedAt => publishedAt < end - _retentionPeriod)
         .ForEach(
         publishedAt =>
         _publishedValues[urn].Remove(publishedAt)
         );
         */
      /*
      }
      */

      return res.IsEmpty()
          ? Option<FloatValueAt>.None()
          : Option<FloatValueAt>.Some(
            res
              .MaxBy(pair => pair.Key)
              .Value
          )
        ;
    }

    return Option<FloatValueAt>.None()
      ;
  }

  public TimeSpan ReadEndAt(
    string rootUrn
  )
  {
    return _end.GetValueOrDefault(
      rootUrn,
      TimeSpan.Zero
    );
  }

  public Option<TimeSpan> ReadLastPublishedInstant(
    string rootUrn
  )
  {
    return _lastPublishInstant.TryGetValue(
        rootUrn,
        out var timeSpan
      )
        ? timeSpan
        : Option<TimeSpan>.None()
      ;
  }

  public Option<FloatValueAt> ReadTsFirst(
    string key,
    TimeSpan? from = null
  )
  {
    throw new NotImplementedException();
  }

  public Option<FloatValueAt> ReadTsLast(
    string key,
    TimeSpan? upTo = null
  )
  {
    throw new NotImplementedException();
  }

  public IEnumerable<FloatValueAt> ReadTsAll(string key)
  {
    throw new NotImplementedException();
  }

  public Option<FloatValueAt> ReadLastUpdate(
    string rootUrn,
    string suffix
  )
  {
    return _lastUpdateValue.TryGetValue(
        MetricUrn(
          rootUrn,
          suffix
        ),
        out var updateValue
      )
        ? updateValue
        : Option<FloatValueAt>.None()
      ;
  }

  public void UpdateLastMetric(
    string rootUrn,
    string suffix,
    TimeSpan instant,
    float value
  )
  {
    _lastUpdateValue[MetricUrn(
      rootUrn,
      suffix
    )] = new FloatValueAt(
      instant,
      value
    );
  }

  public void RemoveLastMetric(
    string rootUrn,
    string suffix
  )
  {
    _lastUpdateValue.Remove(
      MetricUrn(
        rootUrn,
        suffix
      )
    );
  }

  public void UpdateStartAt(
    string rootUrn,
    TimeSpan instant
  )
  {
    _start[rootUrn] = instant;
  }

  public void UpdateEndAt(
    string rootUrn,
    TimeSpan instant
  )
  {
    _end[rootUrn] = instant;
  }

  public void SetupTimeSeries(
    string rootUrn,
    string[] suffixes,
    TimeSpan retentionPeriod
  )
  {
    _retentionPeriod = retentionPeriod;
  }

  public void SetupTimeSeries(
    string key,
    TimeSpan retentionPeriod
  )
  {
    SetupTimeSeries(
      key,
      Array.Empty<string>(),
      retentionPeriod
    );
  }

  public void WriteTsAndApplyRetention(
    string key,
    TimeSpan at,
    float value
  )
  {
    throw new NotImplementedException();
  }

  public void DeleteTs(
    string key,
    TimeSpan? from = null,
    TimeSpan? to = null
  )
  {
    throw new NotImplementedException();
  }

  public void AddValueAtPublish(
    string rootUrn,
    string suffix,
    TimeSpan instant,
    float value
  )
  {
    var metricUrn = MetricUrn(
      rootUrn,
      suffix
    );

    if (!_publishedValues.ContainsKey(metricUrn))
      _publishedValues[metricUrn] = new Dictionary<TimeSpan, FloatValueAt>();

    _publishedValues[metricUrn][instant] = new FloatValueAt(
      instant,
      value
    );

    _lastPublishInstant[rootUrn] = instant;
  }

  private static string MetricUrn(
    string rootUrn,
    string suffix
  )
  {
    return suffix == ""
      ? rootUrn
      : Language.Model.MetricUrn.Build(
        rootUrn,
        suffix
      );
  }
}
