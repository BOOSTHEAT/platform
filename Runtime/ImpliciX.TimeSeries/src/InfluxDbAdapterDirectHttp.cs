using System.Collections.Generic;
using ImpliciX.Language.Core;
using InfluxDB.Client;
using InfluxDB.Client.Writes;

namespace ImpliciX.TimeSeries
{
    public class InfluxDbAdapterDirectHttp : IInfluxDbAdapter
    {
        private readonly string _bucketName;
        private InfluxDBClient _influxDBClient;
        private WriteApiAsync _writeApi;
        private List<PointData> _batch;
        private readonly int batchSizeLimit;

        public InfluxDbAdapterDirectHttp(string url, string bucketName, string retentionPolicy, int batchSizeLimit)
        {
            this.batchSizeLimit = batchSizeLimit;
            _bucketName = bucketName;
            _influxDBClient = InfluxDBClientFactory.CreateV1(url, "", "".ToCharArray(), bucketName, retentionPolicy);
            _writeApi = _influxDBClient.GetWriteApiAsync();
            _batch = new List<PointData>();
        }

        public bool WritePoints(IEnumerable<PointData> pointData)
        {
            _batch.AddRange(pointData);
            var batchCount = _batch.Count;
            if (batchCount >= batchSizeLimit)
            {
                try
                {
                    _writeApi.WritePointsAsync(_bucketName, "org", _batch).GetAwaiter().GetResult();
                }
                catch
                {
                    foreach (var point in pointData)
                    {
                        Log.Warning("TimeSeries Error during writing point : {@point}", point);
                    }

                    throw;
                }
                finally
                {
                    _batch.Clear();
                }
            }

            return true;
        }

        public void Dispose()
        {
            _influxDBClient.Dispose();
        }
    }
}