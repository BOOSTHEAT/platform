using System;
using System.Text;
using System.Threading.Tasks;
using ImpliciX.Language.Core;
using ImpliciX.ThingsBoard.Messages;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace ImpliciX.ThingsBoard.Infrastructure
{
  public interface IMqttAdapter : IDisposable
  {
    public bool SendMessage(IThingsBoardMessage message, IPublishingContext context);
  }

  public class MqttAdapter : IMqttAdapter
  {
    private readonly MqttClientOptions _options;
    private readonly IMqttClient _client;

    private MqttAdapter(ConnectionDetails connectionDetails)
    {
      _options = new MqttClientOptionsBuilder()
        .WithCleanSession()
        .WithTcpServer(connectionDetails.Host)
        .WithCredentials(connectionDetails.AccessToken)
        .Build();
      _client = new MqttFactory().CreateMqttClient();
      _client.ConnectedAsync += args =>
      {
        Log.Debug("Connected to ThingsBoard");
        return Task.CompletedTask;
      };
      _client.DisconnectedAsync += args =>
      {
        Log.Debug("Disconnected from ThingsBoard");
        return Task.CompletedTask;
      };
      _client.ApplicationMessageReceivedAsync += args =>
      {
        Log.Debug("ThingsBoard Message received on {@Topic}", args.ApplicationMessage.Topic);
        return Task.CompletedTask;
      };
    }

    public class ConnectionDetails
    {
      public string Host;
      public string AccessToken;
    }

    public static Result<MqttAdapter> CreateFor(ConnectionDetails connectionDetails)
    {
      try
      {
        var adapter = new MqttAdapter(connectionDetails);
        var task = adapter._client.ConnectAsync(adapter._options);
        task.Wait();
        return adapter;
      }
      catch (Exception)
      {
        return new Error(nameof(MqttAdapter),
          $"Unable to connect to MQTT broker {connectionDetails.Host}");
      }
    }

    public void Dispose()
    {
      _client?.Dispose();
    }

    public bool SendMessage(IThingsBoardMessage message, IPublishingContext context)
    {
      try
      {
        var topic = message.GetTopic();
        var json = message.Format(context);
        var mqttMessage = new MqttApplicationMessageBuilder()
          .WithTopic(topic)
          .WithPayload(Encoding.UTF8.GetBytes(json))
          .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
          .Build();
        var publisher = _client.PublishAsync(mqttMessage);
        publisher.Wait();
        var result = publisher.Result;
        if (!result.IsSuccess)
        {
          Log.Warning("MQTT send message error: {@Error}", result.ReasonString);
          return false;
        }

        Log.Debug("MQTT sent message on topic {@Topic}: {@Json}", topic, json);
        return true;
      }
      catch (Exception e)
      {
        Log.Warning("MQTT send message error: {@Error}", e.Message);
        return false;
      }
    }
  }
}