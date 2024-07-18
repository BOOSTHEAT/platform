using System.Text.Json;

namespace ImpliciX.Data.Api;

public class MessageProperties: Message
{
    public MessageProperties()
    {
        Kind = MessageKind.properties;
    }
    public Property[] Properties { get; set; }
    public string At { get; set; }
    

}