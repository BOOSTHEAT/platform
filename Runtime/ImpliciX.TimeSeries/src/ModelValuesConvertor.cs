using System;
using System.Collections.Generic;
using ImpliciX.Language.Model;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace ImpliciX.TimeSeries
{
  public static class ModelValuesConvertor
  {
    private const string MeasurementName = "measures";
    private const string MetricsName = "metrics";
    private static readonly DateTime EpochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static List<PointData> ToDataPoints(IEnumerable<IDataModelValue> modelValues, bool metricsOnly)
    {
      var pointData = new List<PointData>();
      foreach (var modelValue in modelValues)
      {
        var dt = new DateTime(modelValue.At.Ticks, DateTimeKind.Utc);
        var ts = dt.Subtract(EpochStart).Round(TimespanExtension.Precision.TenthOfSecond);
        var value = modelValue.ModelValue();

                if (metricsOnly && value is not MetricValue) continue;

        switch (value)
        {
          case MetricValue @float:
            pointData.Add(PointData.Measurement(MetricsName).Field(modelValue.Urn, @float.Value).Timestamp(ts, WritePrecision.Ms));
            break;
          case IFloat @float:
            pointData.Add(PointData.Measurement(MeasurementName).Field(modelValue.Urn, @float.ToFloat()).Timestamp(ts, WritePrecision.Ms));
            break;
          case SubsystemState subsystemState:
            pointData.Add(PointData.Measurement(MeasurementName).Field(modelValue.Urn, Convert.ToInt32(subsystemState.State)).Timestamp(ts, WritePrecision.Ms));
            break;
          case Enum @enum:
            pointData.Add(PointData.Measurement(MeasurementName).Field(modelValue.Urn, Convert.ToInt32(@enum)).Timestamp(ts, WritePrecision.Ms));
            break;
        }
      }

      return pointData;
    }
  }
}