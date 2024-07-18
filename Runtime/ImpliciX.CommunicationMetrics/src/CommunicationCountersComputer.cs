using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.CommunicationMetrics
{
    public class CommunicationCountersComputer
    {
        private readonly Metric<AnalyticsCommunicationCountersNode> _metricDefinition;

        public CommunicationCountersComputer(Metric<AnalyticsCommunicationCountersNode> metricDefinition)
        {
            _metricDefinition = metricDefinition;
        }

        public void Update(SlaveCommunicationOccured slaveComEvent)
        {
            _successes += slaveComEvent.CommunicationDetails.SuccessCount;
            _failures += slaveComEvent.CommunicationDetails.FailureCount;
            if (slaveComEvent.CommunicationStatus == CommunicationStatus.Fatal)
                _fatal += 1;
        }


        public (Urn urn, object value)[] Publish() =>
            new (Urn urn, object value)[]
            {
                (_metricDefinition.Target.request_count,Counter.FromInteger(_successes + _failures)),
                (_metricDefinition.Target.failed_request_count,Counter.FromInteger(_failures)),
                (_metricDefinition.Target.fatal_request_count,Counter.FromInteger(_fatal))
            };
        
        private ulong _successes;
        private ulong _failures;
        private ulong _fatal;
    }
}