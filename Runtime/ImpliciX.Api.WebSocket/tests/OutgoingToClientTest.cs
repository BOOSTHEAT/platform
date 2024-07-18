using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Api;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;
using TimeSeriesValue = ImpliciX.RuntimeFoundations.Events.TimeSeriesValue;

namespace ImpliciX.Api.WebSocket.Tests;

[TestFixture]
public class OutgoingToClientTest
{
  [Test]
  public void output_properties_changed()
  {
    EventsHelper.ModelFactory = new ModelFactory(typeof(dummy).Assembly);
    var evt = EventPropertyChanged(
      new (Urn urn, object value)[]
      {
        (dummy.dummy_property1, PowerSupply.On),
        (dummy.dummy_property2, 0.2f)
      },
      TimeSpan.Zero);
    _sut.PropertiesOutput(evt);
    var sentJson = _spyWs.LatestSentMessage;
    var command = sentJson.RootElement.GetProperty("$type").GetString();
    var properties = sentJson.RootElement
      .GetProperty("Properties").EnumerateArray()
      .Select(p => (p.GetProperty("Urn").GetString(), p.GetProperty("Value").GetSingle())).ToArray();
    Check.That(command).IsEqualTo(MessageKind.properties.ToString());
    Check.That(properties).Contains(("dummy:dummy_property1", 1), ("dummy:dummy_property2", 0.2f));
  }

  [Test]
  public void output_complexproperties_changed()
  {
    var evt = PropertiesChanged.Create(new IDataModelValue[]
    {
      Property<MetricValue>.Create(
        MetricUrn.Build("foo", "bar", "Running", "occurrence"),
        new MetricValue(1, TimeSpan.Zero, TimeSpan.Zero), TimeSpan.Zero),
      Property<MetricValue>.Create(
        MetricUrn.Build("foo", "bar", "Running", "duration"),
        new MetricValue(20, TimeSpan.Zero, TimeSpan.Zero), TimeSpan.Zero),
      Property<MetricValue>.Create(
        MetricUrn.Build("foo", "bar", "Disabled", "occurrence"),
        new MetricValue(2, TimeSpan.Zero, TimeSpan.Zero), TimeSpan.Zero),
      Property<MetricValue>.Create(
        MetricUrn.Build("foo", "bar", "Disabled", "duration"),
        new MetricValue(13, TimeSpan.Zero, TimeSpan.Zero), TimeSpan.Zero)
    }, TimeSpan.Zero);

    _sut.PropertiesOutput(evt);
    var sentJson = _spyWs.LatestSentMessage;
    var type = sentJson.RootElement.GetProperty("$type").GetString();
    var properties = sentJson.RootElement
      .GetProperty("Properties").EnumerateArray()
      .Select(p => (p.GetProperty("Urn").GetString(), p.GetProperty("Value").GetSingle())).ToArray();
    Check.That(type).IsEqualTo(MessageKind.properties.ToString());
    Check.That(properties).ContainsExactly(
      ("foo:bar:Running:occurrence", 1),
      ("foo:bar:Running:duration", 20),
      ("foo:bar:Disabled:occurrence", 2),
      ("foo:bar:Disabled:duration", 13)
    );
  }

  [Test]
  public void output_timeseries_changed()
  {
    var evt = TimeSeriesChanged.Create("fake_analytics:heating:_8Hours",
      new Dictionary<Urn, HashSet<TimeSeriesValue>>
      {
        {
          "fake_analytics:heating:_8Hours:Running:occurence", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(8), 81f),
            new(TimeSpan.FromHours(16), 82f)
          }
        },
        {
          "fake_analytics:heating:_8Hours:Running:duration", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(8), 83600f),
            new(TimeSpan.FromHours(16), 83600f)
          }
        }
      }, TimeSpan.FromHours(17));

    _sut.TimeSeriesOutput(evt);
    var sentJson = _spyWs.LatestSentMessage;
    Check.That(sentJson).IsNotNull();

    var command = sentJson.RootElement.GetProperty("$type").GetString();
    Check.That(command).IsEqualTo(MessageKind.timeseries.ToString());
    var flattenDataPoints = sentJson.RootElement.GetProperty("DataPoints").EnumerateObject().SelectMany(p =>
      p.Value.EnumerateArray().Select(it =>
        (p.Name, it.GetProperty("Value").GetSingle(), it.GetProperty("At").GetString()))).ToArray();


    Check.That(flattenDataPoints).ContainsExactly(
      ("fake_analytics:heating:_8Hours:Running:occurence", 81, "0001-01-01T08:00:00Z"),
      ("fake_analytics:heating:_8Hours:Running:occurence", 82, "0001-01-01T16:00:00Z"),
      ("fake_analytics:heating:_8Hours:Running:duration", 83600, "0001-01-01T08:00:00Z"),
      ("fake_analytics:heating:_8Hours:Running:duration", 83600, "0001-01-01T16:00:00Z")
    );
  }


  [SetUp]
  public void Init()
  {
    _spyWs = new SpyWebSocketServer();
    _sut = new OutgoingToClient(new Guid(), Clock, _spyWs.SendAsync);
  }

  private OutgoingToClient _sut;
  private SpyWebSocketServer _spyWs;
  private Clock Clock => () => TestTime;
  private static readonly TimeSpan TestTime = TimeSpan.FromTicks(new DateTime(2021, 10, 8).Ticks);
}
