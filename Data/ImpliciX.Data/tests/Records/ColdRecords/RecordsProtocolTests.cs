using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.ColdDb;
using ImpliciX.Data.Records;
using ImpliciX.Data.Records.ColdRecords;
using ImpliciX.Language.Model;
using ImpliciX.TestsCommon;
using NUnit.Framework;

namespace ImpliciX.Data.Tests.Records.ColdRecords;

public class RecordsProtocolTests
{
    [Test]
    public void encode_decode_datapoint_nominal()
    {
        var sut = new RecordsProtocolVersion1();

        var snapshot = new Snapshot(1,"foo:bar", new IIMutableDataModelValue[]
        {
            new DataModelValue<FloatValue>("fizz", FloatValue.FromFloat(1f).Value, TimeSpan.FromSeconds(1)),
            new DataModelValue<Literal>("qix", Literal.Create("boo"), TimeSpan.FromSeconds(1)),
            new DataModelValue<MeasureStatus>("baz", MeasureStatus.Success, TimeSpan.FromSeconds(1))
        }, TimeSpan.FromSeconds(1), "foo:form");

        var dp = RecordsDataPoint.FromSnapshot(snapshot);

        var bytes = sut.EncodeDataPoint(dp, PropertiesDescriptors, TimeSpan.Zero);

        var dp2 = sut.DecodeDataPoint(bytes, PropertiesDescriptors.Keys.ToArray(), TimeSpan.Zero);
        Assert.That(dp2.At, Is.EqualTo(dp.At));
        Assert.That(dp2.Id, Is.EqualTo(dp.Id));
        Assert.That(dp2.Values, Is.EquivalentTo(dp.Values));
    }

    [Test]
    public void encode_decode_datapoint_properties_order_different_than_descriptors_order()
    {
        var sut = new RecordsProtocolVersion1();

        var data = new IIMutableDataModelValue[]
        {
            new DataModelValue<MeasureStatus>("baz", MeasureStatus.Success, TimeSpan.FromSeconds(1)),
            new DataModelValue<Literal>("qix", Literal.Create("boo"), TimeSpan.FromSeconds(1)),
            new DataModelValue<FloatValue>("fizz", FloatValue.FromFloat(1f).Value, TimeSpan.FromSeconds(1))
        };

        var snapshot = new Snapshot(1, "foo:bar", data, TimeSpan.FromSeconds(1), "foo:form");

        var dp = RecordsDataPoint.FromSnapshot(snapshot);

        var bytes = sut.EncodeDataPoint(dp, PropertiesDescriptors, TimeSpan.Zero);

        var dp2 = sut.DecodeDataPoint(bytes, PropertiesDescriptors.Keys.ToArray(), TimeSpan.Zero);
        Assert.That(dp2.At, Is.EqualTo(dp.At));
        Assert.That(dp2.Id, Is.EqualTo(dp.Id));
        Assert.That(dp2.Values, Is.EquivalentTo(dp.Values));
    }

    [Test]
    public void encode_decode_datapoint_with_missing_properties()
    {
        var sut = new RecordsProtocolVersion1();

        var data = new IIMutableDataModelValue[]
        {
            new DataModelValue<Literal>("qix", Literal.Create("boo"), TimeSpan.FromSeconds(1)),
            new DataModelValue<FloatValue>("fizz", FloatValue.FromFloat(1f).Value, TimeSpan.FromSeconds(1))
        };

        var snapshot = new Snapshot(1,"foo:bar", data, TimeSpan.FromSeconds(1), "foo:form");

        var dp = RecordsDataPoint.FromSnapshot(snapshot);

        var bytes = sut.EncodeDataPoint(dp, PropertiesDescriptorsInitial, TimeSpan.Zero);

        var dp2 = sut.DecodeDataPoint(bytes, PropertiesDescriptors.Keys.ToArray(), TimeSpan.Zero);
        Assert.That(dp2.At, Is.EqualTo(dp.At));
        Assert.That(dp2.Id, Is.EqualTo(dp.Id));
        Assert.That(dp2.Values, Is.EquivalentTo(dp.Values.Append(new DataPointValue("baz", FieldType.Enum, ""))));
    }

    [TestCaseSource(nameof(MetaDataTestsCases))]
    public void encode_decode_metadata(MetaDataItem md)
    {
        var sut = new RecordsProtocolVersion1();
        var bytes = sut.EncodeMetadata(md);
        var md2 = sut.DecodeMetadata(bytes);
        Assert.That(md2, Is.EqualTo(md));
    }

    public static object[] MetaDataTestsCases =
    {
        new[] {MetaDataItem.Urn("foo:bar")},
        new[] {MetaDataItem.PropertyDescription("foo:bar", (byte) FieldType.String)},
        new[] {MetaDataItem.FirstDataItemPointTime(TimeSpan.FromSeconds(1))},
        new[] {MetaDataItem.LastDataItemPointTime(TimeSpan.FromSeconds(1))},
        new[] {MetaDataItem.DataPointsCount(42)},
    };

    private static Dictionary<PropertyDescriptor, byte> PropertiesDescriptors => new()
    {
        {new PropertyDescriptor("fizz", (byte) FieldType.Float), 0},
        {new PropertyDescriptor("qix", (byte) FieldType.String), 1},
        {new PropertyDescriptor("baz", (byte) FieldType.Enum), 2}
    };
    
    private static Dictionary<PropertyDescriptor, byte> PropertiesDescriptorsInitial => new()
    {
        {new PropertyDescriptor("fizz", (byte) FieldType.Float), 0},
        {new PropertyDescriptor("qix", (byte) FieldType.String), 1},
    };
}