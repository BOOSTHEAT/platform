using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.Records;
using ImpliciX.Data.Records.ColdRecords;
using ImpliciX.Language.Model;
using ImpliciX.TestsCommon;
using NUnit.Framework;

namespace ImpliciX.Data.Tests.Records.ColdRecords;

[NonParallelizable]
public class ColdRecordsDbTests
{
    private static readonly string StorageFolderPath = Path.Combine(Path.GetTempPath(), "cold_store");
    
    [SetUp]
    public void Setup()
    {
        if (Directory.Exists(StorageFolderPath))
            Directory.Delete(StorageFolderPath, true);
        
    }
    
    [Test]
    public void store_and_read_records_nominal_case()
    {
        var sut = ColdRecordsDb.LoadOrCreate(new Urn[]{"foo:bar"}, StorageFolderPath);
        var data = new IIMutableDataModelValue[]
        {
            new DataModelValue<FloatValue>("fizz",FloatValue.FromFloat(1f).Value, TimeSpan.FromSeconds(1)),
            new DataModelValue<Literal>("qix",Literal.Create("boo"), TimeSpan.FromSeconds(1)),
            new DataModelValue<MeasureStatus>("baz",MeasureStatus.Success, TimeSpan.FromSeconds(1))
        };

        var snapshot = new Snapshot(1, "foo:bar", data, TimeSpan.FromSeconds(1), "foo:form");
    
        sut.Write(snapshot);
        sut.Dispose();
        var dataPoints = ExtractDataPoints(sut.CurrentFiles[0]);
        Assert.Multiple(() =>
        {
            Assert.That(dataPoints, Has.Count.EqualTo(1));
            Assert.That(dataPoints[0].Id, Is.EqualTo(1));
            Assert.That(dataPoints[0].At, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(dataPoints[0].Values, Is.EquivalentTo(new DataPointValue[]
            {
                new ("fizz", FieldType.Float, 1f),
                new ("qix", FieldType.String, "boo"),
                new ("baz", FieldType.Enum, "Success")
            }));
        });
    }
    
    
    
    private static List<RecordsDataPoint> ExtractDataPoints(string filePath) => 
        ColdRecordsDb.LoadCollection(filePath).DataPoints.ToList();
}