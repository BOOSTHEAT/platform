using System;
using System.Collections.Generic;
using InfluxDB.Client.Writes;

namespace ImpliciX.TimeSeries
{
    public interface IInfluxDbAdapter: IDisposable
    {
        bool WritePoints(IEnumerable<PointData> pointData);
    }
}