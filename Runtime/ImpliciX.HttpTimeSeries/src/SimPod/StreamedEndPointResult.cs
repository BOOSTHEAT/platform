using System.Net;
using System.Text;
using ImpliciX.HttpTimeSeries.HttpApi;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.HttpTimeSeries.SimPod;

internal sealed class StreamedEndPointResult : IEndPointResult
{
  private static readonly TimeSpan _epochTicks = new (
    new DateTime(
      1970,
      1,
      1
    ).Ticks
  );

  public StreamedEndPointResult(
    HttpStatusCode statusCode,
    Dictionary<string, IEnumerable<TimeSeriesValue>> payload
  )
  {
    StatusCode = statusCode;
    Payload = payload;
  }

  private Dictionary<string, IEnumerable<TimeSeriesValue>> Payload { get; }

  public HttpStatusCode StatusCode { get; }

  public void WriteTo(
    Stream outputStream
  )
  {
    var messages = new JsonTimeSeriesMessages(Payload);
    WriteToStream(messages.ToJson());

    void WriteToStream(
      string dataToSend
    )
    {
      var bytes = Encoding.UTF8.GetBytes(dataToSend);
      outputStream.Write(
        bytes,
        0,
        bytes.Length
      );
    }
  }
}
