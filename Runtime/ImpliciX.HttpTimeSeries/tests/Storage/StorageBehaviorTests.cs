using ImpliciX.HttpTimeSeries.Storage;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.TestsCommon;
using Moq;
using NFluent;
using PHD = ImpliciX.TestsCommon.PropertyDataHelper;
using static ImpliciX.HttpTimeSeries.Tests.Helpers.HttpTimeSeriesTestHelpers;

namespace ImpliciX.HttpTimeSeries.Tests.Storage;

public class StorageBehaviorTests
{
  private readonly TimeHelper T = TimeHelper.Minutes();
  private Mock<IMetricsDbRepository> _db;

  [SetUp]
  public void Setup()
  {
    _db = new Mock<IMetricsDbRepository>();
  }

  [Test]
  public void WhenPropertyChangedFromMetricKnown_ThenIStoreIt()
  {
    Urn rootUrn = "foo:bar:fizz";
    DataModelValue<MetricValue>[] series =
    {
      PHD.CreateMetricValueProperty(rootUrn, 2.4f, 0, 2)
    };

    var pc = PropertiesChanged.Create(rootUrn, series, T._2);
    var events = CreateSut(("foo:bar:fizz", 10)).StoreSeries(pc);
    Check.That(events).IsEmpty();

    _db.Verify(
      x => x.WriteMany(
        rootUrn,
        series
      ), Times.Once
    );
  }

  [Test]
  public void WhenFloatPropertyChangedFromSeriesKnown_ThenITransformIntoMetricAndStoreIt()
  {
    var at = TimeSpan.FromSeconds(10000);
    Urn rootUrn = "foo:bar:fizz";

    var pc = PropertiesChanged.Create(rootUrn, new IDataModelValue[]
    {
      Property<Temperature>.Create(
        PropertyUrn<Temperature>.Build(rootUrn),
        Temperature.Create(280f),
        at
      )
    }, T._2);
    var events = CreateSut(("foo:bar:fizz", 10)).StoreSeries(pc);
    Check.That(events).IsEmpty();

    _db.Verify(
      x => x.WriteMany(
        rootUrn,
        new DataModelValue<MetricValue>[]
        {
          Property<MetricValue>.Create(
            PropertyUrn<MetricValue>.Build(rootUrn),
            new MetricValue(280f, at, at),
            at
          )
        }
      ), Times.Once
    );
  }

  [Test]
  public void WhenEnumPropertyChangedFromSeriesKnown_ThenITransformIntoMetricAndStoreIt()
  {
    var at = TimeSpan.FromSeconds(10000);
    Urn rootUrn = "foo:bar:fizz";

    var pc = PropertiesChanged.Create(rootUrn, new IDataModelValue[]
    {
      Property<Fault>.Create(
        PropertyUrn<Fault>.Build(rootUrn),
        Fault.Faulted,
        at
      )
    }, T._2);
    var events = CreateSut(("foo:bar:fizz", 10)).StoreSeries(pc);
    Check.That(events).IsEmpty();

    _db.Verify(
      x => x.WriteMany(
        rootUrn,
        new DataModelValue<MetricValue>[]
        {
          Property<MetricValue>.Create(
            PropertyUrn<MetricValue>.Build(rootUrn),
            new MetricValue(1f, at, at),
            at
          )
        }
      ), Times.Once
    );
  }

  [Test]
  public void WhenReadFromKnownSeries_ThenOldStoreShouldBeRemoved()
  {
    Urn rootUrn = "foo:bar:fizz";
    var values = CreateSut(("foo:bar:fizz", 10)).ReadDbSeriesValues(rootUrn);
    Check.That(values).IsEmpty();

    _db.Verify(
      x => x.ApplyRetentionPolicy(),
      Times.Once
    );
  }

  private DataService CreateSut(params (string Value, int Retention)[] def)
    => new(CreateFakeSeries(def), _ => _db.Object);
}