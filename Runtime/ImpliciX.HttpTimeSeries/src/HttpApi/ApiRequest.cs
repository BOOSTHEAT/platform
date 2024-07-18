using System.Net;

namespace ImpliciX.HttpTimeSeries.HttpApi;

internal record ApiRequest
{
  public ApiRequest(string RawUrl, string verb, string Body)
  {
    this.RawUrl = RawUrl;
    HttpMethod = verb.ToUpper() switch
    {
      "GET" => HttpMethod.Get,
      "POST" => HttpMethod.Post,
      _ => throw new ArgumentException($"Unknown verb: {verb}")
    };

    this.Body = Body;
  }

  public string RawUrl { get; }
  public HttpMethod HttpMethod { get; }
  public string Body { get; }

  public static ApiRequest FromHttpRequest(HttpListenerRequest request)
  {
    var streamReader = new StreamReader(request.InputStream);
    return new ApiRequest(request.RawUrl ?? string.Empty, request.HttpMethod, streamReader.ReadToEnd());
  }
}