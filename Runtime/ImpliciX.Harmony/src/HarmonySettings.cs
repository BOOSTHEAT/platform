using System;

namespace ImpliciX.Harmony
{
    public class HarmonySettings
    {
        public string DpsUri { get; set; }
        public int GlobalRetries { get; set; }
        public int RegistrationTimeout { get; set; }
        public uint MessageQueueMaxCapacity { get; set; }
    }

    /// <summary>
    /// This structure is a work in progress until the device connection is initialized
    /// Some of these values might end in dynamic configuration
    /// </summary>
    public class IotHubSettings
    {
        public string Uri { get; set; }
        public string SymmetricKey { get; set; }

    }

    public class DpsSettings
    {
        public DpsSettings(string uri, TimeSpan registrationTimeout)
        {
            Uri = uri;
            RegistrationTimeout = registrationTimeout;
        }

        public string Uri { get; }
        public string IdScope { get; set; }
        public TimeSpan RegistrationTimeout { get; }
    }
}