namespace ImpliciX.Harmony.Messages
{
    public interface IHarmonyMessage
    {
        string Format(IPublishingContext context);
        string GetMessageType();
    }
}