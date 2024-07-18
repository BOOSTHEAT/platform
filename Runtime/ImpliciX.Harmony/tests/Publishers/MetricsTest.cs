using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Harmony.Messages;
using ImpliciX.Harmony.Publishers;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Harmony.Tests.Publishers
{
    [TestFixture]
    public class MetricsTest
    {
        [Test]
        public void check_version_related_to_message_structure()
        {
            var analytics = new AnalyticsJson();
            var propertiesNames = analytics.GetType().GetProperties().Select(p => p.Name).ToArray();
            Array.Sort(propertiesNames);
            Check.That(analytics.Version).IsEqualTo(1);
            Check.That(propertiesNames).ContainsExactly(
                "PublicationDateTime",
                "SampleEndTime",
                "SampleStartTime",
                "SerialNumber",
                "Urn",
                "Value",
                "Version"
            );
        }

        [Test]
        public void should_create_an_analytics_message()
        {
            var sampleStartTime = new DateTime(2021, 07, 12, 16, 46, 01, 123, DateTimeKind.Utc);
            var sampleEndTime = new DateTime(2021, 07, 12, 17, 00, 00, 000, DateTimeKind.Utc);
            var busPublicationDateTime = new DateTime(2021, 07, 12, 17, 00, 01, 322, DateTimeKind.Utc);

            var propertiesChanged = PropertiesChanged.Create(new IDataModelValue[]
                {
                    // State Counter A FOO
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_counters_a, "foo", "occurrence"),
                        new MetricValue(42, TimeSpan.FromTicks(sampleStartTime.Ticks), TimeSpan.FromTicks(sampleEndTime.Ticks)),
                        TimeSpan.FromTicks(busPublicationDateTime.Ticks)),
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_counters_a, "foo", "duration"),
                        new MetricValue(19, TimeSpan.FromTicks(sampleStartTime.Ticks), TimeSpan.FromTicks(sampleEndTime.Ticks)),
                        TimeSpan.FromTicks(busPublicationDateTime.Ticks)),

                    // State Counter A BAR
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_counters_a, "bar", "occurrence"),
                        new MetricValue(13, TimeSpan.FromTicks(sampleStartTime.Ticks), TimeSpan.FromTicks(sampleEndTime.Ticks)),
                        TimeSpan.FromTicks(busPublicationDateTime.Ticks)),
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_counters_a, "bar", "duration"),
                        new MetricValue(37, TimeSpan.FromTicks(sampleStartTime.Ticks), TimeSpan.FromTicks(sampleEndTime.Ticks)),
                        TimeSpan.FromTicks(busPublicationDateTime.Ticks)),

                    // State Counter B BAZ
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_counters_b, "baz", "occurrence"),
                        new MetricValue(16, TimeSpan.FromTicks(sampleStartTime.Ticks), TimeSpan.FromTicks(sampleEndTime.Ticks)),
                        TimeSpan.FromTicks(busPublicationDateTime.Ticks)),
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_counters_b, "baz", "duration"),
                        new MetricValue(64, TimeSpan.FromTicks(sampleStartTime.Ticks), TimeSpan.FromTicks(sampleEndTime.Ticks)),
                        TimeSpan.FromTicks(busPublicationDateTime.Ticks)),

                    // State Counter B FUZZ
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_counters_b, "fuzz", "occurrence"),
                        new MetricValue(31, TimeSpan.FromTicks(sampleStartTime.Ticks), TimeSpan.FromTicks(sampleEndTime.Ticks)),
                        TimeSpan.FromTicks(busPublicationDateTime.Ticks)),
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_counters_b, "fuzz", "duration"),
                        new MetricValue(27, TimeSpan.FromTicks(sampleStartTime.Ticks), TimeSpan.FromTicks(sampleEndTime.Ticks)),
                        TimeSpan.FromTicks(busPublicationDateTime.Ticks)),

                    // Sample Accumulator
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_sample_accumulator, "samples_count"),
                        new MetricValue(27, TimeSpan.FromTicks(sampleStartTime.Ticks), TimeSpan.FromTicks(sampleEndTime.Ticks)),
                        TimeSpan.FromTicks(busPublicationDateTime.Ticks)),
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_sample_accumulator, "accumulated_value"),
                        new MetricValue(31, TimeSpan.FromTicks(sampleStartTime.Ticks), TimeSpan.FromTicks(sampleEndTime.Ticks)),
                        TimeSpan.FromTicks(busPublicationDateTime.Ticks)),

                    // Gauge
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_gauge),
                        new MetricValue(42.19f, TimeSpan.FromTicks(sampleEndTime.Ticks), TimeSpan.FromTicks(sampleEndTime.Ticks)),
                        TimeSpan.FromTicks(busPublicationDateTime.Ticks)),
                }
                , TimeSpan.FromTicks(busPublicationDateTime.Ticks));


            var queue = new Queue<IHarmonyMessage>();
            var metrics = new Metrics(queue);

            metrics.Handles(propertiesChanged);

            var json = queue.Peek().Format(new ContextStub("bhyolo"));
            var expected =
                @"{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-12T17:00:01.322000+00:00"",""SampleStartTime"":""2021-07-12T16:46:01.123000+00:00"",""SampleEndTime"":""2021-07-12T17:00:00.000000+00:00"",""Urn"":""test_model:test_counters_a:foo:occurrence"",""Value"":42}
{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-12T17:00:01.322000+00:00"",""SampleStartTime"":""2021-07-12T16:46:01.123000+00:00"",""SampleEndTime"":""2021-07-12T17:00:00.000000+00:00"",""Urn"":""test_model:test_counters_a:foo:duration"",""Value"":19}
{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-12T17:00:01.322000+00:00"",""SampleStartTime"":""2021-07-12T16:46:01.123000+00:00"",""SampleEndTime"":""2021-07-12T17:00:00.000000+00:00"",""Urn"":""test_model:test_counters_a:bar:occurrence"",""Value"":13}
{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-12T17:00:01.322000+00:00"",""SampleStartTime"":""2021-07-12T16:46:01.123000+00:00"",""SampleEndTime"":""2021-07-12T17:00:00.000000+00:00"",""Urn"":""test_model:test_counters_a:bar:duration"",""Value"":37}
{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-12T17:00:01.322000+00:00"",""SampleStartTime"":""2021-07-12T16:46:01.123000+00:00"",""SampleEndTime"":""2021-07-12T17:00:00.000000+00:00"",""Urn"":""test_model:test_counters_b:baz:occurrence"",""Value"":16}
{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-12T17:00:01.322000+00:00"",""SampleStartTime"":""2021-07-12T16:46:01.123000+00:00"",""SampleEndTime"":""2021-07-12T17:00:00.000000+00:00"",""Urn"":""test_model:test_counters_b:baz:duration"",""Value"":64}
{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-12T17:00:01.322000+00:00"",""SampleStartTime"":""2021-07-12T16:46:01.123000+00:00"",""SampleEndTime"":""2021-07-12T17:00:00.000000+00:00"",""Urn"":""test_model:test_counters_b:fuzz:occurrence"",""Value"":31}
{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-12T17:00:01.322000+00:00"",""SampleStartTime"":""2021-07-12T16:46:01.123000+00:00"",""SampleEndTime"":""2021-07-12T17:00:00.000000+00:00"",""Urn"":""test_model:test_counters_b:fuzz:duration"",""Value"":27}
{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-12T17:00:01.322000+00:00"",""SampleStartTime"":""2021-07-12T16:46:01.123000+00:00"",""SampleEndTime"":""2021-07-12T17:00:00.000000+00:00"",""Urn"":""test_model:test_sample_accumulator:samples_count"",""Value"":27}
{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-12T17:00:01.322000+00:00"",""SampleStartTime"":""2021-07-12T16:46:01.123000+00:00"",""SampleEndTime"":""2021-07-12T17:00:00.000000+00:00"",""Urn"":""test_model:test_sample_accumulator:accumulated_value"",""Value"":31}
{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-12T17:00:01.322000+00:00"",""SampleStartTime"":""2021-07-12T17:00:00.000000+00:00"",""SampleEndTime"":""2021-07-12T17:00:00.000000+00:00"",""Urn"":""test_model:test_gauge"",""Value"":42.19}
";
            Check.That(json).IsEqualTo(expected);
        }
    }
}