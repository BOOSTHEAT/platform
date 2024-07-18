using System;
using System.Collections.Generic;
using InfluxDB.Client.Writes;
using Serilog;

namespace ImpliciX.TimeSeries
{
  public class DisasterPrevention : IInfluxDbAdapter
  {
    public DisasterPrevention(int errorThreshold, IInfluxDbAdapter influxDbAdapter)
    {
      ErrorThreshold = errorThreshold;
      this.influxDbAdapter = influxDbAdapter;
    }
    
    public int ErrorCount { get; private set; }
    public readonly int ErrorThreshold;
    public bool Inactive => (ErrorCount > ErrorThreshold) && (ErrorCount % ErrorThreshold != 0);
    private readonly IInfluxDbAdapter influxDbAdapter;

    public void Dispose()
    {
    }

    public bool WritePoints(IEnumerable<PointData> pointData)
    {
      if (Inactive)
      {
        ErrorCount++;
        return false;
      }
      try
      {
        influxDbAdapter.WritePoints(pointData);
        ErrorCount = 0;
        return true;
      }
      catch (Exception e)
      {
        ErrorCount++;
        Log.Warning(e,"Failed to write time series data");
        if(Inactive)
          Log.Error("Too many time series error. Writing is deactivated.");
        return false;
      }
    }
    
  }
}
