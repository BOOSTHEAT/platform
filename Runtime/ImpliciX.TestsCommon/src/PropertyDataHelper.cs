using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Model;
using NFluent;

namespace ImpliciX.TestsCommon
{
    public static class PropertyDataHelper
    {
        public static Property<MetricValue> CreateMetricValueProperty(IEnumerable<string> urnComponents, float value, int startInMinutes, int endInMinutes,
            int? atInMinutes = null)
            => CreateMetricValueProperty(urnComponents, value, TimeSpan.FromMinutes(startInMinutes), TimeSpan.FromMinutes(endInMinutes),
                TimeSpan.FromMinutes(atInMinutes ?? endInMinutes));

        public static Property<MetricValue> CreateMetricValueProperty(string urn, float value, int startInMinutes, int endInMinutes,
            int? atInMinutes = null)
            => CreateMetricValueProperty(new[] {urn}, value, TimeSpan.FromMinutes(startInMinutes), TimeSpan.FromMinutes(endInMinutes),
                TimeSpan.FromMinutes(atInMinutes ?? endInMinutes));

        public static Property<MetricValue> CreateMetricValueProperty(string urn, float value, TimeSpan start, TimeSpan end, TimeSpan? at = null)
            => CreateMetricValueProperty(new[] {urn}, value, start, end, at);

        private static Property<MetricValue> CreateMetricValueProperty(IEnumerable<string> urnComponents, float value, TimeSpan start, TimeSpan end,
            TimeSpan? at = null)
        {
            var propertyUrn = MetricUrn.Build(urnComponents.ToArray());
            return Property<MetricValue>.Create(propertyUrn, new MetricValue(value, start, end), at ?? end);
        }

        public static void CheckAreEquals(Property<MetricValue> challenger, Property<MetricValue> expected)
        {
            Check.That(challenger.At).IsEqualTo(expected.At);
            Check.That(challenger.Urn.Value).IsEqualTo(expected.Urn);
            Check.That(challenger.Value.Value).IsEqualTo(expected.Value.Value);
            Check.That(challenger.Value.SamplingStartDate).IsEqualTo(expected.Value.SamplingStartDate);
            Check.That(challenger.Value.SamplingEndDate).IsEqualTo(expected.Value.SamplingEndDate);
        }

        #region StateMonitoring

        public static Property<MetricValue> CreateStateOccurenceProperty(IEnumerable<string> baseUrnComponents,
            float occurenceValue, int startInMinutes, int atInMinutes)
            => CreateStateMonitoringProperty(MetricUrn.OCCURRENCE, baseUrnComponents, occurenceValue, startInMinutes, atInMinutes);

        public static Property<MetricValue> CreateStateOccurenceProperty(Urn propertyUrn, float value, int startInMinutes, int atInMinutes)
            => CreateStateMonitoringProperty(MetricUrn.OCCURRENCE, new[] {propertyUrn.Value}, value, startInMinutes, atInMinutes);

        public static Property<MetricValue> CreateStateDurationProperty(IEnumerable<string> baseUrnComponents,
            float valueInMinutes, int startInMinutes, int atInMinutes)
            => CreateStateMonitoringProperty(MetricUrn.DURATION, baseUrnComponents, valueInMinutes * 60f, startInMinutes, atInMinutes);

        public static Property<MetricValue> CreateStateDurationProperty(Urn propertyUrn, float valueInMinutes, int startInMinutes, int atInMinutes)
            => CreateStateDurationProperty(new[] {propertyUrn.Value}, valueInMinutes, startInMinutes, atInMinutes);

        private static Property<MetricValue> CreateStateMonitoringProperty(string propertyName, IEnumerable<string> baseUrnComponents, float value,
            int startInMinutes,
            int atInMinutes)
        {
            var samplingStartDate = TimeSpan.FromMinutes(startInMinutes);
            var samplingEndDate = TimeSpan.FromMinutes(atInMinutes);

            var metricUrn = MetricUrn.Build(MetricUrn.Build(baseUrnComponents.Append(propertyName).ToArray()));
            return Property<MetricValue>.Create(metricUrn, new MetricValue(value, samplingStartDate, samplingEndDate), samplingEndDate);
        }

        #endregion

        #region Accumulator

        public static Property<MetricValue> CreateAccumulatorValue(IEnumerable<string> baseUrnComponents, float value, int startInMinutes, int atInMinutes)
            => Property<MetricValue>.Create(
                MetricUrn.BuildAccumulatedValue(baseUrnComponents.ToArray()),
                new MetricValue(value, TimeSpan.FromMinutes(startInMinutes), TimeSpan.FromMinutes(atInMinutes)),
                TimeSpan.FromMinutes(atInMinutes));

        public static Property<MetricValue> CreateAccumulatorValue(Urn propertyUrn, float value, int startInMinutes, int atInMinutes)
            => CreateAccumulatorValue(new[] {propertyUrn.Value}, value, startInMinutes, atInMinutes);

        public static Property<MetricValue> CreateAccumulatorCount(IEnumerable<string> baseUrnComponents, float value, int startInMinutes, int atInMinutes)
            => Property<MetricValue>.Create(
                MetricUrn.BuildSamplesCount(baseUrnComponents.ToArray()),
                new MetricValue(value, TimeSpan.FromMinutes(startInMinutes), TimeSpan.FromMinutes(atInMinutes)),
                TimeSpan.FromMinutes(atInMinutes));

        public static Property<MetricValue> CreateAccumulatorCount(Urn propertyUrn, float value, int startInMinutes, int atInMinutes)
            => CreateAccumulatorCount(new[] {propertyUrn.Value}, value, startInMinutes, atInMinutes);

        #endregion

        public static DataModelValue<FloatValue> CreateDataModelFloatValue(Urn urn, float value, TimeSpan at)
            => new (urn, new FloatValue(value), at);
    }
}