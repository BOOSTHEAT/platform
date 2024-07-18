using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Harmony.Messages;
using ImpliciX.Harmony.Publishers;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Harmony.Tests.Publishers
{
  public class LiveDataTest
  {
    private LiveData _liveData;
    private Queue<IHarmonyMessage> _queue;
    
    [SetUp]
    public void Setup()
    {
      _queue = new Queue<IHarmonyMessage>();
      _liveData = new LiveData(new HarmonyModuleDefinition.LiveDataModel()
      {
        Presence = test_model.has_live_data,
        Period = TimeSpan.FromMinutes(1),
        Content = new Urn[]
        {
          test_model.temperature,
          test_model.pressure,
          test_model.burner_status
        }
      }, _queue);
    }
   
    [Test]
    public void should_create_harmony_message_from_properties()
    {
      var propertiesChanged = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Presence>.Create(test_model.has_live_data, Presence.Enabled, TimeSpan.Zero),
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(300), TimeSpan.Zero),
        Property<Pressure>.Create(test_model.pressure, Pressure.FromFloat(10000).Value, TimeSpan.Zero),
        Property<GasBurnerStatus>.Create(test_model.burner_status, GasBurnerStatus.Faulted, TimeSpan.Zero),
      }, TimeSpan.Zero);
      var dt = new DateTime(2021, 07, 12, 16, 46, 0, 0, DateTimeKind.Utc);

      _liveData.Handles(propertiesChanged);
      _liveData.Handles(SystemTicked.Create(new TimeSpan(dt.Ticks),1000,0));
      
      var message = _queue.Peek();
      Check.That(message.GetMessageType()).IsEqualTo("LiveData");
      var expectedMessage =
        "{\"SerialNumber\":\"SN\",\"DateTime\":\"2021-07-12T16:46:00.000000+00:00\",\"Data\":{\"root:temperature\":300,\"root:pressure\":10000,\"root:burner_status\":-1}}";
      Check.That(message.Format(new ContextStub("SN"))).IsEqualTo(expectedMessage);
    }

    [Test]
    public void should_create_harmony_message_from_selected_properties_only()
    {
      var propertiesChanged = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Presence>.Create(test_model.has_live_data, Presence.Enabled, TimeSpan.Zero),
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(300), TimeSpan.Zero),
        Property<Pressure>.Create(test_model.pressure, Pressure.FromFloat(10000).Value, TimeSpan.Zero),
        Property<Energy>.Create(test_model.energy, Energy.FromFloat(10000).Value, TimeSpan.Zero)
      }, TimeSpan.Zero);
      var dt = new DateTime(2021, 07, 12, 16, 46, 0, 0, DateTimeKind.Utc);

      _liveData.Handles(propertiesChanged);
      _liveData.Handles(SystemTicked.Create(new TimeSpan(dt.Ticks),1000,0));

      var message = _queue.Peek();
      Check.That(message.GetMessageType()).IsEqualTo("LiveData");
      var expectedMessage =
        "{\"SerialNumber\":\"SN\",\"DateTime\":\"2021-07-12T16:46:00.000000+00:00\",\"Data\":{\"root:temperature\":300,\"root:pressure\":10000}}";
      Check.That(message.Format(new ContextStub("SN"))).IsEqualTo(expectedMessage);
    }
    
    [Test]
    public void should_always_contains_all_selected_properties()
    {
      var propertiesChanged1 = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Presence>.Create(test_model.has_live_data, Presence.Enabled, TimeSpan.Zero),
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(200), TimeSpan.Zero),
        Property<Pressure>.Create(test_model.pressure, Pressure.FromFloat(10000).Value, TimeSpan.Zero)
      }, TimeSpan.Zero);
      var propertiesChanged2 = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(300), TimeSpan.Zero),
        Property<Energy>.Create(test_model.energy, Energy.FromFloat(10000).Value, TimeSpan.Zero)
      }, TimeSpan.Zero);
      var dt = new DateTime(2021, 07, 12, 16, 46, 0, 0, DateTimeKind.Utc);

      _liveData.Handles(propertiesChanged1);
      _liveData.Handles(propertiesChanged2);
      _liveData.Handles(SystemTicked.Create(new TimeSpan(dt.Ticks),1000,0));

      var message = _queue.Peek();
      Check.That(message.GetMessageType()).IsEqualTo("LiveData");
      var expectedMessage =
        "{\"SerialNumber\":\"SN\",\"DateTime\":\"2021-07-12T16:46:00.000000+00:00\",\"Data\":{\"root:temperature\":300,\"root:pressure\":10000}}";
      Check.That(message.Format(new ContextStub("SN"))).IsEqualTo(expectedMessage);
    }
    
    [Test]
    public void should_enqueue_message_at_specified_interval()
    {
      var origin = new TimeSpan(new DateTime(2021, 07, 12, 15, 0, 0, 0, DateTimeKind.Utc).Ticks);
      var propertiesChanged1 = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Presence>.Create(test_model.has_live_data, Presence.Enabled, TimeSpan.Zero),
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(200), TimeSpan.Zero),
        Property<Pressure>.Create(test_model.pressure, Pressure.FromFloat(10000).Value, TimeSpan.Zero)
      }, TimeSpan.Zero);
      var propertiesChanged2 = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(250), TimeSpan.Zero),
        Property<Energy>.Create(test_model.energy, Energy.FromFloat(10000).Value, TimeSpan.Zero)
      }, TimeSpan.Zero);
      var propertiesChanged3 = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(300), TimeSpan.Zero),
        Property<Pressure>.Create(test_model.pressure, Pressure.FromFloat(10000).Value, TimeSpan.Zero),
        Property<Energy>.Create(test_model.energy, Energy.FromFloat(10000).Value, TimeSpan.Zero)
      }, TimeSpan.Zero);


      _liveData.Handles(propertiesChanged1);
      _liveData.Handles(SystemTicked.Create(origin, 1000,0));
      _liveData.Handles(propertiesChanged2);
      _liveData.Handles(SystemTicked.Create(origin, 1000,28));
      _liveData.Handles(propertiesChanged3);
      _liveData.Handles(SystemTicked.Create(origin, 1000,60));
      
      Check.That(_queue.Count).IsEqualTo(2);
      Check.That(_queue.Select(m => m.Format(new ContextStub("SN")))).IsEqualTo(
        new []
        {
          "{\"SerialNumber\":\"SN\",\"DateTime\":\"2021-07-12T15:00:00.000000+00:00\",\"Data\":{\"root:temperature\":200,\"root:pressure\":10000}}",
          "{\"SerialNumber\":\"SN\",\"DateTime\":\"2021-07-12T15:01:00.000000+00:00\",\"Data\":{\"root:temperature\":300,\"root:pressure\":10000}}"
        }
      );
    }
    
    [Test]
    public void should_enqueue_only_when_service_is_active()
    {
      var origin = new TimeSpan(new DateTime(2021, 07, 12, 15, 0, 0, 0, DateTimeKind.Utc).Ticks);
      var propertiesChanged1 = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Presence>.Create(test_model.has_live_data, Presence.Disabled, TimeSpan.Zero),
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(200), TimeSpan.Zero),
        Property<Pressure>.Create(test_model.pressure, Pressure.FromFloat(10000).Value, TimeSpan.Zero)
      }, TimeSpan.Zero);
      var propertiesChanged2 = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Presence>.Create(test_model.has_live_data, Presence.Enabled, TimeSpan.Zero),
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(250), TimeSpan.Zero),
        Property<Energy>.Create(test_model.energy, Energy.FromFloat(10000).Value, TimeSpan.Zero)
      }, TimeSpan.Zero);
      var propertiesChanged3 = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(300), TimeSpan.Zero),
        Property<Energy>.Create(test_model.energy, Energy.FromFloat(10000).Value, TimeSpan.Zero)
      }, TimeSpan.Zero);


      _liveData.Handles(propertiesChanged1);
      _liveData.Handles(SystemTicked.Create(origin, 1000,0));
      _liveData.Handles(propertiesChanged2);
      _liveData.Handles(SystemTicked.Create(origin, 1000,28));
      _liveData.Handles(propertiesChanged3);
      _liveData.Handles(SystemTicked.Create(origin, 1000,60));
      
      Check.That(_queue.Count).IsEqualTo(1);
      Check.That(_queue.Select(m => m.Format(new ContextStub("SN")))).IsEqualTo(
        new []
        {
          "{\"SerialNumber\":\"SN\",\"DateTime\":\"2021-07-12T15:01:00.000000+00:00\",\"Data\":{\"root:temperature\":300,\"root:pressure\":10000}}"
        }
      );
    }
    
        
    [Test]
    public void should_not_enqueue_unchanged_value_since_last_enqueue()
    {
      var origin = new TimeSpan(new DateTime(2021, 07, 12, 15, 0, 0, 0, DateTimeKind.Utc).Ticks);
      var propertiesChanged1 = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Presence>.Create(test_model.has_live_data, Presence.Enabled, TimeSpan.Zero),
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(200), TimeSpan.Zero),
        Property<Pressure>.Create(test_model.pressure, Pressure.FromFloat(10000).Value, TimeSpan.Zero)
      }, TimeSpan.Zero);
      var propertiesChanged2 = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(250), TimeSpan.Zero)
      }, TimeSpan.Zero);
      var propertiesChanged3 = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Pressure>.Create(test_model.pressure, Pressure.FromFloat(15000).Value, TimeSpan.Zero)
      }, TimeSpan.Zero);


      _liveData.Handles(propertiesChanged1);
      _liveData.Handles(SystemTicked.Create(origin, 1000,0));
      _liveData.Handles(propertiesChanged2);
      _liveData.Handles(SystemTicked.Create(origin, 1000,60));
      _liveData.Handles(propertiesChanged3);
      _liveData.Handles(SystemTicked.Create(origin, 1000,120));
      
      Check.That(_queue.Count).IsEqualTo(3);
      Check.That(_queue.Select(m => m.Format(new ContextStub("SN")))).IsEqualTo(
        new []
        {
          "{\"SerialNumber\":\"SN\",\"DateTime\":\"2021-07-12T15:00:00.000000+00:00\",\"Data\":{\"root:temperature\":200,\"root:pressure\":10000}}",
          "{\"SerialNumber\":\"SN\",\"DateTime\":\"2021-07-12T15:01:00.000000+00:00\",\"Data\":{\"root:temperature\":250}}",
          "{\"SerialNumber\":\"SN\",\"DateTime\":\"2021-07-12T15:02:00.000000+00:00\",\"Data\":{\"root:pressure\":15000}}"
        }
      );
    }

  }
}