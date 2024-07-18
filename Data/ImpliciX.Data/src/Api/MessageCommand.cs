using System.Text.Json;

namespace ImpliciX.Data.Api;

public class MessageCommand: Message
{
    public MessageCommand()
    {
        Kind = MessageKind.command;
    }
    public string Urn { get; set; }
    public string Argument { get; set; }
    public string At { get; set; }
}