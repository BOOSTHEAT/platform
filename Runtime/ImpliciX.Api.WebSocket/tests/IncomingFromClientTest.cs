using System;
using System.Linq;
using System.Text;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Api.WebSocket.Tests;

[TestFixture]
public class IncomingFromClientTest
{
  [Test]
  public void configuration_values_for_enums_can_be_numeric()
  {
    var message = @"
                {
                    ""$type"":""properties"",
                    ""Properties"":[
                        {""Urn"":""dummy:dummy_configuration1"",""Value"":""1"",""At"":""09:00:00.000""},
                        {""Urn"":""dummy:dummy_configuration3"",""Value"":""0"",""At"":""09:00:00.000""}
                    ]
                }
            ";
    var receivedData = Encoding.UTF8.GetBytes(message);
    _sut.Input(receivedData);
    var expected = EventPersistentChangeRequested(
      new (Urn urn, object value)[]
      {
        (dummy.dummy_configuration1, PowerSupply.On),
        (dummy.dummy_configuration3, Presence.Disabled)
      }, TimeSpan.Zero);
    Check.That(_spyBus.RecordedPublications).ContainsExactly(expected);
    Check.That(((PersistentChangeRequest)_spyBus.RecordedPublications[0]).ModelValues.All(mv => mv.At == TestTime))
      .IsTrue();
  }

  [Test]
  public void input_test_properties_successful()
  {
    var message = @"
                    {
                        ""$type"":""properties"",
                        ""Properties"":[
                            {""Urn"":""dummy:dummy_property1"",""Value"":""On"",""At"":""09:00:00.000""},
                            {""Urn"":""dummy:dummy_property2"",""Value"":""0.2"",""At"":""09:00:00.000""}
                        ]
                    }
                    ";
    var receivedData = Encoding.UTF8.GetBytes(message);
    _sut.Input(receivedData);
    var expected = EventPropertyChanged(
      new (Urn urn, object value)[]
      {
        (dummy.dummy_property1, PowerSupply.On),
        (dummy.dummy_property2, 0.2f)
      },
      TestTime);
    Check.That(_spyBus.RecordedPublications).ContainsExactly(expected);
    Check.That(((PropertiesChanged)_spyBus.RecordedPublications[0]).ModelValues.All(mv => mv.At == TestTime))
      .IsTrue();
  }
  
  [Test]
  public void property_change_with_setting_urns_requires_persistence()
  {
    var message = @"
                    {
                        ""$type"":""properties"",
                        ""Properties"":[
                            {""Urn"":""dummy:dummy_property1"",""Value"":""On"",""At"":""09:00:00.000""},
                            {""Urn"":""dummy:dummy_property2"",""Value"":""0.2"",""At"":""09:00:00.000""},
                            {""Urn"":""dummy:dummy_configuration3"",""Value"":""0"",""At"":""09:00:00.000""}
                        ]
                    }
                    ";
    var receivedData = Encoding.UTF8.GetBytes(message);
    _sut.Input(receivedData);
    var expected1 = EventPropertyChanged(
      new (Urn urn, object value)[]
      {
        (dummy.dummy_property1, PowerSupply.On),
        (dummy.dummy_property2, 0.2f)
      },
      TestTime);
    var expected2 = EventPersistentChangeRequested(
      new (Urn urn, object value)[]
      {
        (dummy.dummy_configuration3, Presence.Disabled)
      },
      TestTime);
    Check.That(_spyBus.RecordedPublications).ContainsExactly(expected1, expected2);
    Check.That(((PropertiesChanged)_spyBus.RecordedPublications[0]).ModelValues.All(mv => mv.At == TestTime))
      .IsTrue();
    Check.That(((PersistentChangeRequest)_spyBus.RecordedPublications[1]).ModelValues.All(mv => mv.At == TestTime))
      .IsTrue();
  }

