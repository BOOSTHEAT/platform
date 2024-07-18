namespace ImpliciX.TimeSeries
{
    public class TimeSeriesSettings
    {
        public InfluxDBSettings Storage { get; set; } = new InfluxDBSettings();
        public bool MetricsOnly { get; set; } = false;
    }

    public class InfluxDBSettings
    {
        public string URL { get; set; } = "http://127.0.0.1:8086";
        public string Bucket { get; set; } = "boiler";
        public string RetentionPolicy { get; set; } = "";
        public int HttpBatchSizeLimit { get; set; } = 50;
        public int MaxErrorsBeforeDeactivation { get; set; } = 200;
        
    }
}