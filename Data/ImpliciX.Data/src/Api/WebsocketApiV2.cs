using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;

namespace ImpliciX.Data.Api
{
  public class WebsocketApiV2
  {
    private readonly MessageKind _kind;
    private Message _message;
    public static WebsocketApiV2 CommandMessage => new (MessageKind.command);
    public static WebsocketApiV2 PropertiesMessage => new (MessageKind.properties);

    public WebsocketApiV2 WithParameter(string urn, string argument = null)
    {
      _message = new MessageCommand()
      {
        Urn = urn,
        Argument = argument ?? ".",
        At = TimeSpan.Zero.ToString(),
      };
      return this;
    }

    public WebsocketApiV2 WithProperties(IEnumerable<(string, string)> props)
    {
      var at = TimeSpan.Zero.ToString();
      var propertyValues = props.Select(arg => new Property
        { Urn = arg.Item1, Value = arg.Item2, At = at }).ToArray();

      _message = _kind switch
      {
        MessageKind.properties => new MessageProperties() { Properties = propertyValues, At = at },
        _ => throw new NotSupportedException()
      };
      return this;
    }

    public string ToJson()
    {
      return _message?.ToJson() ?? "{\"$type\":\"" + _kind + "\"}";
    }

    public static WebsocketApiV2 FromJson(string json) => new WebsocketApiV2(json);


    public WebsocketApiV2 OnPrelude(Action<MessagePrelude> action)
    {
      if(_kind == MessageKind.prelude)
        action((MessagePrelude)_message);
      return this;
    }

    public WebsocketApiV2 OnProperties(Action<IEnumerable<Property>> action)
    {
      if(_kind == MessageKind.properties)
        action(((MessageProperties)_message).Properties);
      return this;
    }

    public WebsocketApiV2 SideEffect(Action action)
    {
      action();
      return this;
    }
    
    public static Result<Message> Parse(string json) =>
      Language.Core.SideEffect.TryRun(
        () => Message.FromJson<Message>(json),
        () => new Error("bad_message_format", $"Message {json} is not well formed."));

    private WebsocketApiV2(string json)
    {
      var parse = Parse(json);
      if (parse.IsSuccess)
      {
        _message = parse.Value;
        _kind = _message.Kind;
      }
    }

    private WebsocketApiV2(MessageKind kind)
    {
      _kind = kind;
    }

  }
}