using System;
using System.Diagnostics;
using System.Threading;
using ImpliciX.Language;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.IO;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.SharedKernel.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using static ImpliciX.Runtime.Helpers;
using ExceptionExtensions = ImpliciX.Language.Core.ExceptionExtensions;

namespace ImpliciX.Runtime
{
    public static class Application
    {
        public static void Run(object runtimeModel, string[] args)
        {
            var model = (ApplicationDefinition) runtimeModel;
            Trace.Listeners.Clear();
            var currentEnvironment = GetEnvironmentVariable("IMPLICIX_ENVIRONMENT");
            var applicationStarted = new ManualResetEvent(false);
            try
            {
                BuildHost(model, currentEnvironment, applicationStarted, args)
                    .SetupApplicationLifeCycle(applicationStarted)
                    .Run();
            }
            catch (Exception e)
            {
                DefaultLogger.Error("Unexpected error occurred. Application can't start");
                DefaultLogger.Error("Error: {@message}", ExceptionExtensions.CascadeMessage(e));
                throw;
            }
        }

        private static IHost BuildHost(ApplicationDefinition model, string currentEnvironment, ManualResetEvent applicationStarted,
            string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddJsonFile(model.AppSettingsFile, optional: false, reloadOnChange: false);
                })
                .ConfigureServices((hostingContext, services) =>
                {
                    var configuration = ProgramEnvironment.CreateInstance(hostingContext.Configuration, model, currentEnvironment, ModuleFactory.Create);
                    var modules = configuration.Modules;
                    var busWithFirewall = EventBusWithFirewall.CreateWithFirewall();
                    PublishGlobalProperties(model, currentEnvironment, configuration.Clock, busWithFirewall);
                    services
                        .AddOptions()
                        .AddSingleton(_ => modules)
                        .AddSingleton(_ => configuration.Clock)
                        .AddSingleton<IEventBusWithFirewall>(_ => busWithFirewall)
                        .AddSingleton<IReceiveAppStartSignal>(_ => busWithFirewall)
                        .AddSingleton<IFileSystemService>(new FileSystemService())
                        .AddHostedService(serviceProvider => configuration.Scheduler(applicationStarted, serviceProvider));
                    modules.InitializeDependencies(new DependencyConfigurator(services, hostingContext));
                })
                .UseSerilog((hostingContext, loggerConfiguration) =>
                {
                    var currentSetup = hostingContext.Configuration.GetSection("Setups").GetSection(currentEnvironment);
                    var logId = currentSetup["Log"];
                    var logSetup = hostingContext.Configuration.GetSection("Log").GetSection(logId);
                    loggerConfiguration.ReadFrom.Configuration(logSetup);
                })
                .Build();
    }
}