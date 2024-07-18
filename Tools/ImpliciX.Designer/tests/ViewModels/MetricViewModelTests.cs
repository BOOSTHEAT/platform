using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Designer.ViewModels;
using ImpliciX.Language.Core;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using Moq;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels;

public class MetricViewModelTests
{
    [TestCase(MetricKind.Gauge, "Gauge")]
    [TestCase(MetricKind.Variation, "Variation")]
    [TestCase(MetricKind.SampleAccumulator, "Accumulator")]
    [TestCase(MetricKind.State, "State Monitoring")]
    [TestCase(MetricKind.Communication, "Device Monitoring")]
    public void DescriptionTest(MetricKind kind, string expectedText)
    {
        var sut = CreateMetricViewModel(
            m => m.Setup(x => x.Kind).Returns(kind)
        );

        Assert.That(sut.Main.Details, Is.EqualTo($"{expectedText} of foo:bar"));
    }

    [TestCase("01:00:00", "1 hour")]
    [TestCase("05:00:00", "5 hours")]
    [TestCase("06:00:00", "6 hours")]
    [TestCase("00:01:00", "1 minute")]
    [TestCase("00:05:00", "5 minutes")]
    [TestCase("00:06:00", "6 minutes")]
    [TestCase("00:00:01", "1 second")]
    [TestCase("00:00:05", "5 seconds")]
    [TestCase("00:00:06", "6 seconds")]
    [TestCase("1.00:00:00", "1 day")]
    [TestCase("5.00:00:00", "5 days")]
    [TestCase("6.00:00:00", "6 days")]
    public void SamplePeriodTest(string period, string expectedText)
    {
        var sut = CreateMetricViewModel(
            m => m.Setup(x => x.PublicationPeriod).Returns(TimeSpan.Parse(period))
        );

        Assert.That(sut.SamplePeriod, Is.EqualTo(expectedText));
    }

    [TestCase(1, TimeUnit.Hours, "1 hour")]
    [TestCase(5, TimeUnit.Hours, "5 hours")]
    [TestCase(6, TimeUnit.Hours, "6 hours")]
    [TestCase(1, TimeUnit.Minutes, "1 minute")]
    [TestCase(5, TimeUnit.Minutes, "5 minutes")]
    [TestCase(6, TimeUnit.Minutes, "6 minutes")]
    [TestCase(1, TimeUnit.Seconds, "1 second")]
    [TestCase(5, TimeUnit.Seconds, "5 seconds")]
    [TestCase(6, TimeUnit.Seconds, "6 seconds")]
    [TestCase(1, TimeUnit.Days, "1 day")]
    [TestCase(5, TimeUnit.Days, "5 days")]
    [TestCase(6, TimeUnit.Days, "6 days")]
    [TestCase(1, TimeUnit.Weeks, "1 week")]
    [TestCase(5, TimeUnit.Weeks, "5 weeks")]
    [TestCase(1, TimeUnit.Months, "1 month")]
    [TestCase(5, TimeUnit.Months, "5 months")]
    [TestCase(1, TimeUnit.Quarters, "1 quarter")]
    [TestCase(5, TimeUnit.Quarters, "5 quarters")]
    [TestCase(1, TimeUnit.Years, "1 year")]
    [TestCase(5, TimeUnit.Years, "5 years")]
    public void StoragePeriodTest(int duration, TimeUnit timeUnit, string expectedText)
    {
        var sut = CreateMetricViewModel(
            m => m.Setup(x => x.StoragePolicy)
                .Returns(Option<StoragePolicy>.Some(new StoragePolicy(duration, timeUnit)))
        );

        Assert.That(sut.StoragePeriod, Is.EqualTo(expectedText));
    }

    [Test]
    public void InclusionsTest()
    {
        var sut = CreateMetricViewModel(
            m => m.Setup(x => x.SubMetricDefs).Returns(new List<SubMetricDef>
            {
                new SubMetricDef("fizz", MetricKind.Gauge, Urn.BuildUrn("qix:fizz")),
                new SubMetricDef("buzz", MetricKind.Variation, Urn.BuildUrn("qix:buzz"))
            })
        );

        Assert.That(sut.Inclusions.Count(), Is.EqualTo(2));
        Assert.That(sut.Inclusions.ElementAt(0).Name, Is.EqualTo("fizz"));
        Assert.That(sut.Inclusions.ElementAt(0).Details, Is.EqualTo("Gauge of qix:fizz"));
        Assert.That(sut.Inclusions.ElementAt(1).Name, Is.EqualTo("buzz"));
        Assert.That(sut.Inclusions.ElementAt(1).Details, Is.EqualTo("Variation of qix:buzz"));
    }


    private MetricViewModel CreateMetricViewModel(Action<Mock<IMetric>> setup)
    {
        var metric = new Mock<IMetric>();
        metric.Setup(x => x.TargetUrn).Returns(Urn.BuildUrn("plop"));
        metric.Setup(x => x.InputUrn).Returns(Urn.BuildUrn("foo", "bar"));
        setup(metric);
        return new MetricViewModel(metric.Object);
    }
}