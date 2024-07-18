using System;
using System.Diagnostics;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using Serilog;
using Serilog.Core;

namespace ImpliciX.Runtime
{
    public static class Helpers
    {

        public static string GetEnvironmentVariable(params string[] names)
        {
            foreach (var name in names)
            {
                var value = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            throw new Exception($"Environment Variable {string.Join(" or ", names)} is not defined");
        }
        
        private static SoftwareVersion GetAppVersion()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var appVersion = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
            var softwareVersion =
                SoftwareVersion.FromString(appVersion).GetValueOrDefault(SoftwareVersion.Create(0, 0, 0, 0));
            return softwareVersion;
        }
        
        public static void PublishGlobalProperties(ApplicationDefinition applicationDefinition, string currentEnvironment,
            IClock clock,
            EventBusWithFirewall bus)
        {
            var dmd = applicationDefinition.DataModelDefinition;
            var globals = (dmd.GlobalProperties ?? Array.Empty<(Urn, object)>()).ToList();
            if(dmd.AppVersion != null)
                globals.Add((dmd.AppVersion, GetAppVersion()));
            if(dmd.AppEnvironment != null)
                globals.Add((dmd.AppEnvironment, currentEnvironment));
            var domainEventFactory = EventFactory.Create(new ModelFactory(dmd.Assembly), clock.Now);
            bus.Publish(domainEventFactory.NewEventResult(globals).Value);
        }
        
        private static Logger _defaultLogger;
        public static ILogger DefaultLogger
        {
            get
            {
                return _defaultLogger ??= new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
            }
        }
    }
}