  [Test]
  public void input_test_properties_failure()
  {
    var message = @"
                 {
                     ""$type"":""properties"",
                     ""Properties"":[
                         {""Urn"":""dummy:dummy_property_unknown"",""Value"":""On"",""At"":""09:00:00.000""},
                         {""Urn"":""dummy:dummy_property2"",""Value"":""0.2"",""At"":""09:00:00.000""}
                     ]
                 }
             ";
    var receivedData = Encoding.UTF8.GetBytes(message);
    _sut.Input(receivedData);
    Check.That(_spyBus.RecordedPublications).CountIs(0);
  }


  [Test]
  public void change_configuration_of_function_definition()
  {
    var message = @"
                 {
                     ""$type"":""properties"",
                     ""Properties"":[
                         {""Urn"":""dummy:dummy_func"",""Value"":""a:1|b:2|c:3"",""At"":""09:00:00.000""}
                     ]
                 }
             ";
    var receivedData = Encoding.UTF8.GetBytes(message);
    _sut.Input(receivedData);
    var expected = EventPersistentChangeRequested(
      new (Urn urn, object value)[]
        { (dummy.dummy_func, new FunctionDefinition(new[] { ("a", 1f), ("b", 2f), ("c", 3f) })) }, TimeSpan.Zero);
    Check.That(_spyBus.RecordedPublications).ContainsExactly(expected);
    Check.That(((PersistentChangeRequest)_spyBus.RecordedPublications[0]).ModelValues.All(mv => mv.At == TestTime))
      .IsTrue();
  }
  
  [Test]
  public void input_is_not_a_valid_message()
  {
    var message = @"{";
    var receivedData = Encoding.UTF8.GetBytes(message);
    _sut.Input(receivedData);
    Check.That(_spyBus.RecordedPublications).CountIs(0);
  }


  [Test]
  public void input_test_command()
  {
    var message = @"
{
    ""$type"":""command"",
    ""Urn"": ""dummy:dummy_command"",
    ""Argument"": ""argCommand""
}
";
    var receivedData = Encoding.UTF8.GetBytes(message);
    _sut.Input(receivedData);
    var expected = EventCommandRequested(dummy.dummy_command, "argCommand", TimeSpan.Zero);
    Check.That(_spyBus.RecordedPublications).ContainsExactly(expected);
  }

  [Test]
  public void input_test_configuration()
  {
    var message = @"
{
    ""$type"":""properties"",
    ""Properties"":[
        {""Urn"":""dummy:dummy_configuration1"",""Value"":""On"",""At"":""09:00:00.000""},
        {""Urn"":""dummy:dummy_configuration2"",""Value"":""0.2"",""At"":""09:00:00.000""}
    ]
}
";
    var receivedData = Encoding.UTF8.GetBytes(message);
    _sut.Input(receivedData);
    var expected = EventPersistentChangeRequested(
      new (Urn urn, object value)[]
        { (dummy.dummy_configuration1, PowerSupply.On), (dummy.dummy_configuration2, 0.2f) }, TimeSpan.Zero);
    Check.That(_spyBus.RecordedPublications).ContainsExactly(expected);
    Check.That(((PersistentChangeRequest)_spyBus.RecordedPublications[0]).ModelValues.All(mv => mv.At == TestTime))
      .IsTrue();
  }
  
  [SetUp]
  public void Init()
  {
    _modelFactory = new ModelFactory(typeof(dummy).Assembly);
    _spyBus = new SpyEventBus();
    EventsHelper.ModelFactory = _modelFactory;
    _sut = new IncomingFromClient(new Guid(), Clock, _modelFactory, _spyBus.Publish );
  }

  private IncomingFromClient _sut;
  private SpyEventBus _spyBus;
  private ModelFactory _modelFactory;
  private Clock Clock => () => TestTime;
  private static readonly TimeSpan TestTime = TimeSpan.FromTicks(new DateTime(2021, 10, 8).Ticks);
}
