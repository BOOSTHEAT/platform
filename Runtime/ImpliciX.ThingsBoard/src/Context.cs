using ImpliciX.ThingsBoard.Infrastructure;

namespace ImpliciX.ThingsBoard
{
    public class Context : IPublishingContext
    {
        public Context(string appName, ThingsBoardSettings settings)
        {
            AppName = appName;
            RetryContext = new RetryContext(settings.GlobalRetries);
        }
        
        public string AppName { get; }
        public IMqttAdapter Adapter { get; set; }
        public RetryContext RetryContext { get; }
        public string Host { get; set; }
        public string AccessToken { get; set; }
    }

    public class RetryContext
    {
        private readonly int _retries;
        public int RemainingAttempts { get; set; }

        public RetryContext(int retries)
        {
            _retries = retries;
            RemainingAttempts = retries;
        }

        public void ResetRemainingRetries()
        {
            RemainingAttempts = _retries;
        }

        public void DecrementRemainingRetries()
        {
            RemainingAttempts--;
        }
    }
}