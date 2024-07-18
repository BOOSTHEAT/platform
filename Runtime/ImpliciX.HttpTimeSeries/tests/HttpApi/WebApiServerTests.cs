using System.Net;
using ImpliciX.HttpTimeSeries.HttpApi;

namespace ImpliciX.HttpTimeSeries.Tests.HttpApi;

public class WebApiServerTests
{
    [TestCaseSource(nameof(GetTestCases))]
    public void test_nominal_get(string route, HttpStatusCode expectedStatus, string expectedContent)
    {
        using var sut = new WebApiServer("localhost", 8080, new DummyWebApi());
        using var client = new HttpClient();
        var response = client.GetAsync($"http://localhost:8080{route}").Result;
        var content = response.Content.ReadAsStringAsync().Result;
        Assert.That(response.StatusCode, Is.EqualTo(expectedStatus));
        Assert.That(content, Is.EqualTo(expectedContent));
    }

    [TestCaseSource(nameof(PostTestCases))]
    public void test_nominal_post(string route, string payload, HttpStatusCode expectedStatus, string expectedContent)
    {
        using var sut = new WebApiServer("localhost", 8080, new DummyWebApi());
        using var client = new HttpClient();
        var response = client.PostAsync($"http://localhost:8080{route}", new StringContent(payload)).Result;
        var content = response.Content.ReadAsStringAsync().Result;
        Assert.That(response.StatusCode, Is.EqualTo(expectedStatus));
        Assert.That(content, Is.EqualTo(expectedContent));
    }
    
    public static object[] GetTestCases =
    {
        new object[] {"/", HttpStatusCode.OK, "welcome"},
        new object[] {"/hello", HttpStatusCode.OK , "hello world"},
        new object[] {"/boom", HttpStatusCode.NotFound, "Not Found"},
    };
    
    public static object[] PostTestCases =
    {
        new object[] {"/foo", "joe", HttpStatusCode.OK, "welcome joe"},
        new object[] {"/boom", "", HttpStatusCode.NotFound, "Not Found"},
    };
}

class DummyWebApi : HttpWebApiBase
{
    public DummyWebApi()
    {
        Map = new ApiMap()
            .AddHttpGet("/", _ => new EndPointResult(HttpStatusCode.OK, "welcome"))
            .AddHttpGet("/hello", _ => new EndPointResult(HttpStatusCode.OK, "hello world"))
            .AddHttpPost("/foo", context => new EndPointResult(HttpStatusCode.OK, $"welcome {context.Body}"))
            ;
    }
}
