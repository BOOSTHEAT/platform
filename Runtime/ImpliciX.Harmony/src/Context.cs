using System;
using ImpliciX.Harmony.Infrastructure;

namespace ImpliciX.Harmony
{
    public class Context : IPublishingContext
    {
        public Context(string appName, string dpsUri, int globalRetries, TimeSpan registrationTimeout)
        {
            AppName = appName;
            IotHubSettings = new IotHubSettings();
            DpsSettings = new DpsSettings(dpsUri, registrationTimeout);
            DpsRetryContext = new DpsRetryContext(globalRetries);
            IotHubRetryContext = new IotHubRetryContext(globalRetries);
        }

        public string AppName { get; }
        public string SerialNumber { get; set; }
        public string ReleaseVersion { get; set; }
        public IAzureIoTHubAdapter AzureIoTHubAdapter { get; set; }
        public IotHubSettings IotHubSettings { get; }
        public DpsSettings DpsSettings { get; }
        public IotHubRetryContext IotHubRetryContext { get; }
        public DpsRetryContext DpsRetryContext { get; }
        public string DeviceId { get; set; }
        public string UserTimeZone { get; set; }
    }

    public class DpsRetryContext : HarmonyRetryContext
    {
        public DpsRetryContext(int retries) : base(retries)
        {
        }
    }

    public class IotHubRetryContext : HarmonyRetryContext
    {
        public IotHubRetryContext(int retries) : base(retries)
        {
        }
    }

    public abstract class HarmonyRetryContext
    {
        private readonly int _retries;
        public int RemainingAttempts { get; set; }

        protected HarmonyRetryContext(int retries)
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