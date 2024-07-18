using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.SharedKernel.Logger;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using Log = ImpliciX.Language.Core.Log;

namespace ImpliciX.Data.Tests.HotTimeSeries;

[Platform(Include = "Linux")]
public class TimeSeriesDbTests
{
    private static readonly string TmpComputersDbFolder = Path.Combine(Path.GetTempPath(), "test_computers");
    private const string DbName = "computers";
    private TimeSeriesDb _sut;
    
    private readonly Dictionary<Urn, TimeSpan> _seriesDef = new ()
    {
        {Urn.BuildUrn("ts", "foo"), TimeSpan.FromDays(30)},
        {Urn.BuildUrn("ts", "foo_phy"), TimeSpan.FromDays(30)},
        {Urn.BuildUrn("ts", "foo", "bar"), TimeSpan.FromDays(30)},
        {Urn.BuildUrn("ts", "foo", "fizz"), TimeSpan.FromDays(30)}
    };

    [SetUp]
    public void Init()
    {
        if(Directory.Exists(TmpComputersDbFolder))
            Directory.Delete(TmpComputersDbFolder, true);
        _sut = new TimeSeriesDb(TmpComputersDbFolder, DbName);
    }
    
    [Test]
    public void GivenSteamIsEmpty_WhenIReadAt_ThenIGetNone()
    {
        var result = _sut.ReadAt("ts:foo", 10);
        Check.That(result.IsNone).IsTrue();
    }

    [Test]
    public void GivenValuesExists_WhenIReadAt_ThenIGetItemExpected()
    {
        const string key = "ts:foo";
        GivenTimeSeriesWithDummyData(key, TimeSpan.Zero, 10, TimeSpan.FromSeconds(1));

        var at = TimeSpan.FromSeconds(3);
        var result = _sut.ReadAt(key, at.Ticks);
        result.CheckIsSomeAnd(value => Check.That<TimeSpan>(value.At).IsEqualTo(at));
    }
    
    
    [Test]
    public void ReadAllNominalCase()
    {
        GivenTimeSeriesWithDummyData("ts:foo", TimeSpan.Zero, 1000);
        var results = _sut.ReadAll("ts:foo");
        results.CheckIsSomeAnd(values => Check.That(values).CountIs(1000));
    }
    
    [Test]
    public void ReadAllWithCountLimit()
    {
        GivenTimeSeriesWithDummyData("ts:foo", TimeSpan.Zero, 1000);
        var results = _sut.ReadAll("ts:foo", 10);
        results.CheckIsSomeAnd(values => Check.That(values).CountIs(10));
    }
    
    [Test]
    public void ReadAllWithUpToLimit()
    {
        GivenTimeSeriesWithDummyData("ts:foo", TimeSpan.Zero, 10, TimeSpan.FromSeconds(1));

        var timeSpanLastElement = TimeSpan.FromSeconds(9);
        var timeSpanBeforeLastElement = TimeSpan.FromSeconds(8);
        var results = _sut.ReadAll("ts:foo", upTo: timeSpanLastElement.Ticks);
        results.CheckIsSomeAnd(values =>
        {
            Check.That(values).CountIs(10);
            Check.That(values.Last().At).IsEqualTo(timeSpanLastElement);
        });

        results = _sut.ReadAll("ts:foo", upTo: timeSpanLastElement.Ticks - 1);
        results.CheckIsSomeAnd(values =>
        {
            Check.That(values).CountIs(9);
            Check.That(values.Last().At).IsEqualTo(timeSpanBeforeLastElement);
        });
    }
    
    [Test]
    public void ReadAllWithCountAnUpToLimits()
    {
        GivenTimeSeriesWithDummyData("ts:foo", TimeSpan.Zero, 100, TimeSpan.FromSeconds(1));

        var timeSpanAfterCountLimit = TimeSpan.FromSeconds(10);
        var results = _sut.ReadAll("ts:foo", 10, upTo: timeSpanAfterCountLimit.Ticks);
        results.CheckIsSomeAnd(values => Check.That(values).CountIs(10));

        var timeSpanBeforeCountLimit = TimeSpan.FromSeconds(7);
        results = _sut.ReadAll("ts:foo", 10, upTo: timeSpanBeforeCountLimit.Ticks);
        results.CheckIsSomeAnd(values =>
        {
            Check.That(values).CountIs(8);
            Check.That(values.Last().At).IsEqualTo(timeSpanBeforeCountLimit);
        });
    }
    
