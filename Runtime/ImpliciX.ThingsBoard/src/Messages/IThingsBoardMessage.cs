namespace ImpliciX.ThingsBoard.Messages
{
  public interface IThingsBoardMessage
  {
    string Format(IPublishingContext context);
    string GetTopic();
  }
}