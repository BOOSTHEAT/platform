using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using ImpliciX.HttpTimeSeries.HttpApi;
using ImpliciX.HttpTimeSeries.SimPod;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.HttpTimeSeries.Tests.SimPod;


public class StreamedEndpointResultsTests
{
    [Test]
    [Ignore("This test is not meant to be run in CI, it's for performance testing")]
    public void test_nominal()
    {
        var sut = new StreamedEndPointResult(HttpStatusCode.OK, new Dictionary<string, IEnumerable<TimeSeriesValue>>
        {
            ["foo"] = GenerateTimeSeries(100_000),
            ["bar"] = GenerateTimeSeries(200_000),
        });
        var sw = new Stopwatch();
        sw.Start();
        using var ms = new MemoryStream();
        sut.WriteTo(ms);
        sw.Stop();
        Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
        
        Assert.That(ms.Length, Is.GreaterThan(0));
        var json = Encoding.UTF8.GetString(ms.ToArray());
        Assert.That(json, Is.Not.Empty);
        var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetArrayLength(), Is.EqualTo(2));
        
        var firstTarget = doc.RootElement[0];
        Assert.That(firstTarget.GetProperty("target").GetString(), Is.EqualTo("foo"));
        Assert.That(firstTarget.GetProperty("datapoints").GetArrayLength(), Is.EqualTo(100_000));
        Assert.That(firstTarget.GetProperty("datapoints")[0][0].GetDouble(), Is.EqualTo(0));
        Assert.That(firstTarget.GetProperty("datapoints")[99_999][0].GetDouble(), Is.EqualTo(99_999));
        
        var secondTarget = doc.RootElement[1];
        Assert.That(secondTarget.GetProperty("target").GetString(), Is.EqualTo("bar"));
        Assert.That(secondTarget.GetProperty("datapoints").GetArrayLength(), Is.EqualTo(200_000));
        Assert.That(secondTarget.GetProperty("datapoints")[0][0].GetDouble(), Is.EqualTo(0));
        Assert.That(secondTarget.GetProperty("datapoints")[199_999][0].GetDouble(), Is.EqualTo(199_999));
    }

    [Test]
    [Ignore("This test is not meant to be run in CI, it's for performance testing")]
    public void test_nominal_over_http()
    {
        using var server = new WebApiServer("localhost", 8989, new TestPerfApi());
        using var client = new HttpClient();
        var sw = new Stopwatch();
        sw.Start();
        var response = client.GetAsync("http://localhost:8989/test").Result;
        sw.Stop();
        Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = response.Content.ReadAsStringAsync().Result;
        Assert.That(json, Is.Not.Empty);
        var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetArrayLength(), Is.EqualTo(2));
        
        var firstTarget = doc.RootElement[0];
        Assert.That(firstTarget.GetProperty("target").GetString(), Is.EqualTo("foo"));
        Assert.That(firstTarget.GetProperty("datapoints").GetArrayLength(), Is.EqualTo(100_000));
        Assert.That(firstTarget.GetProperty("datapoints")[0][0].GetDouble(), Is.EqualTo(0));
        Assert.That(firstTarget.GetProperty("datapoints")[99_999][0].GetDouble(), Is.EqualTo(99_999));
        
        var secondTarget = doc.RootElement[1];
        Assert.That(secondTarget.GetProperty("target").GetString(), Is.EqualTo("bar"));
        Assert.That(secondTarget.GetProperty("datapoints").GetArrayLength(), Is.EqualTo(200_000));
        Assert.That(secondTarget.GetProperty("datapoints")[0][0].GetDouble(), Is.EqualTo(0));
        Assert.That(secondTarget.GetProperty("datapoints")[199_999][0].GetDouble(), Is.EqualTo(199_999));
        
    }

    class TestPerfApi:HttpWebApiBase
    {
        public TestPerfApi()
        {
            Map.AddHttpGet("/test", _ => new StreamedEndPointResult(HttpStatusCode.OK, new Dictionary<string, IEnumerable<TimeSeriesValue>>
            {
                ["foo"] = GenerateTimeSeries(100_000),
                ["bar"] = GenerateTimeSeries(200_000),
            }));
        }
        
    }
    
    private static IEnumerable<TimeSeriesValue> GenerateTimeSeries(int count) =>
        Enumerable.Range(0, count)
            .Select(o => new TimeSeriesValue(TimeSpan.FromMinutes(o), o));
}