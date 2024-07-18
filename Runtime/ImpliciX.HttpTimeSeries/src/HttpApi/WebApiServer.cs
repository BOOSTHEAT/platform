using System.Net;
using ImpliciX.Language.Core;

namespace ImpliciX.HttpTimeSeries.HttpApi;

internal sealed class WebApiServer : IDisposable
{
  private readonly HttpListener _listener;
  private readonly HttpWebApiBase _webApiBase;

  public WebApiServer(
    string hostname,
    int port,
    HttpWebApiBase webApiBase
  )
  {
    _webApiBase = webApiBase;

    _listener = new HttpListener();
    _listener.Prefixes.Add($"http://{hostname}:{port}/");
    _listener.Start();
    Receive();
  }

  public void Dispose()
  {
    _listener.Stop();
    ((IDisposable) _listener).Dispose();
  }

  private void Receive()
  {
    var res = _listener.BeginGetContext(
      ListenerCallback,
      _listener
    );
  }

  private void ListenerCallback(
    IAsyncResult result
  )
  {
    if (!_listener.IsListening) return;
    var context = _listener.EndGetContext(result);

    try
    {
      var apiRequest = ApiRequest.FromHttpRequest(context.Request);
      var endPointResult = _webApiBase.Execute(apiRequest);
      context.Response.StatusCode = (int) endPointResult.StatusCode;
      endPointResult.WriteTo(context.Response.OutputStream);
    }
    catch (Exception e)
    {
      Log.Warning(
        "Unable to return data for URL '{MessageType}' message. Reason={ErrorMessage}",
        context.Request.Url,
        e.Message
      );
      throw;
    }
    finally
    {
      context.Response.Close();
      Receive();
    }
  }
}

public interface IEndPointResult
{
  HttpStatusCode StatusCode { get; }

  void WriteTo(
    Stream outputStream
  );
}

public record EndPointResult(HttpStatusCode StatusCode, string Response) : IEndPointResult
{
  public void WriteTo(
    Stream outputStream
  )
  {
    using var writer = new StreamWriter(outputStream);
    writer.Write(Response);
  }
}
