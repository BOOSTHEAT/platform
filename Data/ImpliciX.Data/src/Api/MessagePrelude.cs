namespace ImpliciX.Data.Api;

public class MessagePrelude : Message
{
  public MessagePrelude()
  {
    Kind = MessageKind.prelude;
  }

  public string Name { get; set; }
  public string Version { get; set; }
  public string Setup { get; set; }
  public string[] Setups { get; set; }
}