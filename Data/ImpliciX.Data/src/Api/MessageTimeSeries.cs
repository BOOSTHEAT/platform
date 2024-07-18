using System.Collections.Generic;
using System.Text.Json;

namespace ImpliciX.Data.Api;

public class MessageTimeSeries : Message
{
    public MessageTimeSeries()
    {
        Kind = MessageKind.timeseries;
    }
    public string Urn { get; set; }
    public Dictionary<string, List<TimeSeriesValue>> DataPoints { get; set; }

}