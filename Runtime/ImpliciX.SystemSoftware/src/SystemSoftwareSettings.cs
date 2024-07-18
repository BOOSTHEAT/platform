using System;

namespace ImpliciX.SystemSoftware
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once UnusedMember.Global
    public class SystemSoftwareSettings
    {
        private string[] _alwaysUpdate;
        private string[] _supportedForUpdate;
        public string CurrentReleaseManifestPath { get; set; }
        public string UpdateManifestFilePath { get; set; }

        public string[] SupportedForUpdate
        {
            get => _supportedForUpdate ?? Array.Empty<string>();
            set => _supportedForUpdate = value;
        }

        public string[] AlwaysUpdate
        {
            get => _alwaysUpdate ?? Array.Empty<string>();
            set => _alwaysUpdate = value;
        }

        public string FallbackReleaseManifestPath { get; set; }
    }
}