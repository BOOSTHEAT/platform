using System;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.TimeSeries;

public static class TimeSeriesExtensions
{
  public static TimeSeriesUrn Urn(this ITimeSeries timeSeries) =>
    timeSeries switch
    {
      TimeSeriesWithRetention timeSeriesWithRetention => SetRetention(
        timeSeriesWithRetention.Definition.Urn(),
        timeSeriesWithRetention.TimeSpan
        ),
      ModbusSlaveTimeSeries modbusSlaveTimeSeries => ModbusTimeSeriesInfo.CreateUrn(modbusSlaveTimeSeries),
      MinimalistTimeSeries minimalistTimeSeries =>
        new TimeSeriesUrn(minimalistTimeSeries.Urn, Array.Empty<Urn>(), TimeSpan.Zero),
      TimeSeries ts => new TimeSeriesUrn(ts.Urn, ts.Fields, TimeSpan.Zero),
      _ => throw new NotSupportedException($"Unsupported time series: {timeSeries}")
    };

  private static TimeSeriesUrn SetRetention(TimeSeriesUrn urn, TimeSpan timeSpan) => new(urn, urn.Members, timeSpan);
}