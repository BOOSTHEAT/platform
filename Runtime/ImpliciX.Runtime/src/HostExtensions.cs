using System.Threading;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Logger;
using ImpliciX.SharedKernel.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImpliciX.Runtime
{
    public static class HostExtensions
    {
        public static IHost SetupApplicationLifeCycle(this IHost @this, ManualResetEvent applicationStarted)
        {
            Log.Logger = new SerilogLogger(Serilog.Log.Logger);

            var applicationLifeTime = @this.Services.GetService<IHostApplicationLifetime>();
            
            var modules = @this.Services.GetService<IImpliciXModule[]>();
            
            applicationLifeTime.ApplicationStarted.Register(() =>
            {
                var eventBus = @this.Services.GetService<IReceiveAppStartSignal>();
                eventBus.SignalApplicationStarted();
                modules.StartAll();
                applicationStarted.Set();
            });

            applicationLifeTime.ApplicationStopped.Register(() =>
            {
                modules.StopAll();
                modules.DisposeAll();
                applicationStarted.Dispose();
            });
            return @this;
        }
    }
}