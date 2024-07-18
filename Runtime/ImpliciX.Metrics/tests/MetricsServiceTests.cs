using System;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.Metrics.Tests.Helpers;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using MetricsDSL = ImpliciX.Language.Metrics.Metrics;
using MFactory = ImpliciX.TestsCommon.MetricFactoryHelper;
using PDH = ImpliciX.TestsCommon.PropertyDataHelper;

namespace ImpliciX.Metrics.Tests;

[NonParallelizable]
public class MetricsServiceTests
{
    private readonly TimeHelper T = TimeHelper.Minutes();
    private IWriteTimeSeries _tsWriter;
    private IReadTimeSeries _tsReader;
    private MetricsService _sut;
    private MetricsServiceTestContext _context;


    [SetUp]
    public void Init()
    {
        const string dbPath = "/tmp/metric_service_tests";
        if (System.IO.Directory.Exists(dbPath))
            System.IO.Directory.Delete(dbPath, true);

        var db = new TimeSeriesDb(dbPath, "test");
        _tsReader = db;
        _tsWriter = db;
        _sut = new MetricsService(TimeSpan.FromMinutes(1), () => TimeSpan.Zero);
        _context = new MetricsServiceTestContext(_sut);
    }

    [Test]
    public void ItShouldNotBePossibleToHaveMetricsWithTheSameTargetUrn()
    {
        var outputUrn = MetricUrn.Build("root", "foo");
        var metrics = new IMetric[]
        {
            MFactory.CreateGaugeMetric(outputUrn, "myInputUrn_1", 5).Builder.Build<Metric<MetricUrn>>(),
            MFactory.CreateGaugeMetric(MetricUrn.Build("root", "buzz"), "myInputUrn_1", 5).Builder.Build<Metric<MetricUrn>>(),
            MFactory.CreateGaugeMetric(outputUrn, "myInputUrn_2", 10).Builder.Build<Metric<MetricUrn>>()
        };

        var sut = new MetricsService(T._1, () => T._1);
        var ex = Check.ThatCode(() => sut.Initialize(metrics, _tsReader, _tsWriter))
            .Throws<InvalidOperationException>()
            .Value;

        Check.That(ex.Message).EndsWith($"duplicated : {outputUrn}");
    }

    [Test]
    public void AllMetricsMustBeMetricOfMetricUrn()
    {
        var node = new AnalyticsCommunicationCountersNode("name", null!);
        var metric = Language.Metrics.Metrics.Metric(node)
            .Is
            .Minutely
            .DeviceMonitoringOf(new DeviceNode("urnToken", null!))
            .Builder.Build<Metric<AnalyticsCommunicationCountersNode>>();

        var sut = new MetricsService(T._1, () => T._1);
        Check.ThatCode(() => sut.Initialize(new IMetric[] {metric}, _tsReader, _tsWriter)).Throws<InvalidCastException>();
    }

    [Test]
    public void GivenMetricWithWindowPeriodIsShorterThanPrimaryPeriod_WhenInitialize_ThenIGetAnError()
    {
        var sut = new MetricsService(T._1, () => T._1);

        var windowedVariation = MetricsDSL.Metric(MetricUrn.Build("myOutputUrn"))
            .Is
            .Every(3).Minutes
            .OnAWindowOf(2).Minutes
            .VariationOf("myInputUrn")
            .Builder.Build<Metric<MetricUrn>>();

        var ex = Check.ThatCode(() => sut.Initialize(new IMetric[] {windowedVariation}, _tsReader, _tsWriter))
            .Throws<InvalidOperationException>()
            .Value;

        Check.That(ex.Message).Contains("Window period of Metric must be greater than primary publication period");
    }

    [Test]
    public void GivenMetricWithWindowPeriodIsEqualsToPrimaryPeriod_WhenInitialize_ThenIGetAnError()
    {
        var sut = new MetricsService(T._1, () => T._1);

        var windowedVariation = MetricsDSL.Metric(MetricUrn.Build("myOutputUrn"))
            .Is
            .Every(3).Minutes
            .OnAWindowOf(3).Minutes
            .VariationOf("myInputUrn")
            .Builder.Build<Metric<MetricUrn>>();

        var ex = Check.ThatCode(() => sut.Initialize(new IMetric[] {windowedVariation}, _tsReader, _tsWriter))
            .Throws<InvalidOperationException>()
            .Value;

        Check.That(ex.Message).Contains("Window period of Metric must be greater than primary publication period");
    }

    [Test]
    public void GivenMetricWithWindowPeriodIsGreaterThanPrimaryPeriod_WhenInitialize_ThenIGetNoError()
    {
        var sut = new MetricsService(T._1, () => T._1);

        var windowedVariation = MetricsDSL.Metric(MetricUrn.Build("myOutputUrn"))
            .Is
            .Every(3).Minutes
            .OnAWindowOf(6).Minutes
            .VariationOf("myInputUrn")
            .Builder.Build<Metric<MetricUrn>>();

        sut.Initialize(new IMetric[] {windowedVariation}, _tsReader, _tsWriter);
    }

