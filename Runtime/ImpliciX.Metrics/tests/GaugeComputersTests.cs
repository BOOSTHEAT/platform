using System;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.Metrics.Computers;
using ImpliciX.Metrics.Tests.Helpers;
using ImpliciX.SharedKernel.Storage;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using PDH = ImpliciX.TestsCommon.PropertyDataHelper;

namespace ImpliciX.Metrics.Tests;

[TestFixture(Category = "ExcludeFromCI")]
[NonParallelizable]
public class GaugeComputersTests
{
    private readonly TimeHelper T = TimeHelper.Minutes();
    private IWriteTimeSeries _tsWriter;
    private IReadTimeSeries _tsReader;


    [SetUp]
    public void Init()
    {
        //_helper = new Helper();
        var dbPath = "/tmp/gauge_tests";
        if(System.IO.Directory.Exists(dbPath))
            System.IO.Directory.Delete(dbPath, true);
        var db = new TimeSeriesDb(dbPath,"test");
        _tsReader = db;
        _tsWriter = db;
    }

    [Test]
    public void should_not_publish_gauge_computer_when_value_is_empty()
    {
        var computer = CreateSut();
        var resultingEvents = computer.Publish(T._5);
        Check.That(resultingEvents).IsEqualTo(Option<Property<MetricValue>[]>.None());
    }

    [Test]
    public void should_keep_values_after_publish()
    {
        var sut = CreateSut();
        var updateValue = Property<Temperature>.Create(fake_model.temperature.measure,
            Temperature.Create(242f),
            TimeSpan.FromMinutes(10));

        sut.Update(updateValue);

        sut.Publish(T._5);
        sut.Update(T._10);
        var result = sut.Publish(T._10).GetValue();

        var expected = new []
        {
            PDH.CreateMetricValueProperty(fake_analytics_model.temperature, 242, 5, 10)
        };

        Check.That(result).IsEqualTo(expected);
    }
    
    [Test]
    public void should_not_keep_values_in_storage_after_publish()
    {
        var sut = CreateSut();
        var updateValueT9 = Property<Temperature>.Create(fake_model.temperature.measure,
            Temperature.Create(42f),
            TimeSpan.FromMinutes(9));

        var updateValueT10 = Property<Temperature>.Create(fake_model.temperature.measure,
            Temperature.Create(242f),
            TimeSpan.FromMinutes(10));

        sut.Update(updateValueT9);
        sut.Update(T._9);
       var p1 = sut.Publish(T._9).GetValueOrDefault(Array.Empty<Property<MetricValue>>());

        sut.Update(updateValueT10);
        sut.Update(T._10);
        var p2 = sut.Publish(T._10).GetValueOrDefault(Array.Empty<Property<MetricValue>>());

        Check.That(p1).ContainsExactly(PDH.CreateMetricValueProperty(fake_analytics_model.temperature, 42, 0, 9));
        Check.That(p2).ContainsExactly(PDH.CreateMetricValueProperty(fake_analytics_model.temperature, 242, 9, 10));
        
        _tsReader.ReadAll(fake_analytics_model.temperature).CheckIsSomeAnd((it)=>Check.That(it).HasSize(0));
    }

    private GaugeComputer CreateSut()
        => new (fake_analytics_model.temperature, _tsReader, _tsWriter, TimeSpan.Zero);
}