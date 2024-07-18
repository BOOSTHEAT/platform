using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Core;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Access;
using ImpliciX.TimeMath.Computers;
using ImpliciX.TimeMath.Computers.StateMonitoring;
using ImpliciX.TimeMath.Tests.Helpers;
using Moq;
using NUnit.Framework;

namespace ImpliciX.TimeMath.Tests.StateMonitoring;

public class StateMonitoringPersistenceTests
{
  [Test]
  public void StoreUnpublishedChunkInDb()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.StateMonitoringOf(t),
      StubAllReadsWithEmptyData
    );

    _sut.StoreUnpublished(TimeSpan.FromSeconds(21),
      new StateChange(Status.StandBy, 1, TimeSpan.FromSeconds(9)), _info.States[Status.StandBy]);
    _sut.StoreUnpublished(TimeSpan.FromSeconds(23),
      new StateChange(Status.Running, 1, TimeSpan.FromSeconds(2)), _info.States[Status.Running]);

    _writer.Verify(x =>
      x.Write("foo:StandBy:occurrence$unpublished", TimeSpan.FromSeconds(21), 1)
    );
    _writer.Verify(x =>
      x.Write("foo:StandBy:duration$unpublished", TimeSpan.FromSeconds(21), 9)
    );
    _writer.Verify(x =>
      x.Write("foo:Running:occurrence$unpublished", TimeSpan.FromSeconds(23), 1)
    );
    _writer.Verify(x =>
      x.Write("foo:Running:duration$unpublished", TimeSpan.FromSeconds(23), 2)
    );
  }
  
  [Test]
  public void CanReadNoUnpublishedData()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.StateMonitoringOf(t),
      StubAllReadsWithEmptyData
    );

    var unpublished = _sut.ReadUnpublished();
    
    Assert.True(unpublished.IsNone);
  }
  
  [Test]
  public void CanReadUnpublishedData()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.StateMonitoringOf(t),
      () =>
      {
        StubReadLastWith("foo:StandBy:occurrence$unpublished", 2, TimeSpan.FromSeconds(3628));
        StubReadLastWith("foo:StandBy:duration$unpublished", 14, TimeSpan.FromSeconds(3628));
        StubReadLastWith("foo:Running:occurrence$unpublished", 1, TimeSpan.FromSeconds(3623));
        StubReadLastWith("foo:Running:duration$unpublished", 3, TimeSpan.FromSeconds(3623));
        
        StubReadAllWithEmptyData("foo:StandBy:occurrence$published");
        StubReadAllWithEmptyData("foo:StandBy:duration$published");
        StubReadAllWithEmptyData("foo:StandBy:bounds$published");
        StubReadAllWithEmptyData("foo:Running:occurrence$published");
        StubReadAllWithEmptyData("foo:Running:duration$published");
        StubReadAllWithEmptyData("foo:Running:bounds$published");
      }
    );

    var unpublished = _sut.ReadUnpublished();
    
    Assert.True(unpublished.IsSome);
    var dated = unpublished.GetValue();
    Assert.That(dated.Start, Is.EqualTo(TimeSpan.FromSeconds(3600)));
    Assert.That(dated.End, Is.EqualTo(TimeSpan.FromSeconds(3630)));
    var sd = dated.Value;
    Assert.That(sd[Status.StandBy].Occurrence, Is.EqualTo(2));
    Assert.That(sd[Status.StandBy].Duration, Is.EqualTo(TimeSpan.FromSeconds(14)));
    Assert.That(sd[Status.Running].Occurrence, Is.EqualTo(1));
    Assert.That(sd[Status.Running].Duration, Is.EqualTo(TimeSpan.FromSeconds(3)));
  }

  [Test]
  public void StorePublishedChunksInDb()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(t),
      StubAllReadsWithEmptyData
    );

    _sut.StorePublished(Chunk(StateChange(Status.StandBy,1,14)
                               +StateDigest.Neutral
                               +StateChange(Status.Running,1,9),
      0, 30));
    _sut.StorePublished(Chunk(StateDigest.Neutral
                              +StateChange(Status.Running,1,30),
      30, 60));
    _sut.StorePublished(Chunk(StateDigest.Neutral
                              +StateChange(Status.Running,1,30),
      60, 90));
    
    _writer.Verify(x =>
      x.Write("foo:StandBy:occurrence$published", TimeSpan.FromSeconds(30), 1)
    );
    _writer.Verify(x =>
      x.Write("foo:StandBy:duration$published", TimeSpan.FromSeconds(30), 14)
    );
    _writer.Verify(x =>
      x.Write("foo:StandBy:bounds$published", TimeSpan.FromSeconds(30), 1)
    );
    _writer.Verify(x =>
      x.Write("foo:Running:occurrence$published", TimeSpan.FromSeconds(30), 1)
    );
    _writer.Verify(x =>
      x.Write("foo:Running:duration$published", TimeSpan.FromSeconds(30), 9)
    );
    _writer.Verify(x =>
      x.Write("foo:Running:bounds$published", TimeSpan.FromSeconds(30), 2)
    );
    _writer.Verify(x =>
      x.Write("foo:Running:occurrence$published", TimeSpan.FromSeconds(60), 1)
    );
    _writer.Verify(x =>
      x.Write("foo:Running:duration$published", TimeSpan.FromSeconds(60), 30)
    );
    _writer.Verify(x =>
      x.Write("foo:Running:bounds$published", TimeSpan.FromSeconds(60), 3)
    );
    _writer.Verify(x =>
      x.Write("foo:Running:occurrence$published", TimeSpan.FromSeconds(90), 1)
    );
    _writer.Verify(x =>
      x.Write("foo:Running:duration$published", TimeSpan.FromSeconds(90), 30)
    );
    _writer.Verify(x =>
      x.Write("foo:Running:bounds$published", TimeSpan.FromSeconds(90), 3)
    );
  }

  [Test]
  public void CanReadPublishedData()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(t),
      () =>
      {
        StubReadLastWithEmptyData("foo:StandBy:occurrence$unpublished");
        StubReadLastWithEmptyData("foo:StandBy:duration$unpublished");
        StubReadLastWithEmptyData("foo:Running:occurrence$unpublished");
        StubReadLastWithEmptyData("foo:Running:duration$unpublished");

        StubReadAllWith("foo:StandBy:occurrence$published", 
          (TimeSpan.FromSeconds(3630),2),
          (TimeSpan.FromSeconds(3660),1));
        StubReadAllWith("foo:StandBy:duration$published", 
          (TimeSpan.FromSeconds(3630),14),
          (TimeSpan.FromSeconds(3660),5));
        StubReadAllWith("foo:StandBy:bounds$published", 
          (TimeSpan.FromSeconds(3630),2),
          (TimeSpan.FromSeconds(3660),1));
        StubReadAllWith("foo:Running:occurrence$published",
          (TimeSpan.FromSeconds(3630),2),
          (TimeSpan.FromSeconds(3660),1),
          (TimeSpan.FromSeconds(3690),1));
        StubReadAllWith("foo:Running:duration$published",
          (TimeSpan.FromSeconds(3630),9),
          (TimeSpan.FromSeconds(3660),25),
          (TimeSpan.FromSeconds(3690),30));
        StubReadAllWith("foo:Running:bounds$published",
          (TimeSpan.FromSeconds(3630),1),
          (TimeSpan.FromSeconds(3660),2),
          (TimeSpan.FromSeconds(3690),3));
      }
    );

    var published = _sut.ReadPublished().ToArray();
    
    Assert.That(published.Length, Is.EqualTo(3));

    var sd0 = published.ElementAt(0);
    Assert.That(sd0.Start, Is.EqualTo(TimeSpan.FromSeconds(3600)));
    Assert.That(sd0.End, Is.EqualTo(TimeSpan.FromSeconds(3630)));
    Assert.That(sd0.Value[Status.StandBy].Occurrence, Is.EqualTo(2));
    Assert.That(sd0.Value[Status.StandBy].Duration, Is.EqualTo(TimeSpan.FromSeconds(14)));
    Assert.That(sd0.Value[Status.Running].Occurrence, Is.EqualTo(2));
    Assert.That(sd0.Value[Status.Running].Duration, Is.EqualTo(TimeSpan.FromSeconds(9)));
    Assert.True(sd0.Value.StartsWith(Status.Running), $"{nameof(sd0)} start");
    Assert.True(sd0.Value.EndsWith(Status.StandBy), $"{nameof(sd0)} end");

    var sd1 = published.ElementAt(1);
    Assert.That(sd1.Start, Is.EqualTo(TimeSpan.FromSeconds(3630)));
    Assert.That(sd1.End, Is.EqualTo(TimeSpan.FromSeconds(3660)));
    Assert.That(sd1.Value[Status.StandBy].Occurrence, Is.EqualTo(1));
    Assert.That(sd1.Value[Status.StandBy].Duration, Is.EqualTo(TimeSpan.FromSeconds(5)));
    Assert.That(sd1.Value[Status.Running].Occurrence, Is.EqualTo(1));
    Assert.That(sd1.Value[Status.Running].Duration, Is.EqualTo(TimeSpan.FromSeconds(25)));
    Assert.True(sd1.Value.StartsWith(Status.StandBy), $"{nameof(sd1)} start");
    Assert.True(sd1.Value.EndsWith(Status.Running), $"{nameof(sd1)} end");

    var sd2 = published.ElementAt(2);
    Assert.That(sd2.Start, Is.EqualTo(TimeSpan.FromSeconds(3660)));
    Assert.That(sd2.End, Is.EqualTo(TimeSpan.FromSeconds(3690)));
    Assert.That(sd2.Value[Status.StandBy].Occurrence, Is.EqualTo(0));
    Assert.That(sd2.Value[Status.StandBy].Duration, Is.EqualTo(TimeSpan.Zero));
    Assert.That(sd2.Value[Status.Running].Occurrence, Is.EqualTo(1));
    Assert.That(sd2.Value[Status.Running].Duration, Is.EqualTo(TimeSpan.FromSeconds(30)));
    Assert.True(sd2.Value.StartsWith(Status.Running), $"{nameof(sd2)} start");
    Assert.True(sd2.Value.EndsWith(Status.Running), $"{nameof(sd2)} end");
  }
  

  public enum Status
  {
    StandBy,
    Running
  }

  private StateChange StateChange(Enum state, int occurrence, int seconds) =>
    new (state, occurrence, TimeSpan.FromSeconds(seconds));
  private Dated<StateDigest> Chunk(StateDigest sd, int startSeconds, int endSeconds) =>
    new (sd, TimeSpan.FromSeconds(startSeconds), TimeSpan.FromSeconds(endSeconds));

  private void SetupComputer<T>(
    Func<NamedMetric, PropertyUrn<T>, IMetricDefinition> define,
    Action setups
  )
  {
    var bar = ExpressionsFactory.CreateUrn<T>("bar");
    var foo = MetricUrn.Build("foo");
    _info = MetricInfoFactory.CreateStateMonitoringInfo(
      define(Metrics.Metric(foo), bar).Builder.Build<Metric<MetricUrn>>(),
      new Dictionary<Urn, Type>()
    );
    _writer = new Mock<IWriteTimeSeries>();
    _reader = new Mock<IReadTimeSeries>();
    setups();
    _sut = new StateMonitoringPersistence(_info,
      new TimeBasedTimeMathWriter(_writer.Object),
      new TimeBasedTimeMathReader(_reader.Object));
  }

  private void StubAllReadsWithEmptyData()
  {
    _reader
      .Setup(x => x.ReadLast(It.IsAny<string>(), It.IsAny<long?>()))
      .Returns(Option<DataModelValue<float>>.None);
    _reader
      .Setup(x => x.ReadAll(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<long?>()))
      .Returns(Option<DataModelValue<float>[]>.None);
  }

  private void StubReadLastWithEmptyData(Urn urn)
  {
    _reader
      .Setup(x => x.ReadLast(urn.Value, It.IsAny<long?>()))
      .Returns(Option<DataModelValue<float>>.None);
  }

  private void StubReadAllWithEmptyData(Urn urn)
  {
    _reader
      .Setup(x => x.ReadAll(urn.Value, It.IsAny<long?>(), It.IsAny<long?>()))
      .Returns(Option<DataModelValue<float>[]>.None);
  }

  private void StubReadLastWith(Urn urn, float value, TimeSpan at)
  {
    _reader
      .Setup(x => x.ReadLast(urn.Value, null))
      .Returns(Option<DataModelValue<float>>.Some(new DataModelValue<float>(urn,value,at)));
  }

  private void StubReadAllWith(Urn urn, params (TimeSpan at,float value)[] data)
  {
    _reader
      .Setup(x => x.ReadAll(urn.Value, null, null))
      .Returns(Option<DataModelValue<float>[]>
        .Some(data.Select(x => new DataModelValue<float>(urn,x.value,x.at)).ToArray()));
  }

  private IStateMonitoringPersistence _sut;
  private StateMonitoringInfo _info;
  private Mock<IWriteTimeSeries> _writer;
  private Mock<IReadTimeSeries> _reader;

  [TestCase(33, 5, 30)]
  [TestCase(30, 5, 30)]
  public void PeriodStartsTest(long time, long period, long expectedStart)
  {
    Assert.That(
      StateMonitoringPersistence.PeriodStartJustBefore(
        TimeSpan.FromSeconds(time), TimeSpan.FromSeconds(period)
        ),
      Is.EqualTo(TimeSpan.FromSeconds(expectedStart))
      );
  }
}