    [Test]
    public void MetricsWithTimeSeries_should_send_correct_events_after_restart()
    {
        var snapshotInterval = T._1;

        const int publicationPeriodInMin = 10;
        var tempeInputUrn = fake_model.temperature.measure;

        var tempeGaugeUrn = MetricUrn.Build(nameof(fake_model.temperature), "gauge");
        var gauge = MFactory
            .CreateGaugeMetric(tempeGaugeUrn, tempeInputUrn, publicationPeriodInMin)
            .Builder.Build<Metric<MetricUrn>>();

        var tempeDeltaUrn = fake_analytics_model.temperature_delta;
        var variation = MFactory
            .CreateVariationMetric(tempeDeltaUrn, tempeInputUrn, publicationPeriodInMin)
            .Builder.Build<Metric<MetricUrn>>();

        var tempeAccUrn = fake_analytics_model.sample_metric;
        var accumulator = MFactory
            .CreateAccumulatorMetric(tempeAccUrn, tempeInputUrn, publicationPeriodInMin)
            .Builder.Build<Metric<MetricUrn>>();

        var stateOutputUrn = fake_analytics_model.public_state_A;
        var stateInputUrn = PropertyUrn<fake_model.PublicState>.Build(fake_model.public_state);
        const string stateVarName = "state_temp_delta";
        const string stateAccName = "state_acc";
        var stateMonitoring = MFactory
            .CreateStateMonitoringOfMetric(stateOutputUrn, stateInputUrn, publicationPeriodInMin)
            .Including(stateVarName).As.VariationOf(tempeInputUrn)
            .Including(stateAccName).As.AccumulatorOf(tempeInputUrn)
            .Builder.Build<Metric<MetricUrn>>();

        IMetric[] metrics = {gauge, variation, accumulator, stateMonitoring};

        //   STOP APP at:                         |
        //RESTART APP at:                                 |                                
        //         Time : 0   1   2   3   4   5   6   7   8
        //         State:     DIS         RUN     |    
        //   Temperature:     5   20              |
        // PUB ----------                         |
        //         Gauge:                         |      20
        //         Delta:                         |      15
        //      AccValue:                         |      25
        //      AccCount:                         |      2
        // DISABLED                               |
        //     occurence:                         |       1
        //      duration:                         |       3 (4-1)
        //  Varia. Tempe:                         |      15 (20-5)
        //     Acc value:                         |      25
        //     Acc count:                         |       2
        // RUNNING                                |
        //     occurence:                         |       1
        //      duration:                         |       2 (6-4)

        var turnOffAt = T._6;
        void GivenAppIsRunning()
        {
            var sut = new MetricsService(snapshotInterval, () => T._1);
            sut.Initialize(metrics, _tsReader, _tsWriter);

            var context = new MetricsServiceTestContext(sut);

            context.ChangeStateTo(fake_model.PublicState.Disabled, 1);
            context.ChangeTemperature(1, 5f);
            context.ChangeTemperature(2, 20f);
            context.ChangeStateTo(fake_model.PublicState.Running, 4);

            var events = context.AdvanceTimeTo(turnOffAt);
            Check.That(events).IsEmpty();
        }

        var restartDate = T._8;
        DomainEvent[] WhenAppReboot()
        {
            var sut = new MetricsService(snapshotInterval, () => restartDate);
            return sut.Initialize(metrics, _tsReader, _tsWriter);
        }

        GivenAppIsRunning();

        var result = WhenAppReboot();

        // Then
        var turnOffAtInMinutes = (int) turnOffAt.TotalMinutes;

        var pubGaugeExpected = PropertiesChanged.Create(tempeGaugeUrn, new IDataModelValue[]
        {
            PDH.CreateMetricValueProperty(tempeGaugeUrn, 20f, 1, turnOffAtInMinutes)
        }, restartDate);

        var pubVariationExpected = PropertiesChanged.Create(tempeDeltaUrn, new IDataModelValue[]
        {
            PDH.CreateMetricValueProperty(tempeDeltaUrn, 15f, 1, turnOffAtInMinutes)
        }, restartDate);

        var pubAccumulatorExpected = PropertiesChanged.Create(tempeAccUrn, new IDataModelValue[]
        {
            PDH.CreateAccumulatorValue(tempeAccUrn, 25f, 1, turnOffAtInMinutes),
            PDH.CreateAccumulatorCount(tempeAccUrn, 2f, 1, turnOffAtInMinutes)
        }, restartDate);

        var running_BaseUrn = new[] {fake_analytics_model.public_state_A, fake_model.PublicState.Running.ToString()};
        var disabled_BaseUrn = new[] {fake_analytics_model.public_state_A, fake_model.PublicState.Disabled.ToString()};
        var pubStateMonitoringExpected = PropertiesChanged.Create(fake_analytics_model.public_state_A, new IDataModelValue[]
        {
            PDH.CreateStateOccurenceProperty(disabled_BaseUrn, 1, 1, turnOffAtInMinutes),
            PDH.CreateStateDurationProperty(disabled_BaseUrn, 3, 1, turnOffAtInMinutes),
            PDH.CreateStateOccurenceProperty(running_BaseUrn, 1, 1, turnOffAtInMinutes),
            PDH.CreateStateDurationProperty(running_BaseUrn, 2, 1, turnOffAtInMinutes),

            PDH.CreateMetricValueProperty(disabled_BaseUrn.Append(stateVarName), 15f, 1, turnOffAtInMinutes),
            PDH.CreateMetricValueProperty(running_BaseUrn.Append(stateVarName), 0f, 1, turnOffAtInMinutes),

            PDH.CreateAccumulatorCount(disabled_BaseUrn.Append(stateAccName), 2f, 1, turnOffAtInMinutes),
            PDH.CreateAccumulatorValue(disabled_BaseUrn.Append(stateAccName), 25f, 1, turnOffAtInMinutes),
            PDH.CreateAccumulatorCount(running_BaseUrn.Append(stateAccName), 0f, 1, turnOffAtInMinutes),
            PDH.CreateAccumulatorValue(running_BaseUrn.Append(stateAccName), 0f, 1, turnOffAtInMinutes)
        }, restartDate);

        DomainEvent[] expected =
        {
            pubGaugeExpected,
            pubVariationExpected,
            pubAccumulatorExpected,
            pubStateMonitoringExpected
        };

        Check.That(result).ContainsExactly(expected);
    }

