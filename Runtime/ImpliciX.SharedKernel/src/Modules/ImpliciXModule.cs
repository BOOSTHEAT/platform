using System;
using ImpliciX.SharedKernel.Scheduling;
using ImpliciX.SharedKernel.Tools;
using ImpliciX.Language.Core;

namespace ImpliciX.SharedKernel.Modules
{
    public abstract class ImpliciXModule : IImpliciXModule
    {
        private Assets.Assets Assets;
        private Func<Assets.Assets, IImpliciXFeature> _createFeatureFunc;
        private Action<IConfigureDependency> _initDependencies;
        private Func<IProvideDependency, object[]> _initResource;
        private IProvideDependency _dependencyProvider;
        private bool _areResourcesInitialized;
        private bool _isStarted;

        private Func<Assets.Assets, Action<SchedulingUnit>> OnStopSchedulingUnitFunc { get; set; }

        private Func<Assets.Assets, Action<SchedulingUnit>> OnStartSchedulingUnitFunc { get; set; }
        public Action<SchedulingUnit> OnStartSchedulingUnitAction =>
            OnStartSchedulingUnitFunc == null
                ? _ => { }
                : OnStartSchedulingUnitFunc(Assets);
        public Action<SchedulingUnit> OnStopSchedulingUnitAction =>
            OnStopSchedulingUnitFunc == null
                ? _ => { }
                : OnStopSchedulingUnitFunc(Assets);
        private Action<Assets.Assets> OnStartModuleAction { get; set; }
        private Action<Assets.Assets> OnStopModuleAction { get; set; }

        public string Id { get; }

        public ImpliciXModule(string id)
        {
            Id = id;
            _isStarted = false;
            _areResourcesInitialized = false;
        }

        protected ImpliciXModule DefineModule(Action<IConfigureDependency> initDependencies,
            Func<IProvideDependency, object[]> initResources,
            Func<Assets.Assets, IImpliciXFeature> createFeature, Action<Assets.Assets> onModuleStart = null, Action<Assets.Assets> onModuleStop = null)
        {
            OnStartModuleAction = onModuleStart ?? (_ => { });
            OnStopModuleAction = onModuleStop ?? (_ => { });

            _createFeatureFunc = createFeature;
            _initDependencies = initDependencies;
            _initResource = initResources;
            return this;
        }

        public bool IsRunning => _areResourcesInitialized && _isStarted;

        public void InitializeDependencies(IConfigureDependency dependencyConfigurator)
        {
            _initDependencies(dependencyConfigurator);
        }

        public void Start()
        {
            Debug.PreCondition(() => _areResourcesInitialized, () => "Resources should be initialized before starting the module");
            _isStarted = true;
            OnStartModuleAction(Assets);
        }

        public void Stop()
        {
            _isStarted = false;
            OnStopModuleAction(Assets);
        }


        public void InitializeResources(IProvideDependency dependencyProvider)
        {
            _areResourcesInitialized = true;
            _dependencyProvider = dependencyProvider;
            Assets = new Assets.Assets(_initResource(_dependencyProvider));
        }


        protected ImpliciXModule DefineSchedulingUnit(Func<Assets.Assets, Action<SchedulingUnit>> onStart, Func<Assets.Assets, Action<SchedulingUnit>> onStop)
        {
            OnStartSchedulingUnitFunc = onStart;
            OnStopSchedulingUnitFunc = onStop;
            return this;
        }


        public IImpliciXFeature Feature => _createFeatureFunc(Assets);


        public void Dispose()
        {
            Assets.DisposeAll();
        }
    }
}