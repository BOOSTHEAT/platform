using System.Collections.Concurrent;

namespace ImpliciX.HttpTimeSeries.HttpApi;

internal sealed class ApiMap
{
    private readonly ConcurrentDictionary<HttpMethod, Dictionary<string, Func<ApiRequest, IEndPointResult>>> _endPointsMap = new();

    public Func<ApiRequest, IEndPointResult>? FuncFor(HttpMethod method, string endPointRawUrl)
    {
        if (_endPointsMap.TryGetValue(method, out var endPoints))
        {
            if (endPoints.TryGetValue(endPointRawUrl, out var endPointExecute))
                return endPointExecute;
        }
        return null;
    }
  
    public ApiMap AddHttpGet(string endPointRawUrl, Func<ApiRequest, IEndPointResult> endPointExecute)
        => AddOrUpdate(HttpMethod.Get, endPointRawUrl, endPointExecute);
    public ApiMap AddHttpPost(string endPointRawUrl, Func<ApiRequest, IEndPointResult> endPointExecute) 
        => AddOrUpdate(HttpMethod.Post, endPointRawUrl, endPointExecute);

    private ApiMap AddOrUpdate(HttpMethod httpMethod, string endPointRawUrl,
        Func<ApiRequest, IEndPointResult> endPointExecute)
    {
        _endPointsMap.AddOrUpdate(httpMethod, new Dictionary<string, Func<ApiRequest, IEndPointResult>>()
        {
            {endPointRawUrl, endPointExecute}
        }, (_, dictionary) =>
        {
            dictionary.Add(endPointRawUrl, endPointExecute);
            return dictionary;
        });
        return this;
    }
}