    [Test]
    public void GivenTimeSeriesWithDummyData_When_CheckIfDefines_Series()
    {
        var startTime = TimeSpan.FromSeconds(1);
        var incrementTimeAtEachCount = TimeSpan.FromSeconds(1);
        GivenTimeSeriesWithDummyData("ts:foo", startTime, 1000, incrementTimeAtEachCount);

        var results = _sut.IsDefined("ts:foo");
        Check.That(results).IsTrue();
    }
    
    [Test]
    public void GivenTimeSeriesWithDummyData_When_ReadMany()
    {
        var startTime = TimeSpan.Zero;
        GivenTimeSeriesWithDummyData("ts:foo", startTime, 1000);
        GivenTimeSeriesWithDummyData("ts:foo:bar", startTime, 1000);

        var endTime = startTime.Add(TimeSpan.FromSeconds(1));
        var results = _sut.ReadMany(new Urn[] {"ts:foo", "ts:foo:bar"}, startTime.Ticks, endTime.Ticks);
        Check.That(results.IsSome).IsTrue();

        var values = results.GetValue();
        Check.That(values).HasSize(4);
        Check.That(values[0].Urn.Value).IsEqualTo("ts:foo");
        Check.That(values[0].At).IsEqualTo(startTime);

        Check.That(values[1].Urn.Value).IsEqualTo("ts:foo");
        Check.That(values[1].At).IsEqualTo(endTime);

        Check.That(values[2].Urn.Value).IsEqualTo("ts:foo:bar");
        Check.That(values[2].At).IsEqualTo(startTime);

        Check.That(values[3].Urn.Value).IsEqualTo("ts:foo:bar");
        Check.That(values[3].At).IsEqualTo(endTime);
    }
    
    
    [Test]
    public void GivenNoElementForTsForKey_WhenIReadFirst_ThenIGetNone()
    {
        var readFromDb = _sut.ReadFirst("ts:foo");
        Check.That(readFromDb.IsNone).IsTrue();
    }

    [Test]
    public void GivenNoElementForTsForKey_WhenIReadLast_ThenIGetNone()
    {
        var readFromDb = _sut.ReadLast("ts:foo");
        Check.That(readFromDb.IsNone).IsTrue();
    }
    
    [Test]
    public void GivenTsForKeyExists_WhenIReadFirst_ThenIGetTheFirstKeyValue()
    {
        var startTime = TimeSpan.FromMinutes(1);
        const string targetUrnKey = "ts:foo";
        GivenTimeSeriesWithDummyData(targetUrnKey, startTime, 10, TimeSpan.FromMinutes(1));

        var readFromDb = _sut.ReadFirst(targetUrnKey);
        readFromDb.CheckIsSomeAnd(read => Check.That(read.At).IsEqualTo(startTime));
    }
    
    
    [Test]
    public void GivenTsForKeyExists_WhenIReadLast_ThenIGetTheLastKeyValue()
    {
        var startTime = TimeSpan.FromMinutes(1);
        const string targetUrnKey = "ts:foo";
        const int targetUrnKeyValuesCount = 10;
        var incrementTimeAtEachCount = TimeSpan.FromMinutes(1);
    
        GivenTimeSeriesWithDummyData(targetUrnKey, startTime, targetUrnKeyValuesCount, incrementTimeAtEachCount);
    
        var readFromDb = _sut.ReadLast(targetUrnKey);
    
        readFromDb.CheckIsSomeAnd(read =>
        {
            var atExpected = incrementTimeAtEachCount * (targetUrnKeyValuesCount - 1) + startTime;
            Check.That(read.At).IsEqualTo(atExpected);
        });
    }
    
    
    [Test]
    public void GivenTsForKeyExists_WhenIReadLastWithUpToLimit_ThenIGetTheLastValueAtEqualOrOlderThatUptoLimit()
    {
        const string targetUrnKey = "ts:foo";
        GivenTimeSeriesWithDummyData(targetUrnKey, TimeSpan.FromMinutes(1), 10, TimeSpan.FromMinutes(1));

        var lastElementAt = TimeSpan.FromMinutes(10);
        var upToThatExcludeTheLastElement = lastElementAt.Ticks - 1;

        var readFromDb = _sut.ReadLast(targetUrnKey, upToThatExcludeTheLastElement);
        readFromDb.CheckIsSomeAnd(read => Check.That(read.At).IsEqualTo(TimeSpan.FromMinutes(9)));
    }

