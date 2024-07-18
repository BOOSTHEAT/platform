using System;
using System.IO;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Storage;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Metrics.Tests;

[NonParallelizable]
public class ValueStreamTests
{
    private const string StreamKey = "test:vs";
    private IReadTimeSeries _tsReader;
    private IWriteTimeSeries _tsWriter;


    [SetUp]
    public void Init()
    {
        if (Directory.Exists("/tmp/test_stream"))
            Directory.Delete("/tmp/test_stream", true);
        var db = new TimeSeriesDb("/tmp/test_stream","tsstream");
        _tsReader = db;
        _tsWriter = db;
    }

    [Test]
    public void all_test()
    {
        var sut = ValuesStream.Create(StreamKey, _tsReader, _tsWriter, TimeSpan.FromSeconds(10));
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(0), 1);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(1), 2);
        var result = sut.ReadAllPeriodValues();
        result.CheckIsSomeAnd(values =>
        {
            Check.That(values).ContainsExactly(
                new DataModelValue<float>(sut.Key, 1, TimeSpan.FromSeconds(0)),
                new DataModelValue<float>(sut.Key, 2, TimeSpan.FromSeconds(1))
            );
        });
    }

    [Test]
    public void all_applies_retention_policy()
    {
        var sut = ValuesStream.Create(StreamKey, _tsReader, _tsWriter, TimeSpan.FromSeconds(10));
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(0), 1);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(1), 2);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(10), 10);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(11), 11);

        var result = sut.ReadAllPeriodValues();

        result.CheckIsSomeAnd(values =>
        {
            Check.That(values).ContainsExactly(
                new DataModelValue<float>(sut.Key, 2, TimeSpan.FromSeconds(1)),
                new DataModelValue<float>(sut.Key, 10, TimeSpan.FromSeconds(10)),
                new DataModelValue<float>(sut.Key, 11, TimeSpan.FromSeconds(11))
            );
        });
    }

    [Test]
    public void GivenPeriodIsEqualToKeepLatestOnly_WhenReadAll_ThenIGetOnlyTheLatest()
    {
        var sut = ValuesStream.Create(StreamKey, _tsReader, _tsWriter, ValuesStreamPeriod.KeepLatestOnly);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(0), 1);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(1), 2);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(2), 3);
        var result = sut.ReadAllPeriodValues();
        result.CheckIsSomeAnd(values =>
        {
            Check.That(values).ContainsExactly(new DataModelValue<float>(sut.Key, 3, TimeSpan.FromSeconds(3)));
        });
    }

    [Test]
    public void write_test()
    {
        var sut = ValuesStream.Create(StreamKey, _tsReader, _tsWriter, TimeSpan.FromSeconds(10));
        sut.Write(1, TimeSpan.FromSeconds(0));
        sut.Write(2, TimeSpan.FromSeconds(1));
        _tsReader.ReadAll(sut.Key).CheckIsSomeAnd(values =>
            Check.That(values).ContainsExactly(
                new DataModelValue<float>(sut.Key, 1, TimeSpan.FromSeconds(0)),
                new DataModelValue<float>(sut.Key, 2, TimeSpan.FromSeconds(1))
            )
        );
    }

    [Test]
    public void write_applies_retention_policy()
    {
        var sut = ValuesStream.Create(StreamKey, _tsReader, _tsWriter, TimeSpan.FromSeconds(10));
        sut.Write(1, TimeSpan.FromSeconds(0));
        sut.Write(2, TimeSpan.FromSeconds(1));
        sut.Write(10, TimeSpan.FromSeconds(10));
        sut.Write(11, TimeSpan.FromSeconds(11));

        var result = _tsReader.ReadAll(sut.Key);

        result.CheckIsSomeAnd(values =>
        {
            Check.That(values).ContainsExactly(
                new DataModelValue<float>(sut.Key, 2, TimeSpan.FromSeconds(1)),
                new DataModelValue<float>(sut.Key, 10, TimeSpan.FromSeconds(10)),
                new DataModelValue<float>(sut.Key, 11, TimeSpan.FromSeconds(10))
            );
        });
    }


    [Test]
    public void first_test()
    {
        var sut = ValuesStream.Create(StreamKey, _tsReader, _tsWriter, TimeSpan.FromSeconds(10));

        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(0), 1);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(1), 2);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(2), 3);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(10), 10);

        var result = sut.First();
        result.CheckIsSomeAnd(value => Check.That(value).IsEqualTo(new DataModelValue<float>(sut.Key, 1, TimeSpan.FromSeconds(0))));
    }

    [Test]
    public void first_applies_retention_test()
    {
        var sut = ValuesStream.Create(StreamKey, _tsReader, _tsWriter, TimeSpan.FromSeconds(10));

        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(0), 1);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(1), 2);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(10), 10);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(11), 11);

        var result = sut.First();
        result.CheckIsSomeAnd(value => Check.That(value).IsEqualTo(new DataModelValue<float>(sut.Key, 2, TimeSpan.FromSeconds(1))));
    }


    [Test]
    public void last_test()
    {
        var sut = ValuesStream.Create(StreamKey, _tsReader, _tsWriter, TimeSpan.FromSeconds(10));

        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(0), 1);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(1), 2);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(10), 10);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(11), 11);

        var result = sut.Last();
        result.CheckIsSomeAnd(value => Check.That(value).IsEqualTo(new DataModelValue<float>(sut.Key, 11, TimeSpan.FromSeconds(11))));
    }

    [Test]
    public void last_test_applies_retention_policy()
    {
        var sut = ValuesStream.Create(StreamKey, _tsReader, _tsWriter, TimeSpan.FromSeconds(10));

        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(0), 1);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(1), 2);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(10), 10);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(11), 11);

        var last = sut.Last();
        var all = _tsReader.ReadAll(sut.Key);
        last.CheckIsSomeAnd(value => Check.That(value).IsEqualTo(new DataModelValue<float>(sut.Key, 11, TimeSpan.FromSeconds(11))));
        all.CheckIsSomeAnd(values => Check.That(values).ContainsExactly(
            new DataModelValue<float>(sut.Key, 2, TimeSpan.FromSeconds(1)),
            new DataModelValue<float>(sut.Key, 10, TimeSpan.FromSeconds(10)),
            new DataModelValue<float>(sut.Key, 11, TimeSpan.FromSeconds(11))
        ));
    }

    [Test]
    public void last_applies_retention_policy_with_getPeriodStartTime()
    {
        var sut = ValuesStream.Create(StreamKey, _tsReader, _tsWriter, TimeSpan.FromSeconds(2), () => TimeSpan.FromSeconds(4));
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(0), 1);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(1), 2);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(2), 3);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(3), 4);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(4), 5);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(5), 6);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(6), 7);
        _tsWriter.Write(sut.Key, TimeSpan.FromSeconds(7), 8);
        var last = sut.Last();
        last.CheckIsSomeAnd(value =>
            Check.That(value).IsEqualTo(new DataModelValue<float>(sut.Key, 6, TimeSpan.FromSeconds(5)))
        );
    }

    [Test]
    public void create_stream_with_urn_only()
    {
        var sut = ValuesStream.Create(StreamKey, _tsReader, _tsWriter, TimeSpan.FromSeconds(10));
        Check.That(sut.ReadAllPeriodValues().GetValue()).CountIs(0);
    }
}