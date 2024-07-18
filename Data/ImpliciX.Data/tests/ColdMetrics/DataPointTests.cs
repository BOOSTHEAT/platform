using System;
using ImpliciX.Data.ColdMetrics;
using NUnit.Framework;
using PHD = ImpliciX.TestsCommon.PropertyDataHelper;

namespace ImpliciX.Data.Tests.ColdMetrics;

public class DataPointTests
{
       [Test]
    public void create_from_model_values()
    {
        var dps = MetricsDataPoint.FromModel(new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 3f, 0, 42),
            PHD.CreateMetricValueProperty("foo:bar:buzz", 15f, 0, 42),
            PHD.CreateMetricValueProperty("foo:bar:fizz", 2f, 0, 41),
            PHD.CreateMetricValueProperty("foo:bar:buzz", 14f, 0, 41)
        });

        Assert.Multiple(() =>
        {
            Assert.That(dps.Length, Is.EqualTo(2));
            Assert.That(dps[0].At, Is.EqualTo(TimeSpan.FromMinutes(41)));
            Assert.That(dps[0].SampleStartTime, Is.EqualTo(TimeSpan.FromMinutes(0)));
            Assert.That(dps[0].SampleEndTime, Is.EqualTo(TimeSpan.FromMinutes(41)));
            Assert.That(dps[0].Values, Is.EquivalentTo(new[]
            {
                new DataPointValue("foo:bar:fizz", 2f),
                new DataPointValue("foo:bar:buzz", 14f)
            }));

            Assert.That(dps[1].At, Is.EqualTo(TimeSpan.FromMinutes(42)));
            Assert.That(dps[1].SampleStartTime, Is.EqualTo(TimeSpan.FromMinutes(0)));
            Assert.That(dps[1].SampleEndTime, Is.EqualTo(TimeSpan.FromMinutes(42)));
            Assert.That(dps[1].Values, Is.EquivalentTo(new[]
            {
                new DataPointValue("foo:bar:fizz", 3f),
                new DataPointValue("foo:bar:buzz", 15f)
            }));
        });
    }

    [Test]
    public void create_from_model_values_should_take_the_min_sampling_start_and_the_max_sampling_end_from_model_values()
    {
        var dps = MetricsDataPoint.FromModel(new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 3f, 2, 42, 43),
            PHD.CreateMetricValueProperty("foo:bar:buzz", 15f, 1, 43, 43),
        });
        Assert.Multiple(() =>
        {
            Assert.That(dps[0].At, Is.EqualTo(TimeSpan.FromMinutes(43)));
            Assert.That(dps[0].SampleStartTime, Is.EqualTo(TimeSpan.FromMinutes(1)));
            Assert.That(dps[0].SampleEndTime, Is.EqualTo(TimeSpan.FromMinutes(43)));
        });
    }

}