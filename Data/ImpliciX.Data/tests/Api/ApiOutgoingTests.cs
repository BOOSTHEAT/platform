using System.Text.Json;
using ImpliciX.Data.Api;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Data.Tests.Api
{
  [TestFixture]
  public class ApiOutgoingTests
  {
    [Test]
    public void create_command_message()
    {
      var message = WebsocketApiV2
        .CommandMessage
        .WithParameter("general:labmode:start", "virtual")
        .ToJson();
      Check.That(message).IsNotNull();
      var json = JsonDocument.Parse(message);
      Check.That(json.RootElement.GetProperty("$type").GetString()).IsEqualTo("command");
      Check.That(json.RootElement.GetProperty("Urn").GetString()).IsEqualTo("general:labmode:start");
      Check.That(json.RootElement.GetProperty("Argument").GetString()).IsEqualTo("virtual");
    }
    
    [Test]
    public void create_command_message_with_empty_argument()
    {
      var message = WebsocketApiV2
        .CommandMessage
        .WithParameter("general:labmode:stop")
        .ToJson();
      Check.That(message).IsNotNull();
      var json = JsonDocument.Parse(message);
      Check.That(json.RootElement.GetProperty("$type").GetString()).IsEqualTo("command");
      Check.That(json.RootElement.GetProperty("Urn").GetString()).IsEqualTo("general:labmode:stop");
      Check.That(json.RootElement.GetProperty("Argument").GetString()).IsEqualTo(".");
    }
    
    [Test]
    public void create_properties_message()
    {
      var message = WebsocketApiV2
        .PropertiesMessage
        .WithProperties(
          new (string, string)[]
          {
            ("service:dhw:top_temperature:measure","1200"),
            ("service:dhw:top_temperature:status","Success")
          })
        .ToJson();
      Check.That(message).IsNotNull();
      var json = JsonDocument.Parse(message);
      Check.That(json.RootElement.GetProperty("$type").GetString()).IsEqualTo("properties");
      var property0 = json.RootElement.GetProperty("Properties")[0];
      Check.That(property0.GetProperty("Urn").GetString()).IsEqualTo("service:dhw:top_temperature:measure");
      Check.That(property0.GetProperty("Value").GetString()).IsEqualTo("1200");
      var property1 = json.RootElement.GetProperty("Properties")[1];
      Check.That(property1.GetProperty("Urn").GetString()).IsEqualTo("service:dhw:top_temperature:status");
      Check.That(property1.GetProperty("Value").GetString()).IsEqualTo("Success");
    }
    
    [Test]
    public void side_effect_during_message_creation()
    {
      var sideEffect = false;
      var message = WebsocketApiV2
        .CommandMessage
        .SideEffect(() => sideEffect = true)
        .ToJson();
      Check.That(message).IsNotNull();
      var json = JsonDocument.Parse(message);
      Check.That(json.RootElement.GetProperty("$type").GetString()).IsEqualTo("command");
      Check.That(sideEffect).IsTrue();
    }
  }
}