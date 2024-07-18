using ImpliciX.Language.Model;

namespace ImpliciX.CommunicationMetrics.Tests
{
    public class fake_analytics_model : RootModelNode
    {
        public fake_analytics_model() : base("fake_analytics") {}

        static fake_analytics_model()
        {
            daily_timer = Urn.BuildUrn("fake_analytics", nameof(daily_timer));
            hourly_timer = Urn.BuildUrn("fake_analytics", nameof(hourly_timer));
            minutely_timer = Urn.BuildUrn("fake_analytics", nameof(minutely_timer));
            other_timer = Urn.BuildUrn("fake_analytics", nameof(other_timer));
        }
        
        public static Urn daily_timer { get; }
        public static Urn hourly_timer { get; }
        
        public static Urn minutely_timer { get; }
        public static Urn other_timer { get; }
    }
}