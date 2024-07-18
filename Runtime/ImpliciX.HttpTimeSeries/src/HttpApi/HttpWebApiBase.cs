using System.Net;

namespace ImpliciX.HttpTimeSeries.HttpApi;

abstract class HttpWebApiBase
{
    protected ApiMap Map { get; init; } = new();

    public IEndPointResult Execute(ApiRequest request)
    {
        var func = Map.FuncFor(request.HttpMethod, request.RawUrl);
        return func != null
            ? func(request)
            : new EndPointResult(HttpStatusCode.NotFound, "Not Found");
    }
}