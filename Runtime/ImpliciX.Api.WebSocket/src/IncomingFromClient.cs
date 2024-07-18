using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImpliciX.Data.Api;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Api.WebSocket;

public class IncomingFromClient
{
  private readonly Guid _clientId;
  private readonly Clock _clock;
  private readonly ModelFactory _modelFactory;
  private readonly EventBusSend _busSend;

  public IncomingFromClient(Guid clientId, Clock clock, ModelFactory modelFactory, EventBusSend busSend)
  {
    _clientId = clientId;
    _clock = clock;
    _modelFactory = modelFactory;
    _busSend = busSend;
  }
  
  public void Input(ArraySegment<byte> data)
  {
    var json = Encoding.UTF8.GetString(data);
    WebsocketApiV2.Parse(json).Tap(ProcessMessage);
  }

  private void ProcessMessage(Message message)
  {
    (message switch
    {
      MessageProperties msg => CreatePropertiesChanged(msg.Properties),
      MessageCommand msg => CreateCommandRequested(msg),
      _ => Result<DomainEvent[]>.Create(new Error("incorrect_message", "Incorrect message"))
    }).Tap(
      err => Log.Error("WebSocketProxy error client {@client} message {@message}", _clientId, err.Message),
      events => _busSend(events));
  }

  private Result<DomainEvent[]> CreateCommandRequested(MessageCommand message) =>
    from command in _modelFactory.CreateWithLog(
      message.Urn,
      message.Argument,
      _clock())
    select new[] { (DomainEvent)CommandRequested.Create((IModelCommand)command, _clock()) };

  private Result<DomainEvent[]> CreatePropertiesChanged(Property[] messageProperties) =>
    messageProperties
      .Select(p => _modelFactory.CreateWithLog(p.Urn, p.Value, _clock()))
      .Traverse()
      .Match(
        Result<IEnumerable<IDataModelValue>>.Create,
        modelValues => Result<IEnumerable<IDataModelValue>>.Create(modelValues.Cast<IDataModelValue>())
      )
      .Select(props => CreatePropertiesEvents(props).ToArray());

  private IEnumerable<DomainEvent> CreatePropertiesEvents(IEnumerable<IDataModelValue> properties)
  {
    var standardProperties = new List<IDataModelValue>();
    var settingProperties = new List<IDataModelValue>();
    foreach (var property in properties)
    {
      if (property.Urn is ISettingUrn)
        settingProperties.Add(property);
      else
        standardProperties.Add(property);
    }
    var ts = _clock();
    if (standardProperties.Any())
      yield return PropertiesChanged.Create(standardProperties, ts);
    if (settingProperties.Any())
      yield return PersistentChangeRequest.Create(settingProperties, ts);
  }
}