    [Test]
    public void GivenTimeSeriesWithDummyData_When_ReadMany_With_Count_Limit()
    {
        var startTime = TimeSpan.Zero;
        GivenTimeSeriesWithDummyData("ts:foo", startTime, 1000);

        var endTime = startTime.Add(TimeSpan.FromSeconds(1000));
        var results = _sut.ReadMany(new Urn[] {"ts:foo"}, startTime.Ticks, endTime.Ticks, 10);
        Check.That(results.IsSome).IsTrue();
        Check.That(results.GetValue()).HasSize(10);
    }
   
    [Test]
    public void GivenNoTimeSeries_When_ReadMany()
    {
        var startTime = TimeSpan.Zero;
        var results = _sut.ReadMany(new Urn[] {"ts:foo"}, startTime.Ticks, DateTime.Now.Ticks, 10);
        Check.That(results.IsSome).IsTrue();
        Check.That(results.GetValue()).HasSize(0);
    }
    
    [Test]
    public void GivenSeriesAndSubseries_When_ReadAllServerKeys()
    {
        var startTime = TimeSpan.Zero;
        GivenTimeSeriesWithDummyData("ts:foo", startTime, 10);
        GivenTimeSeriesWithDummyData("ts:foo:bar", startTime, 2);
        GivenTimeSeriesWithDummyData("ts:foo:fizz", startTime, 2);
        GivenTimeSeriesWithDummyData("ts:foo_phy", startTime, 2);
        var results = _sut.AllKeys();
        Check.That(results.IsSome).IsTrue();
        Check.That(results.GetValue()).CountIs(4);
        Check.That(results.GetValue()).Contains(
            Urn.BuildUrn("ts:foo"),
            Urn.BuildUrn("ts:foo:bar"),
            Urn.BuildUrn("ts:foo:fizz"),
            Urn.BuildUrn("ts:foo_phy")
        );
    }

    [Test]
    public void GivenTimeSeries_With_RetentionPeriod_Expired_DataPoints_ShouldBeDeletedAutomatically()
    {
        var startTime = TimeSpan.Zero;
        GivenAnExistingTimeSeries("foo:bar", TimeSpan.FromSeconds(10));
        ThenWriteMany("foo:bar", 100, startTime, TimeSpan.FromSeconds(1));

        _sut.ApplyRetentionPolicy();
        var ts = _sut.ReadAll("foo:bar").GetValueOrDefault(Array.Empty<DataModelValue<float>>());
        
        Assert.That(ts.Length, Is.EqualTo(11));
        Assert.That(ts[0].At, Is.EqualTo(TimeSpan.FromSeconds(89)));
        Assert.That(ts[^1].At, Is.EqualTo(TimeSpan.FromSeconds(99)));
    }
    
    [Test]
    public void GivenTimeSeries_With_RetentionPeriod_Zero_Means_Infinite()
    {
        var startTime = TimeSpan.Zero;
        GivenAnExistingTimeSeries("foo:bar", TimeSpan.Zero);
        ThenWriteMany("foo:bar", 100, startTime, TimeSpan.FromSeconds(1));
        
        _sut.ApplyRetentionPolicy();
        var ts = _sut.ReadAll("foo:bar").GetValueOrDefault(Array.Empty<DataModelValue<float>>());
        
        Assert.That(ts.Length, Is.EqualTo(100));
    }


