using System;
using ImpliciX.SharedKernel.Scheduling;
using ImpliciX.SharedKernel.Tools;

namespace ImpliciX.SharedKernel.Modules
{
    public interface IImpliciXModule : IDisposable
    {
        void InitializeResources(IProvideDependency dependencyProvider);
        Action<SchedulingUnit> OnStartSchedulingUnitAction { get; }
        Action<SchedulingUnit> OnStopSchedulingUnitAction { get; }
        string Id { get; }
        IImpliciXFeature Feature { get; }

        bool IsRunning { get; }
        void InitializeDependencies(IConfigureDependency dependencyConfigurator);

        void Start();
        void Stop();
    }
}