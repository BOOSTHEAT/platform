using System;
using System.Collections.Generic;
using ImpliciX.Harmony.Messages;
using ImpliciX.Harmony.Publishers;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Harmony.Tests.Publishers
{
  public class AdditionalIDsTests
  {
    private AdditionalID _additionalIDs;
    private Queue<IHarmonyMessage> _queue;
    
    [SetUp]
    public void Setup()
    {
      _queue = new Queue<IHarmonyMessage>();
      _additionalIDs = new AdditionalID(
        new Dictionary<string,PropertyUrn<Literal>>
        {
          ["ID1"] = test_model.additionalID1,
          ["ID2"] = test_model.additionalID2,
        }, _queue
      );
    }
    
    [Test]
    public void should_create_harmony_message_from_additional_IDs()
    {
      var virtualClock = new VirtualClock(new DateTime(2021, 07, 12, 16, 46, 0, 0, DateTimeKind.Utc));

      var propertiesChanged = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Literal>.Create(test_model.additionalID1, Literal.Create("Foo"), TimeSpan.Zero),
        Property<Presence>.Create(test_model.has_live_data, Presence.Enabled, TimeSpan.Zero),
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(300), TimeSpan.Zero),
        Property<Literal>.Create(test_model.additionalID2, Literal.Create("Bar"), TimeSpan.Zero),
      }, virtualClock.Now());

      _additionalIDs.Handles(propertiesChanged);

      var message = _queue.Peek();
      Check.That(message.GetMessageType()).IsEqualTo("AdditionalID");
      var expectedMessage =
        "{\"SerialNumber\":\"SN\",\"DateTime\":\"2021-07-12T16:46:00.000000+00:00\",\"Data\":{\"ID1\":\"Foo\",\"ID2\":\"Bar\"}}";
      Check.That(message.Format(new ContextStub("SN"))).IsEqualTo(expectedMessage);
    }
    
    [Test]
    public void should_not_send_message_when_properties_are_not_additional_IDs()
    {
      var virtualClock = new VirtualClock(new DateTime(2021, 07, 12, 16, 46, 0, 0, DateTimeKind.Utc));

      var propertiesChanged = PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Presence>.Create(test_model.has_live_data, Presence.Enabled, TimeSpan.Zero),
        Property<Temperature>.Create(test_model.temperature, Temperature.Create(300), TimeSpan.Zero),
      }, virtualClock.Now());

      _additionalIDs.Handles(propertiesChanged);
      Check.That(_queue.Count).IsEqualTo(0);
    }
  }
}