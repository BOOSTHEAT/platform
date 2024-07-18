using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using InfluxDB.Client;
using InfluxDB.Client.Writes;

namespace ImpliciX.TimeSeries
{
    public class InfluxDbAdapter : IInfluxDbAdapter
    {
        private readonly string _bucketName;
        private InfluxDBClient _influxDBClient;
        private WriteApi _writeApi;

        public InfluxDbAdapter(string url, string bucketName, string retentionPolicy)
        {
            _bucketName = bucketName;
            _influxDBClient = InfluxDBClientFactory.CreateV1(url, "", "".ToCharArray(), bucketName, retentionPolicy);
            _writeApi = _influxDBClient.GetWriteApi();
            RegisterInfluxHandler();
        }

        public bool WritePoints(IEnumerable<PointData> pointData)
        {
            _writeApi.WritePoints(_bucketName, "org", pointData.ToList());
            return true;
        }
        
        private void RegisterInfluxHandler()
        {
            _writeApi.EventHandler += (sender, args) =>
            {
                switch (args)
                {
                    case WriteErrorEvent e:
                        Log.Error(e.Exception.ToString());
                        break;
                    case WriteSuccessEvent s:
                        Log.Verbose(s.LineProtocol);
                        break;
                    case WriteRetriableErrorEvent re:
                        Log.Warning(re.Exception.ToString());
                        break;
                }
            };
        }
        
        public void Dispose()
        {
            _writeApi.Flush();
            _writeApi.Dispose();
            _influxDBClient.Dispose();
        }
    }
}