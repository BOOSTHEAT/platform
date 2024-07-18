using ImpliciX.ThingsBoard.Infrastructure;
using ImpliciX.ThingsBoard.Messages;
using NUnit.Framework;

namespace ImpliciX.ThingsBoard.Tests.Infrastructure
{
  [TestFixture(Category = "ExcludeFromCI")]
  [Ignore("require mqtt broker")]
  public class MqttAdapterManualTests
  {
    static readonly MqttAdapter.ConnectionDetails Local = new MqttAdapter.ConnectionDetails
    {
      Host = "127.0.0.1",
      AccessToken = "b2vZlooF"
    };
    
    static readonly MqttAdapter.ConnectionDetails ThingsBoardConnection = new MqttAdapter.ConnectionDetails
    {
      Host = "thingsboard.cloud",
      AccessToken = "XXXXXXXXXXXXXX"
    };

    private readonly MqttAdapter.ConnectionDetails _connectionDetails = Local;
    
    [Test]
    public void Connect()
    {
      var sut = MqttAdapter.CreateFor(_connectionDetails);
      Assert.That(sut.IsSuccess);
    }
    
    [Test]
    public void connect_and_publish()
    {
      var sut = MqttAdapter.CreateFor(_connectionDetails);
      Assert.That(sut.IsSuccess);
      Assert.That(sut.Value.SendMessage(new MyMessage(), null));
    }
  }
  
  class MyMessage : IThingsBoardMessage
  {
    public string Format(IPublishingContext context)
    {
      return
        "{'training:return_temperature:measure':315.8,'training:pump_power_consumption:measure':6542.25,'training:PUMP_THROTTLE:measure':68.1}";
    }

    public string GetTopic()
    {
      return "v1/devices/me/telemetry";
    }
  }
}