using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.CommunicationMetrics
{
    public class CommunicationMetricsService
    {
        private readonly Dictionary<Urn, Metric<AnalyticsCommunicationCountersNode>> _metricsDefinitions;
        private readonly DomainEventFactory _domainEventFactory;
        private readonly Dictionary<Urn,CommunicationCountersComputer> _activeComputers;

        public CommunicationMetricsService(Metric<AnalyticsCommunicationCountersNode>[] metricsDefinitions, DomainEventFactory domainEventFactory)
        {
            _metricsDefinitions = metricsDefinitions.ToDictionary(def=>def.InputUrn,def=>def);
            _domainEventFactory = domainEventFactory;
            _activeComputers = new Dictionary<Urn, CommunicationCountersComputer>();
        }

        public DomainEvent[] HandleSlaveCommunication(SlaveCommunicationOccured trigger)
        {
            if (_activeComputers.TryGetValue(trigger.DeviceNode.Urn, out var computer))
            {
                computer.Update(trigger);
            }else if (_metricsDefinitions.TryGetValue(trigger.DeviceNode.Urn, out var definition))
            {
                computer = new CommunicationCountersComputer(definition);
                computer.Update(trigger);
                _activeComputers.Add(trigger.DeviceNode.Urn, computer);
            }
            return Array.Empty<DomainEvent>();
        }

        public DomainEvent[] HandleSystemTicked(SystemTicked systemTicked)
        {
            if (CanHandle(systemTicked))
            {
                Log.Debug("Publish communication counters.", systemTicked.At);
                var props = _activeComputers.Values.SelectMany(c => c.Publish()).ToArray();
                return _domainEventFactory.NewEventResult(props)
                    .Match(
                        _ => Array.Empty<DomainEvent>(), 
                        pc => new DomainEvent[] { pc });
            }
            return Array.Empty<DomainEvent>();
        }


        public bool CanHandle(SlaveCommunicationOccured evt) => _metricsDefinitions.ContainsKey(evt.DeviceNode.Urn);

        public bool CanHandle(SystemTicked evt) => evt.IsMinuteElapsed();
    }
}