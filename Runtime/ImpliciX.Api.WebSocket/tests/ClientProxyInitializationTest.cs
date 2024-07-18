using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Api;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.Runtime;
using ImpliciX.RuntimeFoundations;
using ImpliciX.SharedKernel.IO;
using NFluent;
using NUnit.Framework;
using TimeSeriesValue = ImpliciX.RuntimeFoundations.Events.TimeSeriesValue;
using static ImpliciX.Api.WebSocket.ClientProxy;

namespace ImpliciX.Api.WebSocket.Tests;

public class ClientProxyInitializationTest
{
  [Test]
  public void when_proxy_is_created_the_prelude_is_sent_to_the_client()
  {
    Create(_clientId, _spyWs.SendAsync, null, null, Clock)
      .Start( FakeApplicationRuntimeDefinition("yolo", dummy.version, dummy.environment, new []{"foo", "bar"}),
        () => new IDataModelValue[]
        {
          Property<SoftwareVersion>.Create(dummy.version, SoftwareVersion.Create(6,4,2,888), TimeSpan.Zero),
          Property<Literal>.Create(dummy.environment, Literal.Create("foo"), TimeSpan.Zero)
        }, () => _emptyTimeSeries);
    var sentJson = _spyWs.SentMessages.First();
    Check.That(sentJson).IsNotNull();
    var kind = sentJson.RootElement.GetProperty("$type").GetString();
    Check.That(kind).IsEqualTo(MessageKind.prelude.ToString());
    Check.That(sentJson.RootElement.GetProperty("Name").GetString()).IsEqualTo("yolo");
    Check.That(sentJson.RootElement.GetProperty("Version").GetString()).IsEqualTo("6.4.2.888");
    Check.That(sentJson.RootElement.GetProperty("Setup").GetString()).IsEqualTo("foo");
    var actualSetups = sentJson.RootElement.GetProperty("Setups").EnumerateArray().ToArray();
    Check.That(actualSetups[0].GetString()).IsEqualTo("foo");
    Check.That(actualSetups[1].GetString()).IsEqualTo("bar");
  }
  
  [Test]
  public void prelude_supports_missing_values()
  {
    Create(_clientId, _spyWs.SendAsync, null, null, Clock)
      .Start( FakeApplicationRuntimeDefinition(null, null, null, null),
        () => new IDataModelValue[]
        {
          Property<SoftwareVersion>.Create(dummy.version, SoftwareVersion.Create(6,4,2,888), TimeSpan.Zero),
          Property<Literal>.Create(dummy.environment, Literal.Create("foo"), TimeSpan.Zero)
        }, () => _emptyTimeSeries);
    var sentJson = _spyWs.SentMessages.First();
    Check.That(sentJson).IsNotNull();
    var kind = sentJson.RootElement.GetProperty("$type").GetString();
    Check.That(kind).IsEqualTo(MessageKind.prelude.ToString());
  }

  [Test]
  public void when_proxy_is_created_the_properties_are_sent_to_the_client()
  {
    var modelValues = new IDataModelValue[]
    {
      Property<PowerSupply>.Create(dummy.dummy_property1, PowerSupply.On, TimeSpan.Zero)
    };
    Create(_clientId, _spyWs.SendAsync, null, null, Clock)
      .Start(FakeApplicationRuntimeDefinition(), () => modelValues, () => _emptyTimeSeries);
    var sentJson = _spyWs.LatestSentMessage;
    Check.That(sentJson).IsNotNull();
    var kind = sentJson.RootElement.GetProperty("$type").GetString();
    var properties = sentJson.RootElement
      .GetProperty("Properties").EnumerateArray()
      .Select(p => (p.GetProperty("Urn").GetString(), p.GetProperty("Value").GetInt32())).ToArray();
    Check.That(kind).IsEqualTo(MessageKind.properties.ToString());
    Check.That(properties).Contains(("dummy:dummy_property1", 1));
  }

  [Test]
  public void when_proxy_is_created_the_timeseries_are_sent_to_the_client()
  {
    var ts = new Dictionary<Urn, Dictionary<Urn, HashSet<RuntimeFoundations.Events.TimeSeriesValue>>>
    {
      {
        "fake_analytics:heating:_8Hours", new Dictionary<Urn, HashSet<RuntimeFoundations.Events.TimeSeriesValue>>
        {
          {
            "fake_analytics:heating:_8Hours:Running:occurence", new HashSet<TimeSeriesValue>
            {
              new(TimeSpan.FromHours(8), 81f),
              new(TimeSpan.FromHours(16), 82f)
            }
          }
        }
      }
    };
    Create(_clientId, _spyWs.SendAsync, null, null, Clock)
      .Start(FakeApplicationRuntimeDefinition(), () => new IDataModelValue[] { }, () => ts);
    var sentJson = _spyWs.LatestSentMessage;
    Check.That(sentJson).IsNotNull();
    var command = sentJson.RootElement.GetProperty("$type").GetString();
    var flattenDataPoints = sentJson.RootElement.GetProperty("DataPoints").EnumerateObject().SelectMany(p =>
      p.Value.EnumerateArray().Select(it =>
        (p.Name, it.GetProperty("Value").GetSingle(), it.GetProperty("At").GetString()))).ToArray();

    Check.That(command).IsEqualTo(MessageKind.timeseries.ToString());
    Check.That(flattenDataPoints).ContainsExactly(
      ("fake_analytics:heating:_8Hours:Running:occurence", 81f, "0001-01-01T08:00:00Z"),
      ("fake_analytics:heating:_8Hours:Running:occurence", 82f, "0001-01-01T16:00:00Z")
    );
  }
  

  [SetUp]
  public void Init()
  {
    _clientId = new Guid();
    _spyWs = new SpyWebSocketServer();
  }

  private Guid _clientId;
  private SpyWebSocketServer _spyWs;
  private Clock Clock => () => TestTime;
  private static readonly TimeSpan TestTime = TimeSpan.FromTicks(new DateTime(2021, 10, 8).Ticks);
  private readonly Dictionary<Urn, Dictionary<Urn, HashSet<TimeSeriesValue>>> _emptyTimeSeries = new();

  private ApplicationRuntimeDefinition FakeApplicationRuntimeDefinition(
    string appName = null,
    PropertyUrn<SoftwareVersion> version = null,
    PropertyUrn<Literal> setup = null,
    string[] setups = null
    )
  {
    return new ApplicationRuntimeDefinition(
      new ApplicationDefinition
      {
        AppName = appName,
        DataModelDefinition = new DataModelDefinition
        {
          AppVersion = version,
          AppEnvironment = setup,
        }
      },
      new ApplicationOptions(
        new Dictionary<string, string>
        {
          {"LOCAL_STORAGE","/tmp"}
        },
        new EnvironmentService()),
      setups ?? new string[] {}
      );
  }
}