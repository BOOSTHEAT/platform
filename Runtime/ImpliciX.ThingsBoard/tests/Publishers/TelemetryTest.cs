using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.ThingsBoard.Messages;
using ImpliciX.ThingsBoard.Publishers;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.ThingsBoard.Tests.Publishers
{
  public class TelemetryTest
  {
    private Telemetry _telemetry;
    private Queue<IThingsBoardMessage> _queue;
    private readonly string _expectedTopic = "v1/devices/me/telemetry";
    
    [SetUp]
    public void Setup()
    {
      _queue = new Queue<IThingsBoardMessage>();
      _telemetry = new Telemetry(new (Urn,TimeSpan)[]
      {
        (test_model.temperature, TimeSpan.FromSeconds(1)),
        (test_model.pressure,TimeSpan.FromMinutes(1)),
        (test_model.burner_status,TimeSpan.FromHours(1)),
        (test_model.metric1_simple,TimeSpan.FromSeconds(1)),
        (test_model.metric2_simple,TimeSpan.FromSeconds(1)),
        (test_model.metric3_composite,TimeSpan.FromSeconds(1)),
        (test_model.metric4_composite,TimeSpan.FromSeconds(1)),
      }, _queue);
    }

    [Test]
    public void should_create_telemetry_from_properties()
    {
      var propertiesChanged = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(300), TimeSpan.Zero),
        Property<Pressure>.Create(test_model.pressure, Pressure.FromFloat(10000).Value, TimeSpan.Zero),
        Property<GasBurnerStatus>.Create(test_model.burner_status, GasBurnerStatus.Faulted, TimeSpan.Zero),
      }, TimeSpan.Zero);
      var dt = new DateTime(2021, 07, 12, 16, 46, 0, 0, DateTimeKind.Utc);

      _telemetry.Handles(propertiesChanged);
      _telemetry.Handles(SystemTicked.Create(new TimeSpan(dt.Ticks),1000,0));
      
      var message = _queue.Peek();
      Check.That(message.GetTopic()).IsEqualTo(_expectedTopic);
      var expectedMessage =
        "{\"ts\":1626108360000,\"values\":{\"root:temperature\":300,\"root:pressure\":10000,\"root:burner_status\":-1}}";
      Check.That(message.Format(null)).IsEqualTo(expectedMessage);
    }

    [Test]
    public void should_create_telemetry_from_selected_properties_only()
    {
      var propertiesChanged = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(300), TimeSpan.Zero),
        Property<Pressure>.Create(test_model.pressure, Pressure.FromFloat(10000).Value, TimeSpan.Zero),
        Property<Energy>.Create(test_model.energy, Energy.FromFloat(10000).Value, TimeSpan.Zero)
      }, TimeSpan.Zero);
      var dt = new DateTime(2021, 07, 12, 16, 46, 0, 0, DateTimeKind.Utc);

      _telemetry.Handles(propertiesChanged);
      _telemetry.Handles(SystemTicked.Create(new TimeSpan(dt.Ticks),1000,0));

      var message = _queue.Peek();
      Check.That(message.GetTopic()).IsEqualTo(_expectedTopic);
      var expectedMessage =
        "{\"ts\":1626108360000,\"values\":{\"root:temperature\":300,\"root:pressure\":10000}}";
      Check.That(message.Format(null)).IsEqualTo(expectedMessage);
    }

    [Test]
    public void should_enqueue_message_at_specified_interval()
    {
      var origin = new TimeSpan(new DateTime(2021, 07, 12, 15, 0, 0, 0, DateTimeKind.Utc).Ticks);
      void PropertyChanged(int n) => _telemetry.Handles(
        PropertiesChanged.Create(new IDataModelValue[]
        {
          Property<Pressure>.Create(test_model.pressure, Pressure.FromFloat(n).Value, TimeSpan.Zero)
        }, TimeSpan.Zero));
      void Tick(uint n) => _telemetry.Handles(SystemTicked.Create(origin, 1000, n));

      PropertyChanged(0);
      Tick(0);
      Tick(30);
      PropertyChanged(1);
      Tick(40);
      PropertyChanged(2);
      Tick(50);
      Tick(60);
      Tick(120);
      Tick(130);
      PropertyChanged(3);
      Tick(140);
      Tick(180);
      PropertyChanged(4);
      Tick(190);
      Tick(200);
      
      Check.That(_queue.Count).IsEqualTo(4);
      Check.That(_queue.Select(m => m.Format(null))).IsEqualTo(
        new []
        {
          "{\"ts\":1626102000000,\"values\":{\"root:pressure\":0}}",
          "{\"ts\":1626102060000,\"values\":{\"root:pressure\":2}}",
          "{\"ts\":1626102140000,\"values\":{\"root:pressure\":3}}",
          "{\"ts\":1626102200000,\"values\":{\"root:pressure\":4}}",
        }
      );
    }
    
    [Test]
    public void should_create_single_telemetry_from_metrics_properties()
    {
      var dt = new DateTime(2021, 07, 12, 16, 46, 0, 0, DateTimeKind.Utc);
      var end = dt - TimeSpan.FromSeconds(1);
      var start = end - TimeSpan.FromHours(1);
      MetricValue CreateMetricValue(float v) => new MetricValue(v, new TimeSpan(start.Ticks), new TimeSpan(end.Ticks));

      _telemetry.Handles(PropertiesChanged.Create(test_model.metric1_simple, new IDataModelValue[]
      {
        Property<MetricValue>.Create(test_model.metric1_simple, CreateMetricValue(12), TimeSpan.Zero),
      }, TimeSpan.Zero));
      
      _telemetry.Handles(PropertiesChanged.Create(test_model.metric2_simple, new IDataModelValue[]
      {
        Property<MetricValue>.Create(test_model.metric2_simple, CreateMetricValue(15), TimeSpan.Zero),
      }, TimeSpan.Zero));
      
      _telemetry.Handles(PropertiesChanged.Create(test_model.metric3_composite, new IDataModelValue[]
      {
        Property<MetricValue>.Create(MetricUrn.BuildOccurence(test_model.metric3_composite, "Disabled"), CreateMetricValue(45), TimeSpan.Zero),
        Property<MetricValue>.Create(MetricUrn.BuildDuration(test_model.metric3_composite, "Disabled"), CreateMetricValue(1500), TimeSpan.Zero),
        Property<MetricValue>.Create(MetricUrn.BuildOccurence(test_model.metric3_composite, "Running"), CreateMetricValue(64), TimeSpan.Zero),
        Property<MetricValue>.Create(MetricUrn.BuildDuration(test_model.metric3_composite, "Running"), CreateMetricValue(2100), TimeSpan.Zero),
      }, TimeSpan.Zero));
      
      _telemetry.Handles(PropertiesChanged.Create(test_model.metric4_composite, new IDataModelValue[]
      {
        Property<MetricValue>.Create(MetricUrn.BuildSamplesCount(test_model.metric4_composite), CreateMetricValue(28), TimeSpan.Zero),
        Property<MetricValue>.Create(MetricUrn.BuildAccumulatedValue(test_model.metric4_composite), CreateMetricValue(125698), TimeSpan.Zero),
      }, TimeSpan.Zero));
      
      _telemetry.Handles(SystemTicked.Create(new TimeSpan(dt.Ticks),1000,0));
      
      var message = _queue.Peek();
      Check.That(message.GetTopic()).IsEqualTo(_expectedTopic);
      var expectedMessage =
        @"{
          'ts':1626108360000,
          'values':{
            'root:metric1_simple':{
              'start':1626104759000,
              'end':1626108359000,
              'value':12
            },
            'root:metric2_simple':{
              'start':1626104759000,
              'end':1626108359000,
              'value':15
            },
            'root:metric3_composite':{
              'start':1626104759000,
              'end':1626108359000,
              'Disabled:occurrence':45,
              'Disabled:duration':1500,
              'Running:occurrence':64,
              'Running:duration':2100
            },
            'root:metric4_composite':{
              'start':1626104759000,
              'end':1626108359000,
              'samples_count':28,
              'accumulated_value':125698
            }
          }
        }";
      Check.That(message.Format(null)).IsEqualTo(CreateJsonSerialization(expectedMessage));
    }

    private string CreateJsonSerialization(string message)
    {
      using var stream = new System.IO.MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        var jsonDocument = JsonDocument.Parse(message.Replace('\'', '"'));
        jsonDocument.WriteTo(writer);
      } 
      return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }


    private static object[] _datetimeAndTimestamps = {
      new object[] { new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), 0L },
      new object[] { new DateTime(2021, 7, 12, 16, 46, 0, DateTimeKind.Utc), 1626108360000L },
      new object[] { new DateTime(2021, 7, 12, 16, 45, 59, DateTimeKind.Utc), 1626108359000L },
      new object[] { new DateTime(2021, 7, 12, 15, 45, 59, DateTimeKind.Utc), 1626104759000L },
    };

    [TestCaseSource(nameof(_datetimeAndTimestamps))]
    public void convert_datetime_to_timestamp(DateTime dt, long expectedTimestamp)
    {
      Assert.That(Telemetry.GetUnixTimestamp(dt), Is.EqualTo(expectedTimestamp));
    }

  }
}