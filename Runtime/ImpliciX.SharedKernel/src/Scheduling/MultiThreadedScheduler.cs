using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.SharedKernel.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace ImpliciX.SharedKernel.Scheduling
{
    public class MultiThreadedScheduler : ImpliciXScheduler
    {
        private readonly SchedulingUnit[] _schedulingUnits;
        private readonly IEventBusWithFirewall _eventBus;

        public MultiThreadedScheduler(ManualResetEvent applicationStarted, IServiceProvider serviceProvider,
            IImpliciXModule[] modules)
        {
            modules.InitializeResources(new DependencyProvider(serviceProvider));
            _eventBus = serviceProvider.GetService<IEventBusWithFirewall>();
            _schedulingUnits = modules.Select(CreateSchedulingUnit(applicationStarted)).ToArray();
        }


        public MultiThreadedScheduler(IEventBusWithFirewall eventBus, SchedulingUnit[] schedulingUnits)
        {
            _schedulingUnits = schedulingUnits;
            _eventBus = eventBus;
        }

        private Func<IImpliciXModule, SchedulingUnit> CreateSchedulingUnit(ManualResetEvent applicationStarted) =>
            module => new SchedulingUnit(applicationStarted, module.Id, module.Feature, _eventBus, module.OnStartSchedulingUnitAction,
                module.OnStopSchedulingUnitAction);

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var startTasks = _schedulingUnits.Select(a => a.StartAsync(cancellationToken));
            Log.Information("Starting multi threaded scheduler {@ModuleIds}", _schedulingUnits.Select(m => m.Id).ToArray());
            return Task.WhenAll(startTasks);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            var stopTasks = _schedulingUnits.Select(a => a.StopAsync(cancellationToken));
            return Task.WhenAll(stopTasks);
        }
    }
}