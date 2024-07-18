using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.ColdDb;
using ImpliciX.Data.ColdMetrics;
using ImpliciX.Language.Model;
using NUnit.Framework;

namespace ImpliciX.Data.Tests.ColdMetrics;

public class MetricsDataPointProtocolTests
{
    [TestCaseSource(nameof(DataPointsTestCases))]
    public void encode_decode_datapoint_test(TimeSpan prev, TimeSpan at, TimeSpan ss, TimeSpan se)
    {
        var sut = new MetricsProtocolVersion1();

        var dp = new MetricsDataPoint(at, new DataPointValue[]
        {
            new ("foo:bar", 3f),
            new ("fizz:buzz", 15f)
        }, ss, se);

        var bytes = sut.EncodeDataPoint(dp, PropertiesDescriptors, at);

        var dp2 = sut.DecodeDataPoint(bytes, PropertiesDescriptors.Keys.ToArray(), at);
        Assert.That(dp2.At, Is.EqualTo(dp.At));
        Assert.That(dp2.Values, Is.EquivalentTo(dp.Values));
        Assert.That(dp2.SampleStartTime, Is.EqualTo(dp.SampleStartTime));
        Assert.That(dp2.SampleEndTime, Is.EqualTo(dp.SampleEndTime));
    }


    [Test]
    public void encode_decode_datapoint_test_the_values_should_be_stored_in_order_defined_by_the_urns_index()
    {
        var sut = new MetricsProtocolVersion1();

        var at = TimeSpan.FromSeconds(43);
        var dp = new MetricsDataPoint(at, new DataPointValue[]
        {
            new ("fizz:buzz", 15f),
            new ("foo:bar", 3f)
        }, TimeSpan.FromSeconds(41), at);

        var bytes = sut.EncodeDataPoint(dp, PropertiesDescriptors, at);

        var derivedPropertiesUrns = PropertiesDescriptors.Keys.ToArray();
        var dp2 = sut.DecodeDataPoint(bytes, derivedPropertiesUrns, at);

        Assert.That(dp2.Values[0].Urn, Is.EqualTo(derivedPropertiesUrns[0].Urn));
        Assert.That(dp2.Values[1].Urn, Is.EqualTo(derivedPropertiesUrns[1].Urn));
    }

    [TestCaseSource(nameof(MetaDataTestsCases))]
    public void encode_decode_metadata(MetaDataItem md)
    {
        var sut = new MetricsProtocolVersion1();
        var bytes = sut.EncodeMetadata(md);
        var md2 = sut.DecodeMetadata(bytes);
        Assert.That(md2, Is.EqualTo(md));
    }

    public static object[] MetaDataTestsCases =
    {
        new[] {MetaDataItem.Urn("foo:bar")},
        new[] {MetaDataItem.PropertyDescription("foo:bar")},
        new[] {MetaDataItem.FirstDataItemPointTime(TimeSpan.FromSeconds(1))},
        new[] {MetaDataItem.LastDataItemPointTime(TimeSpan.FromSeconds(1))},
        new[] {MetaDataItem.DataPointsCount(42)},
    };

    public static object[] DataPointsTestCases =
    {
        new[] {TimeSpan.FromSeconds(43), TimeSpan.FromSeconds(43), TimeSpan.FromSeconds(41), TimeSpan.FromSeconds(43)},
        new[] {TimeSpan.FromSeconds(41), TimeSpan.FromSeconds(43), TimeSpan.FromSeconds(41), TimeSpan.FromSeconds(43)},
        new[] {TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(43), TimeSpan.FromSeconds(43), TimeSpan.FromSeconds(45)},
        new[] {TimeSpan.FromSeconds(41), TimeSpan.FromSeconds(43), TimeSpan.FromSeconds(41), TimeSpan.FromSeconds(42)},
    };

    private static Dictionary<PropertyDescriptor, byte> PropertiesDescriptors => new ()
    {
        {new PropertyDescriptor("foo:bar",0), 0},
        {new PropertyDescriptor("fizz:buzz",0), 1}
    };
}