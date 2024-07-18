using System;
using System.Collections.Generic;
using ImpliciX.SharedKernel.IO;

namespace ImpliciX.Runtime
{
    public sealed class ApplicationOptions
    {
        public string LocalStoragePath { get; }
        
        
        public StartMode StartMode { get; }

        private readonly IEnvironmentService _environmentService;
        private readonly IReadOnlyDictionary<string, string> _optionsDictionary;

        public ApplicationOptions(IReadOnlyDictionary<string, string> optionsDictionary, IEnvironmentService environmentService)
        {
            _optionsDictionary = optionsDictionary ?? throw new ArgumentNullException(nameof(optionsDictionary));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));

            LocalStoragePath = GetLocalStoragePath();

            StartMode = _optionsDictionary.GetValueOrDefault("START_MODE", "safe")
                .Equals("failfast", StringComparison.OrdinalIgnoreCase)
                ? StartMode.FailFast
                : StartMode.Safe;  
        }

        private string GetLocalStoragePath()
        {
            const string OptionsLocalStorageName = "LOCAL_STORAGE";
            const string EnvImplicixLocalStorageName = "IMPLICIX_LOCAL_STORAGE";

            var localStoragePath = _optionsDictionary.TryGetValue(OptionsLocalStorageName, out var optionLocalStorage)
                ? optionLocalStorage
                : _environmentService.GetEnvironmentVariable(EnvImplicixLocalStorageName);

            return string.IsNullOrEmpty(localStoragePath)
                ? throw new InvalidOperationException(
                    $"Local storage path can not be found in '{EnvImplicixLocalStorageName}' environment variable or '{OptionsLocalStorageName}' appSettings Options section")
                : localStoragePath;
        }
    }

    public enum StartMode
    {
        FailFast,
        Safe
    }
}