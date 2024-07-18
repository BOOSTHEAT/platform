using System;
using System.IO;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Control;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.Metrics.Computers;
using ImpliciX.Metrics.Tests.Helpers;
using ImpliciX.SharedKernel.Storage;
using NFluent;
using NUnit.Framework;
using MFactory = ImpliciX.TestsCommon.MetricFactoryHelper;

namespace ImpliciX.Metrics.Tests;

[NonParallelizable]
public class MetricComputerFactoryTests
{
    private MetricComputerFactory _sut;

    private IReadTimeSeries _tsReader;
    private IWriteTimeSeries _tsWriter;


    [SetUp]
    public void SetUp()
    {
        var folderPath = "/tmp/computer_factory";
        if (Directory.Exists(folderPath))
            Directory.Delete(folderPath, true);
        var db = new TimeSeriesDb(folderPath,"test");
        _tsReader = db;
        _tsWriter = db;
        _sut = new MetricComputerFactory(_tsReader, _tsWriter);
    }

    #region Gauge

    [Test]
    public void GivenSimpleGauge_WhenICreate()
    {
        var outputUrn = MetricUrn.Build("myOutputUrn");
        var inputUrn = Urn.BuildUrn("myInputUrn");

        var metric = MFactory.CreateGaugeMetric(outputUrn, inputUrn, 2)
            .Builder.Build<Metric<MetricUrn>>();

        var now = TimeSpan.FromMilliseconds(150);
        var infos = _sut.Create(metric, now);
        Check.That(infos).HasSize(1);

        CheckComputerRuntimeInfoExpected(
            infos.Single(),
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromMinutes(2), new GaugeComputer(outputUrn, _tsReader, _tsWriter, now))
        );
    }

    [Test]
    public void GivenGaugeWithGroups_WhenICreate()
    {
        var outputUrn = MetricUrn.Build("myOutputUrn");
        var inputUrn = Urn.BuildUrn("myInputUrn");

        var metric = MFactory.CreateGaugeMetric(outputUrn, inputUrn, 2)
            .Group.Hourly
            .Group.Every(7).Days
            .Builder.Build<Metric<MetricUrn>>();

        var now = TimeSpan.FromMilliseconds(150);
        var infos = _sut.Create(metric, now);
        Check.That(infos).HasSize(3);

        CheckComputerRuntimeInfoExpected(infos[0],
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromHours(1),
                new GaugeComputer(MetricUrn.Build(outputUrn, "_1Hours"), _tsReader, _tsWriter, now))
        );

        CheckComputerRuntimeInfoExpected(infos[1],
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromDays(7),
                new GaugeComputer(MetricUrn.Build(outputUrn, "_7Days"), _tsReader, _tsWriter, now))
        );

        CheckComputerRuntimeInfoExpected(infos[2],
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromMinutes(2), new GaugeComputer(outputUrn, _tsReader, _tsWriter, now))
        );
    }

    #endregion

    #region Variation

    [Test]
    public void GivenSimpleVariation_WhenICreate()
    {
        var outputUrn = MetricUrn.Build("myOutputUrn");
        var inputUrn = Urn.BuildUrn("myInputUrn");

        var metric = MFactory.CreateVariationMetric(outputUrn, inputUrn, 2)
            .Builder.Build<Metric<MetricUrn>>();

        var now = TimeSpan.FromMilliseconds(150);
        var infos = _sut.Create(metric, now);
        Check.That(infos).HasSize(1);

        CheckComputerRuntimeInfoExpected(
            infos.Single(),
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromMinutes(2),
                new VariationComputer(outputUrn, TimeSpan.FromMinutes(2), null, _tsReader, _tsWriter, now)
            )
        );
    }

    [Test]
    public void GivenVariationWithGroups_WhenICreate()
    {
        var outputUrn = MetricUrn.Build("myOutputUrn");
        var inputUrn = Urn.BuildUrn("myInputUrn");

        var metric = MFactory.CreateVariationMetric(outputUrn, inputUrn, 2)
            .Group.Hourly
            .Group.Every(7).Days
            .Builder.Build<Metric<MetricUrn>>();

        var now = TimeSpan.FromMilliseconds(150);
        var infos = _sut.Create(metric, now);
        Check.That(infos).HasSize(3);

        var publicationPeriod = TimeSpan.FromMinutes(2);

        CheckComputerRuntimeInfoExpected(
            infos[0],
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromHours(1),
                new VariationComputer(MetricUrn.Build(outputUrn, "_1Hours"), publicationPeriod, null, _tsReader, _tsWriter, now)
            )
        );

        CheckComputerRuntimeInfoExpected(
            infos[1],
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromDays(7),
                new VariationComputer(MetricUrn.Build(outputUrn, "_7Days"), publicationPeriod, null, _tsReader, _tsWriter, now)
            )
        );

        CheckComputerRuntimeInfoExpected(
            infos[2],
            new ComputerRuntimeInfo("", new[] {inputUrn}, publicationPeriod,
                new VariationComputer(outputUrn, publicationPeriod, null, _tsReader, _tsWriter, now)
            )
        );
    }

    #endregion

    #region Accumulator

    [Test]
    public void GivenSimpleAccumulator_WhenICreate()
    {
        var outputUrn = MetricUrn.Build("myOutputUrn");
        var inputUrn = Urn.BuildUrn("myInputUrn");

        var metric = MFactory.CreateAccumulatorMetric(outputUrn, inputUrn, 2)
            .Builder.Build<Metric<MetricUrn>>();

        var now = TimeSpan.FromMilliseconds(150);
        var infos = _sut.Create(metric, now);
        Check.That(infos).HasSize(1);

        var publicationPeriod = TimeSpan.FromMinutes(2);

        CheckComputerRuntimeInfoExpected(
            infos.Single(),
            new ComputerRuntimeInfo("", new[] {inputUrn}, publicationPeriod,
                new AccumulatorComputer(
                    outputUrn, publicationPeriod, null,
                    _tsReader, _tsWriter, now
                )
            )
        );
    }

    [Test]
    public void GivenAccumulatorWithGroups_WhenICreate()
    {
        var outputUrn = MetricUrn.Build("myOutputUrn");
        var inputUrn = Urn.BuildUrn("myInputUrn");

        var metric = MFactory.CreateAccumulatorMetric(outputUrn, inputUrn, 2)
            .Group.Hourly
            .Group.Every(7).Days
            .Builder.Build<Metric<MetricUrn>>();

        var now = TimeSpan.FromMilliseconds(150);
        var infos = _sut.Create(metric, now);
        Check.That(infos).HasSize(3);

        CheckComputerRuntimeInfoExpected(
            infos[0],
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromHours(1),
                new AccumulatorComputer(
                    MetricUrn.Build(outputUrn, "_1Hours"), TimeSpan.FromHours(1), null,
                    _tsReader, _tsWriter, now
                )
            )
        );

        CheckComputerRuntimeInfoExpected(
            infos[1],
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromDays(7),
                new AccumulatorComputer(
                    MetricUrn.Build(outputUrn, "_7Days"), TimeSpan.FromDays(7), null,
                    _tsReader, _tsWriter, now
                )
            )
        );

        CheckComputerRuntimeInfoExpected(
            infos[2],
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromMinutes(2),
                new AccumulatorComputer(
                    outputUrn, TimeSpan.FromMinutes(2), null,
                    _tsReader, _tsWriter, now
                )
            )
        );
    }

    #endregion

    #region StateMonitoring

    [Test]
    public void GivenStateMonitoringOfSubsystemState_WithWrongInputUrn_WhenICreate_ThenIGetAnError()
    {
        var metric = MFactory.CreateStateMonitoringOfMetric(MetricUrn.Build("myOutputUrn"), PropertyUrn<SubsystemState>.Build("myWrongInputUrn"), 2)
            .Builder.Build<Metric<MetricUrn>>();

        var ex = Check.ThatCode(() => _sut.Create(metric, TimeSpan.FromMilliseconds(150)))
            .Throws<InvalidOperationException>()
            .Value;

        Check.That(ex.Message).Contains("Cannot get state type to build");
    }

    [Test]
    public void GivenStateMonitoringOfSubsystemState_WhenICreate()
    {
        var outputUrn = MetricUrn.Build("myOutputUrn");
        var inputUrn = fake_model.dummy_subsystem.state;

        var metric = MFactory.CreateStateMonitoringOfMetric(outputUrn, inputUrn, 2)
            .Builder.Build<Metric<MetricUrn>>();

        var now = TimeSpan.FromMilliseconds(150);
        var infos = _sut.Create(metric, now);
        Check.That(infos).HasSize(1);

        CheckComputerRuntimeInfoExpected(
            infos.Single(),
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromMinutes(2),
                CreateStateMonitoringComputer(outputUrn, TimeSpan.FromMinutes(2), Array.Empty<MeasureRuntimeInfo>(), now, typeof(Processing.State)))
        );
    }

    [Test]
    public void GivenSimpleStateMonitoring_WhenICreate()
    {
        var outputUrn = MetricUrn.Build("myOutputUrn");
        var inputUrn = PropertyUrn<fake_model.PublicState>.Build("myInputUrn");

        var metric = MFactory.CreateStateMonitoringOfMetric(outputUrn, inputUrn, 2)
            .Builder.Build<Metric<MetricUrn>>();

        var now = TimeSpan.FromMilliseconds(150);
        var infos = _sut.Create(metric, now);
        Check.That(infos).HasSize(1);

        CheckComputerRuntimeInfoExpected(
            infos.Single(),
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromMinutes(2),
                CreateStateMonitoringComputer(outputUrn, TimeSpan.FromMinutes(2), Array.Empty<MeasureRuntimeInfo>(), now))
        );
    }

    [Test]
    public void GivenStateMonitoringWithGroups_WhenICreate()
    {
        var outputUrn = MetricUrn.Build("myOutputUrn");
        var inputUrn = PropertyUrn<fake_model.PublicState>.Build("myInputUrn");

        var metric = MFactory.CreateStateMonitoringOfMetric(outputUrn, inputUrn, 2)
            .Group.Hourly
            .Group.Every(7).Days
            .Builder.Build<Metric<MetricUrn>>();

        var now = TimeSpan.FromMilliseconds(150);
        var infos = _sut.Create(metric, now);
        Check.That(infos).HasSize(3);

        CheckComputerRuntimeInfoExpected(
            infos[0],
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromMinutes(2),
                CreateStateMonitoringComputer(outputUrn, TimeSpan.FromMinutes(2), Array.Empty<MeasureRuntimeInfo>(), now))
        );

        CheckComputerRuntimeInfoExpected(
            infos[1],
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromHours(1),
                CreateStateMonitoringComputer(MetricUrn.Build(outputUrn, "_1Hours"), TimeSpan.FromHours(1),
                    Array.Empty<MeasureRuntimeInfo>(), now))
        );

        CheckComputerRuntimeInfoExpected(
            infos[2],
            new ComputerRuntimeInfo("", new[] {inputUrn}, TimeSpan.FromDays(7),
                CreateStateMonitoringComputer(MetricUrn.Build(outputUrn, "_7Days"), TimeSpan.FromDays(7),
                    Array.Empty<MeasureRuntimeInfo>(), now))
        );
    }

    [Test]
    public void GivenSimpleStateMonitoringWithIncluding_WhenICreate()
    {
        var outputUrn = MetricUrn.Build("myOutputUrn");
        var inputUrn = PropertyUrn<fake_model.PublicState>.Build("myInputUrn");

        const string subVariationName = "subVar";
        var subVarInputUrn = Urn.BuildUrn("mySubVarInputUrn");

        const string subAccumulatorName = "subAcc";
        var subAccInputUrn = Urn.BuildUrn("mySubAccInputUrn");


        var metric = MFactory.CreateStateMonitoringOfMetric(outputUrn, inputUrn, 2)
            .Including(subVariationName).As.VariationOf(subVarInputUrn)
            .Including(subAccumulatorName).As.AccumulatorOf(subAccInputUrn)
            .Builder.Build<Metric<MetricUrn>>();

        var now = TimeSpan.FromMilliseconds(150);
        var infos = _sut.Create(metric, now);
        Check.That(infos).HasSize(1);

        var measureRuntimeInfos = new[]
        {
            new MeasureRuntimeInfo(StateMonitoringMeasureKind.Variation, "s", subVarInputUrn)
        };

        CheckComputerRuntimeInfoExpected(
            infos.Single(),
            new ComputerRuntimeInfo("", new[] {inputUrn, subVarInputUrn, subAccInputUrn}, TimeSpan.FromMinutes(2),
                CreateStateMonitoringComputer(outputUrn, TimeSpan.FromMinutes(2), measureRuntimeInfos, now))
        );
    }

    #endregion

    private static void CheckComputerRuntimeInfoExpected(ComputerRuntimeInfo challenger, ComputerRuntimeInfo expected)
    {
        Check.That(challenger.PublicationPeriod).IsEqualTo(expected.PublicationPeriod);
        Check.That(challenger.TriggerUrns).ContainsExactly(expected.TriggerUrns);
        Check.That(challenger.Computer).IsInstanceOfType(expected.Computer.GetType());
        Check.That(challenger.Computer.Root).IsEqualTo(expected.Computer.Root);
    }

    private StateMonitoringComputer CreateStateMonitoringComputer(MetricUrn outputUrn, TimeSpan publicationPeriod,
        MeasureRuntimeInfo[] measureRuntimeInfos, TimeSpan now, Type? enumType = null)
        => new (outputUrn, enumType ?? typeof(fake_model.PublicState), publicationPeriod, null, measureRuntimeInfos, _tsReader, _tsWriter, now);

    #region Helpers

    public class Processing : SubSystemDefinition<Processing.State>
    {
        public enum State
        {
            A,
            B
        }

        public Processing()
        {
            Subsystem(fake_model.dummy_subsystem)
                .Initial(State.A)
                .Define(State.A)
                .Transitions.When(Condition.LowerThan(fake_model.fake_index, fake_model.fake_index_again))
                .Then(State.B)
                .Define(State.B)
                .Transitions.When(Condition.LowerThan(fake_model.fake_index_again, fake_model.fake_index));
        }
    }

    #endregion
}