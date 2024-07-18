using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Core;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Computers;
using ImpliciX.TimeMath.Computers.StateMonitoring;
using ImpliciX.TimeMath.Tests.Helpers;
using Moq;
using NUnit.Framework;

namespace ImpliciX.TimeMath.Tests.StateMonitoring;

public class StateMonitoringComputerTests
{
  [Test]
  public void CurrentChunkIsStoredInDb()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.StateMonitoringOf(t),
      FinalizeSutWithPersistenceEmptyData
    );

    _sut.Update(Happen(Status.StandBy, 12));
    _sut.Update(Happen(Status.Running, 21));
    
    _persistence.Verify(x =>
      x.StoreUnpublished(TimeSpan.FromSeconds(21),
        StateChange(Status.StandBy, 1, 21-12),
        _info.States[Status.StandBy]
        )
      );
  }

  [Test]
  public void UpdatedCurrentChunkIsStoredInDb()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.StateMonitoringOf(t),
      FinalizeSutWithPersistenceEmptyData
    );

    _sut.Update(Happen(Status.StandBy, 12));
    _sut.Update(Happen(Status.Running, 21));
    _sut.Update(Happen(Status.StandBy, 23));

    _persistence.Verify(x =>
      x.StoreUnpublished(TimeSpan.FromSeconds(21), StateChange(Status.StandBy, 1, 21-12),
        _info.States[Status.StandBy]));
    _persistence.Verify(x =>
      x.StoreUnpublished(TimeSpan.FromSeconds(23), StateChange(Status.Running, 1, 23-21),
        _info.States[Status.Running]));
  }

  [Test]
  public void UpdatedTwiceCurrentChunkIsStoredInDb()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.StateMonitoringOf(t),
      FinalizeSutWithPersistenceEmptyData
    );

    _sut.Update(Happen(Status.StandBy, 12));
    _sut.Update(Happen(Status.Running, 21));
    _sut.Update(Happen(Status.StandBy, 23));
    _sut.Update(Happen(Status.Running, 28));

    _persistence.Verify(x =>
      x.StoreUnpublished(TimeSpan.FromSeconds(21), StateChange(Status.StandBy, 1, 21-12),
        _info.States[Status.StandBy]));
    _persistence.Verify(x =>
      x.StoreUnpublished(TimeSpan.FromSeconds(23), StateChange(Status.Running, 1, 23-21),
        _info.States[Status.Running]));
    _persistence.Verify(x =>
      x.StoreUnpublished(TimeSpan.FromSeconds(28), StateChange(Status.StandBy, 2, 28 - 23 + 21 - 12),
        _info.States[Status.StandBy]));
  }

  [Test]
  public void WindowIsStoredInDb()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(t),
      FinalizeSutWithPersistenceEmptyData
    );

    _sut.Update(Happen(Status.StandBy, 12));
    _sut.Update(Happen(Status.Running, 21));
    _sut.Publish(TimeSpan.FromSeconds(30));
    _sut.Publish(TimeSpan.FromSeconds(60));
    _sut.Publish(TimeSpan.FromSeconds(90));

    _persistence.Verify(x =>
      x.StorePublished(
        Chunk( StateDigest.Hole + StateChange(Status.StandBy,1,21-12)
              +Empty+StateChange(Status.Running,1,30-21),
          0, 30) )
    );
    _persistence.Verify(x =>
      x.StorePublished(
        Chunk(Empty+StateChange(Status.Running,1,30),
          30, 60) )
    );
    _persistence.Verify(x =>
      x.StorePublished(
        Chunk(Empty+StateChange(Status.Running,1,30),
          60, 90) )
    );
  }

  [Test]
  public void InitializeWindowWithStoredObsoleteData()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(t),
      () =>
      {
        StubPublishedWith(3600,
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30),
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30),
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30),
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30)
        );
        StubUnpublishedWith(3600, StateDigest.Neutral + StateChange(Status.StandBy, 1, 15));
      }
    );

    var content =
      StateMonitoringComputer.CreateWindowContentFromStoredData(TimeSpan.FromSeconds(6100), _info, _persistence.Object).ToArray();
    
    Assert.That(content, Is.EqualTo(new Dated<StateDigest>[]
    {
      Chunk(StateDigest.Hole, 6000, 6030),
      Chunk(StateDigest.Hole, 6030, 6060),
      Chunk(StateDigest.Hole, 6060, 6090),
    }));
  }

  [Test]
  public void InitializeWindowWithStoredOldButStillUsefulData()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(t),
      () =>
      {
        StubPublishedWith(3600,
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30),
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30),
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30),
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30)
        );
        StubUnpublishedWith(3600, StateDigest.Neutral + StateChange(Status.StandBy, 1, 15));
      }
    );

    var content =
      StateMonitoringComputer.CreateWindowContentFromStoredData(TimeSpan.FromSeconds(3800), _info, _persistence.Object).ToArray();
    
    Assert.That(content, Is.EqualTo(new Dated<StateDigest>[]
    {
      Chunk(StateDigest.Neutral + StateChange(Status.StandBy, 1, 30), 3690, 3720),
      Chunk(StateChange(Status.StandBy, 1, 15)+StateDigest.Hole, 3720, 3750),
      Chunk(StateDigest.Hole, 3750, 3780),
    }));
  }

  [Test]
  public void InitializeWindowWithStoredRecentData()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(t),
      () =>
      {
        StubPublishedWith(3600,
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30),
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30),
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30),
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30)
        );
        StubUnpublishedWith(3600, StateDigest.Neutral + StateChange(Status.StandBy, 1, 15));
      }
    );

    var content =
      StateMonitoringComputer.CreateWindowContentFromStoredData(TimeSpan.FromSeconds(3760), _info, _persistence.Object).ToArray();
    
    Assert.That(content, Is.EqualTo(new Dated<StateDigest>[]
    {
      Chunk(StateDigest.Neutral + StateChange(Status.StandBy, 1, 30), 3660, 3690),
      Chunk(StateDigest.Neutral + StateChange(Status.StandBy, 1, 30), 3690, 3720),
      Chunk(StateChange(Status.StandBy, 1, 15)+StateDigest.Hole, 3720, 3750),
    }));
  }

  [Test]
  public void InitializeWindowWithStoredRecentDataAndMissingOldData()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(t),
      () =>
      {
        StubPublishedWith(3690,
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30)
        );
        StubUnpublishedWith(3600, StateDigest.Neutral + StateChange(Status.StandBy, 1, 15));
      }
    );

    var content =
      StateMonitoringComputer.CreateWindowContentFromStoredData(TimeSpan.FromSeconds(3760), _info, _persistence.Object).ToArray();
    
    Assert.That(content, Is.EqualTo(new Dated<StateDigest>[]
    {
      Chunk(StateDigest.Hole, 3660, 3690),
      Chunk(StateDigest.Hole + StateChange(Status.StandBy, 1, 30), 3690, 3720),
      Chunk(StateChange(Status.StandBy, 1, 15)+StateDigest.Hole, 3720, 3750),
    }));
  }

  [Test]
  public void InitializeWindowWithStoredCurrentData()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(t),
      () =>
      {
        StubPublishedWith(3600,
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30),
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30),
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30),
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30)
        );
        StubUnpublishedWith(3600, StateDigest.Neutral + StateChange(Status.StandBy, 1, 15));
      }
    );

    var content =
      StateMonitoringComputer.CreateWindowContentFromStoredData(TimeSpan.FromSeconds(3740), _info, _persistence.Object).ToArray();
    
    Assert.That(content, Is.EqualTo(new Dated<StateDigest>[]
    {
      Chunk(StateDigest.Neutral + StateChange(Status.StandBy, 1, 30), 3630, 3660),
      Chunk(StateDigest.Neutral + StateChange(Status.StandBy, 1, 30), 3660, 3690),
      Chunk(StateDigest.Neutral + StateChange(Status.StandBy, 1, 30), 3690, 3720),
    }));
  }

  [Test]
  public void InitializeWindowWithStoredCurrentDataAndMissingOldData()
  {
    SetupComputer<Status>(
      (m, t) => m.Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(t),
      () =>
      {
        StubPublishedWith(3660,
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30),
          StateDigest.Neutral + StateChange(Status.StandBy, 1, 30)
        );
        StubUnpublishedWith(3600, StateDigest.Neutral + StateChange(Status.StandBy, 1, 15));
      }
    );

    var content =
      StateMonitoringComputer.CreateWindowContentFromStoredData(TimeSpan.FromSeconds(3740), _info, _persistence.Object).ToArray();
    
    Assert.That(content, Is.EqualTo(new Dated<StateDigest>[]
    {
      Chunk(StateDigest.Hole, 3630, 3660),
      Chunk(StateDigest.Hole + StateChange(Status.StandBy, 1, 30), 3660, 3690),
      Chunk(StateDigest.Neutral + StateChange(Status.StandBy, 1, 30), 3690, 3720),
    }));
  }

  public enum Status
  {
    StandBy,
    Running
  }

  private IDataModelValue Happen<T>(T state, int seconds) =>
    new DataModelValue<T>(ExpressionsFactory.CreateUrn<T>("bar"), state, TimeSpan.FromSeconds(seconds));
  
  private StateChange StateChange(Enum state, int occurrence, int seconds) =>
    new (state, occurrence, TimeSpan.FromSeconds(seconds));
  private StateDigest Empty => StateDigest.Neutral;
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
    _persistence = new Mock<IStateMonitoringPersistence>();
    _windowSize = _info.WindowRetention.Match(
      () => 1,
      windowRetention => (int)(windowRetention.TotalMilliseconds / _info.PublicationPeriod.TotalMilliseconds)
    );
    _persistence.Setup(x => x.WindowSize).Returns(() => _windowSize);
    _persistence.Setup(x => x.PeriodStartJustBefore(It.IsAny<TimeSpan>()))
      .Returns<TimeSpan>(ts => new TimeSpan(_info.PublicationPeriod.Ticks * (ts.Ticks / _info.PublicationPeriod.Ticks)));
    setups();
  }
  
  private void FinalizeSutWithPersistenceEmptyData()
  {
    _persistence.Setup(x => x.ReadUnpublished()).Returns(Option<Dated<StateDigest>>.None);
    _persistence.Setup(x => x.ReadPublished()).Returns(Enumerable.Empty<Dated<StateDigest>>);
    _sut = new StateMonitoringComputer(_info, _persistence.Object);
    _sut.IsPublishTimePassed(TimeSpan.Zero, TimeSpan.Zero);
  }
  
  private void StubPublishedWith(int startAt, params StateDigest[] content)
  {
    var step = (int)_info.PublicationPeriod.TotalSeconds;
    _persistence.Setup(x => x.ReadPublished()).Returns(
      content.Select((sd,i) => Chunk(sd, startAt+i*step, startAt+(i+1)*step)).ToArray()
    );
  }
  
  private void StubUnpublishedWith(int startAt, StateDigest content)
  {
    var step = (int)_info.PublicationPeriod.TotalSeconds;
    var startAfterPublished = startAt + step * _windowSize;
    _persistence.Setup(x => x.ReadUnpublished()).Returns(
      Option<Dated<StateDigest>>.Some(
        Chunk(content, startAfterPublished, startAfterPublished+ (int)content.TotalDuration.TotalSeconds)
      )
    );
  }

  private StateMonitoringComputer _sut;
  private StateMonitoringInfo _info;
  private Mock<IStateMonitoringPersistence> _persistence;
  private int _windowSize;
}