    [Test]
    public void minimal_test()
    {
        const string inputUrn = "foo:measure";
        var metric = MetricsDSL.Metric(MetricUrn.Build("foo:variation"))
            .Is
            .Every(3).Minutes
            .VariationOf(inputUrn)
            .Builder.Build<Metric<MetricUrn>>();

        _sut.Initialize(new IMetric[] {metric}, _tsReader, _tsWriter);

        //  Temps(min): 0 1 2 3 
        //       Value: 1   5  
        //Publish 3min:       4 

        _context.ChangeFloatTo(inputUrn, 0, 1f);
        _context.ChangeFloatTo(inputUrn, 1, 1f);
        _context.ChangeFloatTo(inputUrn, 2, 5f);
        var outcome = _context.AdvanceTimeTo(3).Single();
        var value = ((IFloat) outcome.ModelValue()).ToFloat();

        Check.That(value).IsEqualTo(4f);
    }

    [Test]
    public void GivenVariationWithGroups_WhenIChangeTriggerUrnValue_ThenIGetEventsExpected()
    {
        var metric = MFactory.CreateVariationMetric(fake_analytics_model.temperature, fake_model.temperature.measure, 3)
            .Group.Every(4).Minutes
            .Group.Every(5).Minutes
            .Builder.Build<Metric<MetricUrn>>();

        _sut.Initialize(new IMetric[] {metric}, _tsReader, _tsWriter);

        //  Temps(min): 1  2.98 3  4  5  6  7  8  9  10  11  12  14 15
        //       Value: 10 15   13 20 30 35 07 32 38     40      31
        //Publish 3min:         5        15       2           8     -9 = 21
        //Publish 4min:            3           -6            33        = 30
        //Publish 5min:               10             18             -7 = 21 
        //      Global:                                      30  21(31-10)

        _context.ChangeTemperature(1, 10f);
        _context.AdvanceTimeTo(1);
        _context.CheckLastPropertiesReceived(Array.Empty<Property<MetricValue>>());
        _context.ChangeTemperature(TimeSpan.FromSeconds(179), 15f);
        _context.ChangeTemperature(3, 13f);
        _context.AdvanceTimeTo(3);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value}, 5f, 0, 3, 3));
        _context.ChangeTemperature(4, 20f);
        _context.AdvanceTimeTo(4);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value, "_4Minutes"}, 3f, 0, 4, 4));
        _context.ChangeTemperature(5, 30f);
        _context.AdvanceTimeTo(5);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value, "_5Minutes"}, 10f, 0, 5, 5));
        _context.ChangeTemperature(6, 35f);
        _context.AdvanceTimeTo(6);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value}, 15f, 3, 6, 6));
        _context.ChangeTemperature(7, 7f);
        _context.AdvanceTimeTo(7);
        _context.CheckLastPropertiesReceived(Array.Empty<Property<MetricValue>>());
        _context.ChangeTemperature(8, 32f);
        _context.AdvanceTimeTo(8);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value, "_4Minutes"}, -6f, 4, 8, 8));
        _context.ChangeTemperature(9, 38f);
        _context.AdvanceTimeTo(9);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value}, 2f, 6, 9, 9));
        _context.AdvanceTimeTo(10);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value, "_5Minutes"}, 18f, 5, 10, 10));
        _context.ChangeTemperature(11, 40f);
        _context.AdvanceTimeTo(12);
        _context.CheckLastPropertiesReceived(
            PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value, "_4Minutes"}, 33f, 8, 12, 12), // For every 4 minutes
            PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value}, 8f, 9, 12, 12) // For every 3 minutes
        );

        _context.ChangeTemperature(14, 31f);
        _context.AdvanceTimeTo(15);
        _context.CheckLastPropertiesReceived(
            PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value, "_5Minutes"}, -7f, 10, 15, 15), // For every 5 minutes
            PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value}, -9f, 12, 15, 15) // For every 3 minutes
        );
    }

    [Test]
    public void GivenGaugeWithGroups_WhenIChangeTriggerUrnValue_ThenIGetEventsExpected()
    {
        var metric = MFactory.CreateGaugeMetric(fake_analytics_model.temperature, fake_model.temperature.measure, 3)
            .Group.Every(4).Minutes
            .Group.Every(5).Minutes
            .Builder.Build<Metric<MetricUrn>>();

        _sut.Initialize(new IMetric[] {metric}, _tsReader, _tsWriter);

        //  Temps(min): 1  2  3  4  5  6  7  8  9  10 11 12 14 15
        //       Value: 10 15 13 20 30 35 07 32 38    40    31
        //Publish 3min:       13       35       38       40    31
        //Publish 4min:          20          32          40
        //Publish 5min:             30             38          31

        _context.ChangeTemperature(1, 10f);
        _context.AdvanceTimeTo(1);
        _context.CheckLastPropertiesReceived(Array.Empty<Property<MetricValue>>());
        _context.ChangeTemperature(2, 15f);
        _context.ChangeTemperature(3, 13f);
        _context.AdvanceTimeTo(3);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value}, 13f, 0, 3, 3));
        _context.ChangeTemperature(4, 20f);
        _context.AdvanceTimeTo(4);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value, "_4Minutes"}, 20f, 0, 4, 4));
        _context.ChangeTemperature(5, 30f);
        _context.AdvanceTimeTo(5);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value, "_5Minutes"}, 30f, 0, 5, 5));
        _context.ChangeTemperature(6, 35f);
        _context.AdvanceTimeTo(6);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value}, 35f, 3, 6, 6));
        _context.ChangeTemperature(7, 7f);
        _context.AdvanceTimeTo(7);
        _context.CheckLastPropertiesReceived(Array.Empty<Property<MetricValue>>());
        _context.ChangeTemperature(8, 32f);
        _context.AdvanceTimeTo(8);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value, "_4Minutes"}, 32f, 4, 8, 8));
        _context.ChangeTemperature(9, 38f);
        _context.AdvanceTimeTo(9);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value}, 38f, 6, 9, 9));
        _context.AdvanceTimeTo(10);
        _context.CheckLastPropertiesReceived(PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value, "_5Minutes"}, 38f, 5, 10, 10));
        _context.ChangeTemperature(11, 40f);
        _context.AdvanceTimeTo(12);
        _context.CheckLastPropertiesReceived(
            PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value, "_4Minutes"}, 40f, 8, 12, 12), // For every 4 minutes
            PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value}, 40f, 9, 12, 12) // For every 3 minutes
        );

        _context.ChangeTemperature(14, 31f);
        _context.AdvanceTimeTo(15);
        _context.CheckLastPropertiesReceived(
            PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value, "_5Minutes"}, 31f, 10, 15, 15), // For every 5 minutes
            PDH.CreateMetricValueProperty(new[] {fake_analytics_model.temperature.Value}, 31f, 12, 15, 15) // For every 3 minutes
        );
    }

    [Test]
    public void GivenAccumulatorWithGroups_WhenIChangeTriggerUrnValue_ThenIGetEventsExpected()
    {
        var metric = MFactory.CreateAccumulatorMetric(fake_analytics_model.temperature, fake_model.temperature.measure, 3)
            .Group.Every(4).Minutes
            .Group.Every(5).Minutes
            .Builder.Build<Metric<MetricUrn>>();

        _sut.Initialize(new IMetric[] {metric}, _tsReader, _tsWriter);

        var accUrn = new[] {fake_analytics_model.temperature.Value};
        var countUrn = new[] {fake_analytics_model.temperature.Value};
        var accUrn_4Min = new[] {fake_analytics_model.temperature.Value, "_4Minutes"};
        var countUrn_4min = new[] {fake_analytics_model.temperature.Value, "_4Minutes"};
        var accUrn_5Min = new[] {fake_analytics_model.temperature.Value, "_5Minutes"};
        var countUrn_5min = new[] {fake_analytics_model.temperature.Value, "_5Minutes"};

        //         Temps(min): 1  2.98 3  4  5  6  7  8  9  10 11 12 14 15
        //              Value: 10 15   13 20 30 35 07 32 38    40    31
        // Publish 3min Count:         2        3        3        2     1 
        //   Publish 3min Acc:         25       63       74       78    31
        // Publish 4min Count:            3           4           3
        //   Publish 4min Acc:            38          92          110
        // Publish 5min Count:               4              5           2
        //   Publish 5min Acc:               58             142         71

        _context.ChangeTemperature(1, 10f);
        _context.AdvanceTimeTo(1);
        _context.CheckLastPropertiesReceived(Array.Empty<Property<MetricValue>>());
        _context.ChangeTemperature(TimeSpan.FromSeconds(179), 15f);
        _context.ChangeTemperature(3, 13f);
        _context.AdvanceTimeTo(3);
        _context.CheckLastPropertiesReceived(
            PDH.CreateAccumulatorValue(accUrn, 25f, 0, 3),
            PDH.CreateAccumulatorCount(countUrn, 2f, 0, 3)
        );
        
        _context.ChangeTemperature(4, 20f);
        _context.AdvanceTimeTo(4);
        _context.CheckLastPropertiesReceived(
            PDH.CreateAccumulatorValue(accUrn_4Min, 38f, 0, 4),
            PDH.CreateAccumulatorCount(countUrn_4min, 3f, 0, 4)
        );

        _context.ChangeTemperature(5, 30f);
        _context.AdvanceTimeTo(5);
        _context.CheckLastPropertiesReceived(
            PDH.CreateAccumulatorValue(accUrn_5Min, 58f, 0, 5),
            PDH.CreateAccumulatorCount(countUrn_5min, 4f, 0, 5)
        );

        _context.ChangeTemperature(6, 35f);
        _context.AdvanceTimeTo(6);
        _context.CheckLastPropertiesReceived(
            PDH.CreateAccumulatorValue(accUrn, 63f, 3, 6),
            PDH.CreateAccumulatorCount(countUrn, 3f, 3, 6)
        );

        _context.AdvanceTimeTo(7);
        _context.CheckLastPropertiesReceived(Array.Empty<Property<MetricValue>>());
        _context.ChangeTemperature(7, 7f);
        _context.ChangeTemperature(8, 32f);
        _context.AdvanceTimeTo(8);
        _context.CheckLastPropertiesReceived(
            PDH.CreateAccumulatorValue(accUrn_4Min, 92f, 4, 8),
            PDH.CreateAccumulatorCount(countUrn_4min, 4f, 4, 8)
        );

        _context.ChangeTemperature(9, 38f);
        _context.AdvanceTimeTo(9);
        _context.CheckLastPropertiesReceived(
            PDH.CreateAccumulatorValue(accUrn, 74f, 6, 9),
            PDH.CreateAccumulatorCount(countUrn, 3f, 6, 9)
        );

        _context.AdvanceTimeTo(10);
        _context.CheckLastPropertiesReceived(
            PDH.CreateAccumulatorValue(accUrn_5Min, 142f, 5, 10),
            PDH.CreateAccumulatorCount(countUrn_5min, 5f, 5, 10)
        );

        _context.ChangeTemperature(11, 40f);
        _context.AdvanceTimeTo(12);
        _context.CheckLastPropertiesReceived(
            PDH.CreateAccumulatorValue(accUrn_4Min, 110f, 8, 12),
            PDH.CreateAccumulatorCount(countUrn_4min, 3f, 8, 12),
            PDH.CreateAccumulatorValue(accUrn, 78f, 9, 12),
            PDH.CreateAccumulatorCount(countUrn, 2f, 9, 12)
        );

        _context.ChangeTemperature(14, 31f);
        _context.AdvanceTimeTo(15);
        _context.CheckLastPropertiesReceived(
            PDH.CreateAccumulatorValue(accUrn_5Min, 71f, 10, 15),
            PDH.CreateAccumulatorCount(countUrn_5min, 2f, 10, 15),
            PDH.CreateAccumulatorValue(accUrn, 31f, 12, 15),
            PDH.CreateAccumulatorCount(countUrn, 1f, 12, 15)
        );
    }

    [Test]
    public void GivenStateMonitoringWithGroups_WhenIChangeTriggerUrnValue_ThenIGetEventsExpected()
    {
        var metric = MFactory.CreateStateMonitoringOfMetric(fake_analytics_model.public_state_A, fake_model.public_state, 3)
            .Including("fake_index_delta").As.VariationOf(fake_model.fake_index)
            .Including("temperature_acc").As.AccumulatorOf(fake_model.temperature.measure)
            .Group.Every(4).Minutes
            .Group.Every(5).Minutes
            .Builder.Build<Metric<MetricUrn>>();

        var Running_BaseUrn = new[] {fake_analytics_model.public_state_A, "Running"};
        var Disabled_BaseUrn = new[] {fake_analytics_model.public_state_A, "Disabled"};
        var Running_BaseUrn_4min = new[] {fake_analytics_model.public_state_A, "_4Minutes", "Running"};
        var Disabled_BaseUrn_4min = new[] {fake_analytics_model.public_state_A, "_4Minutes", "Disabled"};
        var Running_BaseUrn_5min = new[] {fake_analytics_model.public_state_A, "_5Minutes", "Running"};
        var Disabled_BaseUrn_5min = new[] {fake_analytics_model.public_state_A, "_5Minutes", "Disabled"};

        _sut.Initialize(new IMetric[] {metric}, _tsReader, _tsWriter);

        //         Temps(min): 1  2.98 3   4   5   6   7   8   9  10  11  12  14  15
        //              State: RUN
        //        Temperature: 10 15   13  20  30  35  07  32  38     40      31
        //         fake_index: 30 8        14
        //   Delta fake_index:        -22 -22  6   6       0   0  0       0       0
        // Publish 3min Count:         2           3           3          2       1
        //   Publish 3min Acc:         25          63          74         78      31
        // Publish 4min Count:             3               4              3
        //   Publish 4min Acc:             38              92             110
        // Publish 5min Count:                4                   5               2
        //   Publish 5min Acc:                58                  142             71

        _context.ChangeStateTo(fake_model.PublicState.Running, 0);
        _context.ChangeTemperature(1, 10f);
        _context.ChangeFloatTo(fake_model.fake_index, 1, 30f);
        _context.AdvanceTimeTo(1);
        _context.CheckLastPropertiesReceived(Array.Empty<Property<MetricValue>>());
        _context.ChangeTemperature(TimeSpan.FromSeconds(179), 15f);
        _context.ChangeFloatTo(fake_model.fake_index, TimeSpan.FromSeconds(179), 8f);
        _context.ChangeTemperature(3, 13f);
        _context.AdvanceTimeTo(3);
        _context.CheckLastPropertiesReceived(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 0, 3),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 0, 3),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 0, 3),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 0, 3),
            PDH.CreateMetricValueProperty(Disabled_BaseUrn.Append("fake_index_delta"), 0f, 0, 3),
            PDH.CreateMetricValueProperty(Running_BaseUrn.Append("fake_index_delta"), -22f, 0, 3),
            PDH.CreateAccumulatorCount(Disabled_BaseUrn.Append("temperature_acc"), 0f, 0, 3),
            PDH.CreateAccumulatorValue(Disabled_BaseUrn.Append("temperature_acc"), 0f, 0, 3),
            PDH.CreateAccumulatorCount(Running_BaseUrn.Append("temperature_acc"), 2f, 0, 3),
            PDH.CreateAccumulatorValue(Running_BaseUrn.Append("temperature_acc"), 25f, 0, 3)
        );

        _context.ChangeTemperature(4, 20f);
        _context.AdvanceTimeTo(4);
        _context.CheckLastPropertiesReceived(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn_4min, 0, 0, 4),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn_4min, 0, 0, 4),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn_4min, 1, 0, 4),
            PDH.CreateStateDurationProperty(Running_BaseUrn_4min, 4, 0, 4),
            PDH.CreateMetricValueProperty(Disabled_BaseUrn_4min.Append("fake_index_delta"), 0f, 0, 4),
            PDH.CreateMetricValueProperty(Running_BaseUrn_4min.Append("fake_index_delta"), -22f, 0, 4),
            PDH.CreateAccumulatorCount(Disabled_BaseUrn_4min.Append("temperature_acc"), 0f, 0, 4),
            PDH.CreateAccumulatorValue(Disabled_BaseUrn_4min.Append("temperature_acc"), 0f, 0, 4),
            PDH.CreateAccumulatorCount(Running_BaseUrn_4min.Append("temperature_acc"), 3f, 0, 4),
            PDH.CreateAccumulatorValue(Running_BaseUrn_4min.Append("temperature_acc"), 38f, 0, 4)
        );

        _context.ChangeFloatTo(fake_model.fake_index, 4, 14f);
        _context.ChangeTemperature(5, 30f);
        _context.AdvanceTimeTo(5);
        _context.CheckLastPropertiesReceived(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn_5min, 0, 0, 5),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn_5min, 0, 0, 5),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn_5min, 1, 0, 5),
            PDH.CreateStateDurationProperty(Running_BaseUrn_5min, 5, 0, 5),
            PDH.CreateMetricValueProperty(Disabled_BaseUrn_5min.Append("fake_index_delta"), 0f, 0, 5),
            PDH.CreateMetricValueProperty(Running_BaseUrn_5min.Append("fake_index_delta"), -16f, 0, 5),
            PDH.CreateAccumulatorCount(Disabled_BaseUrn_5min.Append("temperature_acc"), 0f, 0, 5),
            PDH.CreateAccumulatorValue(Disabled_BaseUrn_5min.Append("temperature_acc"), 0f, 0, 5),
            PDH.CreateAccumulatorCount(Running_BaseUrn_5min.Append("temperature_acc"), 4f, 0, 5),
            PDH.CreateAccumulatorValue(Running_BaseUrn_5min.Append("temperature_acc"), 58f, 0, 5)
        );

        _context.ChangeTemperature(6, 35f);
        _context.AdvanceTimeTo(6);
        _context.CheckLastPropertiesReceived(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 3, 6),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 3, 6),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 3, 6),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 3, 6),
            PDH.CreateMetricValueProperty(Disabled_BaseUrn.Append("fake_index_delta"), 0f, 3, 6),
            PDH.CreateMetricValueProperty(Running_BaseUrn.Append("fake_index_delta"), 6f, 3, 6),
            PDH.CreateAccumulatorCount(Disabled_BaseUrn.Append("temperature_acc"), 0f, 3, 6),
            PDH.CreateAccumulatorValue(Disabled_BaseUrn.Append("temperature_acc"), 0f, 3, 6),
            PDH.CreateAccumulatorCount(Running_BaseUrn.Append("temperature_acc"), 3f, 3, 6),
            PDH.CreateAccumulatorValue(Running_BaseUrn.Append("temperature_acc"), 63f, 3, 6)
        );

        _context.AdvanceTimeTo(7);
        _context.CheckLastPropertiesReceived(Array.Empty<Property<MetricValue>>());
        _context.ChangeTemperature(7, 7f);
        _context.ChangeTemperature(8, 32f);
        _context.AdvanceTimeTo(8);
        _context.CheckLastPropertiesReceived(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn_4min, 0, 4, 8),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn_4min, 0, 4, 8),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn_4min, 1, 4, 8),
            PDH.CreateStateDurationProperty(Running_BaseUrn_4min, 4, 4, 8),
            PDH.CreateMetricValueProperty(Disabled_BaseUrn_4min.Append("fake_index_delta"), 0f, 4, 8),
            PDH.CreateMetricValueProperty(Running_BaseUrn_4min.Append("fake_index_delta"), 6f, 4, 8),
            PDH.CreateAccumulatorCount(Disabled_BaseUrn_4min.Append("temperature_acc"), 0f, 4, 8),
            PDH.CreateAccumulatorValue(Disabled_BaseUrn_4min.Append("temperature_acc"), 0f, 4, 8),
            PDH.CreateAccumulatorCount(Running_BaseUrn_4min.Append("temperature_acc"), 4f, 4, 8),
            PDH.CreateAccumulatorValue(Running_BaseUrn_4min.Append("temperature_acc"), 92f, 4, 8)
        );

        _context.ChangeTemperature(9, 38f);
        _context.AdvanceTimeTo(9);
        _context.CheckLastPropertiesReceived(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 6, 9),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 6, 9),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 6, 9),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 6, 9),
            PDH.CreateMetricValueProperty(Disabled_BaseUrn.Append("fake_index_delta"), 0f, 6, 9),
            PDH.CreateMetricValueProperty(Running_BaseUrn.Append("fake_index_delta"), 0f, 6, 9),
            PDH.CreateAccumulatorCount(Disabled_BaseUrn.Append("temperature_acc"), 0f, 6, 9),
            PDH.CreateAccumulatorValue(Disabled_BaseUrn.Append("temperature_acc"), 0f, 6, 9),
            PDH.CreateAccumulatorCount(Running_BaseUrn.Append("temperature_acc"), 3f, 6, 9),
            PDH.CreateAccumulatorValue(Running_BaseUrn.Append("temperature_acc"), 74f, 6, 9)
        );

        _context.AdvanceTimeTo(10);
        _context.CheckLastPropertiesReceived(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn_5min, 0, 5, 10),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn_5min, 0, 5, 10),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn_5min, 1, 5, 10),
            PDH.CreateStateDurationProperty(Running_BaseUrn_5min, 5, 5, 10),
            PDH.CreateMetricValueProperty(Disabled_BaseUrn_5min.Append("fake_index_delta"), 0f, 5, 10),
            PDH.CreateMetricValueProperty(Running_BaseUrn_5min.Append("fake_index_delta"), 0f, 5, 10),
            PDH.CreateAccumulatorCount(Disabled_BaseUrn_5min.Append("temperature_acc"), 0f, 5, 10),
            PDH.CreateAccumulatorValue(Disabled_BaseUrn_5min.Append("temperature_acc"), 0f, 5, 10),
            PDH.CreateAccumulatorCount(Running_BaseUrn_5min.Append("temperature_acc"), 5f, 5, 10),
            PDH.CreateAccumulatorValue(Running_BaseUrn_5min.Append("temperature_acc"), 142f, 5, 10)
        );

        _context.ChangeTemperature(11, 40f);
        _context.AdvanceTimeTo(12);
        _context.CheckLastPropertiesReceived(
            // For every 3 minutes
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 9, 12),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 9, 12),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 9, 12),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 9, 12),
            PDH.CreateMetricValueProperty(Disabled_BaseUrn.Append("fake_index_delta"), 0f, 9, 12),
            PDH.CreateMetricValueProperty(Running_BaseUrn.Append("fake_index_delta"), 0f, 9, 12),
            PDH.CreateAccumulatorCount(Disabled_BaseUrn.Append("temperature_acc"), 0f, 9, 12),
            PDH.CreateAccumulatorValue(Disabled_BaseUrn.Append("temperature_acc"), 0f, 9, 12),
            PDH.CreateAccumulatorCount(Running_BaseUrn.Append("temperature_acc"), 2f, 9, 12),
            PDH.CreateAccumulatorValue(Running_BaseUrn.Append("temperature_acc"), 78f, 9, 12),

            // For every 4 minutes
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn_4min, 0, 8, 12),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn_4min, 0, 8, 12),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn_4min, 1, 8, 12),
            PDH.CreateStateDurationProperty(Running_BaseUrn_4min, 4, 8, 12),
            PDH.CreateMetricValueProperty(Disabled_BaseUrn_4min.Append("fake_index_delta"), 0f, 8, 12),
            PDH.CreateMetricValueProperty(Running_BaseUrn_4min.Append("fake_index_delta"), 0f, 8, 12),
            PDH.CreateAccumulatorCount(Disabled_BaseUrn_4min.Append("temperature_acc"), 0f, 8, 12),
            PDH.CreateAccumulatorValue(Disabled_BaseUrn_4min.Append("temperature_acc"), 0f, 8, 12),
            PDH.CreateAccumulatorCount(Running_BaseUrn_4min.Append("temperature_acc"), 3f, 8, 12),
            PDH.CreateAccumulatorValue(Running_BaseUrn_4min.Append("temperature_acc"), 110f, 8, 12)
        );

        _context.ChangeTemperature(14, 31f);
        _context.AdvanceTimeTo(15);
        _context.CheckLastPropertiesReceived(
            // For every 3 minutes
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 12, 15),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 12, 15),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 12, 15),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 12, 15),
            PDH.CreateMetricValueProperty(Disabled_BaseUrn.Append("fake_index_delta"), 0f, 12, 15),
            PDH.CreateMetricValueProperty(Running_BaseUrn.Append("fake_index_delta"), 0f, 12, 15),
            PDH.CreateAccumulatorCount(Disabled_BaseUrn.Append("temperature_acc"), 0f, 12, 15),
            PDH.CreateAccumulatorValue(Disabled_BaseUrn.Append("temperature_acc"), 0f, 12, 15),
            PDH.CreateAccumulatorCount(Running_BaseUrn.Append("temperature_acc"), 1f, 12, 15),
            PDH.CreateAccumulatorValue(Running_BaseUrn.Append("temperature_acc"), 31f, 12, 15),

            // For every 5 minutes
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn_5min, 0, 10, 15),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn_5min, 0, 10, 15),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn_5min, 1, 10, 15),
            PDH.CreateStateDurationProperty(Running_BaseUrn_5min, 5, 10, 15),
            PDH.CreateMetricValueProperty(Disabled_BaseUrn_5min.Append("fake_index_delta"), 0f, 10, 15),
            PDH.CreateMetricValueProperty(Running_BaseUrn_5min.Append("fake_index_delta"), 0f, 10, 15),
            PDH.CreateAccumulatorCount(Disabled_BaseUrn_5min.Append("temperature_acc"), 0f, 10, 15),
            PDH.CreateAccumulatorValue(Disabled_BaseUrn_5min.Append("temperature_acc"), 0f, 10, 15),
            PDH.CreateAccumulatorCount(Running_BaseUrn_5min.Append("temperature_acc"), 2f, 10, 15),
            PDH.CreateAccumulatorValue(Running_BaseUrn_5min.Append("temperature_acc"), 71f, 10, 15)
        );
    }
}