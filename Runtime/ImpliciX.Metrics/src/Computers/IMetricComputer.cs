using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Metrics.Computers
{
    public interface IMetricComputer
    {
        Urn Root { get; }
        void Update(IDataModelValue modelValue);
        void Update(TimeSpan at);
        Option<Property<MetricValue>[]> Publish(TimeSpan now);
    }
}