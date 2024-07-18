using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Control.DomainEvents;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Control
{
    public class ControlSystem : IImpliciXSystem
    {
        public IReadOnlyList<IImpliciXSystem> SubSystems { get; set; }
        protected IExecutionEnvironment ExecutionEnvironment { get; }

        public ControlSystem(IExecutionEnvironment executionEnvironment = null)
        {
            ExecutionEnvironment = executionEnvironment ?? new ExecutionEnvironment();
        }

        public DomainEvent[] HandleDomainEvent(DomainEvent @event) =>
            @event switch
            {
                CommandRequested commandRequested => HandleCommandRequest(commandRequested),
                PropertiesChanged propertiesChanged => HandlePropertiesChanged(propertiesChanged),
                StateChanged stateChanged => HandleStateChanged(stateChanged),
                SystemTicked systemTicked => HandleSystemTicked(systemTicked),
                TimeoutOccured timeoutOccured => HandleTimeoutOccured(timeoutOccured),
                _ => Array.Empty<DomainEvent>()
            };


        public DomainEvent[] HandleCommandRequest(CommandRequested commandRequest)
        {
            return HandleForAll(subSystem => subSystem.HandleDomainEvent(commandRequest));
        }

        public DomainEvent[] HandleTimeoutOccured(TimeoutOccured timeoutOccured)
        {
            return HandleForAll(subSystem => subSystem.HandleDomainEvent(timeoutOccured));
        }

        public DomainEvent[] HandlePropertiesChanged(PropertiesChanged propertiesChanged)
        {
            propertiesChanged = ExecutionEnvironment.Changed(propertiesChanged);
            return HandleForAll(subSystem => subSystem.HandleDomainEvent(propertiesChanged));
        }

        public DomainEvent[] HandleSystemTicked(SystemTicked systemTicked)
        {
            return HandleForAll(subSystem => subSystem.HandleDomainEvent(systemTicked));
        }

        public DomainEvent[] HandleStateChanged(StateChanged stateChanged) =>
            HandlePropertiesChanged(PropertiesChanged.Create(stateChanged.ModelValues, stateChanged.At));

        private DomainEvent[] HandleForAll(Func<IImpliciXSystem, IEnumerable<DomainEvent>> handler) =>
            MergePropertiesChanged(SubSystems.SelectMany(handler).ToArray());

        private static DomainEvent[] MergePropertiesChanged(DomainEvent[] events)
        {
            var propertiesChanged = new List<PropertiesChanged>();
            var domainEvents = new List<DomainEvent>();
            foreach (var @event in events)
            {
                if (@event is PropertiesChanged changed)
                    propertiesChanged.Add(changed);
                else
                    domainEvents.Add(@event);
            }

            var mergedPropertiesChanged = PropertiesChanged.Join(propertiesChanged.ToArray());
            return mergedPropertiesChanged.Match(() => events, changed => domainEvents.Append(changed).ToArray());
        }

        public bool CanHandle(CommandRequested commandRequested)
        {
            return SubSystems.Any(s => s.CanHandle(commandRequested));
        }

        public DomainEvent[] Activate() => SubSystems.SelectMany(s => s.Activate()).ToArray();
    }
}