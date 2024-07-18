using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Model;

namespace ImpliciX.Metrics.Tests.Helpers;

public static class StateMonitoringTestHelper
{
    public static Property<MetricValue>[] CreateSimpleMetric(
        IEnumerable<(string metric_name, float val,
                int start, int end)>
            propertiesDefinition)
    {
        var properties = new List<Property<MetricValue>>();

        foreach (var prop in propertiesDefinition)
        {
            properties.Add(
             CreateMetricProperty(
                    new[] { prop.metric_name },
                    prop.val, prop.start, prop.end)
            );
        }

        return properties.ToArray();
    }
    
    public static Property<MetricValue>[] CreateStateAccumulatorMetric(
        IEnumerable<(fake_model.PublicState curState, string metric_name, float sample_count, float accumulated_value,
                int start, int end)>
            propertiesDefinition)
    {
        var properties = new List<Property<MetricValue>>();

        foreach (var prop in propertiesDefinition)
        {
            properties.AddRange(new[]
            {
                CreateMetricProperty(
                    new[] { fake_analytics_model.public_state_A, prop.curState.ToString(), prop.metric_name,"samples_count" },
                    prop.sample_count, prop.start, prop.end),
                CreateMetricProperty(
                    new[] { fake_analytics_model.public_state_A, prop.curState.ToString(), prop.metric_name,"accumulated_value" },
                    prop.accumulated_value, prop.start, prop.end)
            });
        }

        return properties.ToArray();
    }

    public static Property<MetricValue>[] CreateStateVariationMetric(
        IEnumerable<(fake_model.PublicState curState, string metric_name, float val, int start, int end)>
            propertiesDefinition)
    {
        var properties = new List<Property<MetricValue>>();

        foreach (var prop in propertiesDefinition)
        {
            properties.Add(
                CreateMetricProperty(new[] { fake_analytics_model.public_state_A, prop.curState.ToString(),prop.metric_name },
                    prop.val, prop.start, prop.end));
        }

        return properties.ToArray();
    }


    public static Property<MetricValue>[] CreateStateMetric(
        IEnumerable<(fake_model.PublicState state, int occurence, int duration, int start, int end)>
            propertiesDefinition)
    {
        var properties = new List<Property<MetricValue>>();

        foreach (var prop in propertiesDefinition)
        {
            properties.AddRange(new[]
            {
                CreateMetricProperty(
                    new[] { fake_analytics_model.public_state_A, prop.state.ToString(),MetricUrn.OCCURRENCE },
                    prop.occurence, prop.start, prop.end),
                CreateMetricProperty(
                    new[] { fake_analytics_model.public_state_A, prop.state.ToString() ,MetricUrn.DURATION},
                    prop.duration, prop.start, prop.end)
            });
        }

        return properties.ToArray();
    }


    private static Property<MetricValue> CreateMetricProperty(
        IEnumerable<string> UrnComponents,
        float value, int startInMinutes,
        int atInMinutes)
    {
        var metricUrn = MetricUrn.Build(MetricUrn.Build(UrnComponents.ToArray()));
        return Property<MetricValue>.Create(metricUrn,
            new MetricValue(value, TimeSpan.FromMinutes(startInMinutes), TimeSpan.FromMinutes(atInMinutes)),
            TimeSpan.FromMinutes(atInMinutes));
    }
}