    [Test]
    public void On_Reload_Dont_Define_Series_That_AlreadyExists()
    {
        var startTime = TimeSpan.Zero;
        GivenAnExistingTimeSeries("foo:bar", TimeSpan.FromMinutes(1));
       
        _sut.Dispose();
        
        _sut = new TimeSeriesDb(TmpComputersDbFolder, DbName);
        _sut.SetupTimeSeries("foo:bar", TimeSpan.FromMinutes(2));
        
        Assert.That(_sut.DefinedStructs["foo:bar"].Count, Is.EqualTo(1));
        Assert.That(_sut.RetentionPeriods["foo:bar"], Is.EqualTo(TimeSpan.FromMinutes(2)));

    }
    
    private const string SeriesName = "ts:foo";
    
    [Test]
    public void WhenIWrite()
    {
        GivenAnExistingTimeSeries(SeriesName, TimeSpan.FromMinutes(29));
        var result = _sut.Write(SeriesName, TimeSpan.FromMinutes(30), 30);
        Check.That(result.IsSome).IsTrue();
    }
    
    [Test]
    public void GivenAnExistingTimeSeries_When_WriteMany()
    {
        GivenAnExistingTimeSeries(SeriesName, TimeSpan.FromDays(30));
        ThenWriteMany(SeriesName, 1000);
    }
    
    [Test]
    public void GivenAnExistingTimeSeries_WhenIDelete()
    {
        GivenAnExistingTimeSeries(SeriesName, TimeSpan.Zero);

        const int samplesCreatedAtBeginning = 50;
        var incrementTimeAtEachCount = TimeSpan.FromSeconds(1);
        ThenWriteMany(SeriesName, samplesCreatedAtBeginning, TimeSpan.Zero, incrementTimeAtEachCount);

        
        const int deletedCountExpected = 20 - 2 + 1;
        var deletedCount = WhenIDelete(SeriesName, from: incrementTimeAtEachCount * 2, to: incrementTimeAtEachCount * 20);
        Check.That(deletedCount).IsEqualTo(deletedCountExpected);
    }

   
    private static IEnumerable<IDataModelValue> GenerateMetricValues(string seriesName, int count, TimeSpan? startTime = null,
        TimeSpan? incrementTimeAtEachCount = null)
    {
        var r = new Random(42);
        var t = startTime ?? DateTime.Now.TimeOfDay;
        var incrementTime = incrementTimeAtEachCount ?? TimeSpan.FromSeconds(1);

        for (var i = 0; i < count; i++)
        {
            yield return new DataModelValue<MetricValue>(seriesName, new MetricValue(r.NextSingle() * 1000, TimeSpan.Zero, TimeSpan.Zero), t);
            t = t.Add(incrementTime);
        }
    }
    
    private void GivenTimeSeriesWithDummyData(string seriesName, TimeSpan startTime, int count, TimeSpan? incrementTimeAtEachCount = null)
    {
        var ts = GenerateMetricValues(seriesName, count, startTime, incrementTimeAtEachCount: incrementTimeAtEachCount);

        ts.Chunk(100).ForEach(c =>
        {
            var result = _sut.WriteMany(Enumerable.ToList<IDataModelValue>(c), _seriesDef);
            Check.That(result.IsSome).IsTrue();
        });
    }
    
    private void GivenAnExistingTimeSeries(string key, TimeSpan retentionTime)
    {
        _sut.SetupTimeSeries(key, retentionTime);
    }
    
    private void ThenWriteMany(string seriesName, int count, TimeSpan? startTime = null, TimeSpan? incrementTimeAtEachCount = null)
    {
        var ts = GenerateMetricValues(seriesName, count, startTime, incrementTimeAtEachCount);

        ts.Chunk(100).ForEach(c =>
        {
            var result = _sut.WriteMany(c.ToList(), _seriesDef);
            Check.That(result.IsSome).IsTrue();
        });
    }
    
    private long WhenIDelete(string key, TimeSpan from, TimeSpan to)
    {
        return _sut.Delete(key, from, to);
    }
    
    
}