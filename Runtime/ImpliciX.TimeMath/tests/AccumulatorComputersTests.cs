using System;
using ImpliciX.Language.Model;
using ImpliciX.TestsCommon;
using ImpliciX.TimeMath.Access;
using ImpliciX.TimeMath.Computers;
using ImpliciX.TimeMath.Tests.Helpers;
using Moq;
using NFluent;
using NUnit.Framework;
using PDH = ImpliciX.TestsCommon.PropertyDataHelper;
using TestsModel = ImpliciX.TimeMath.Tests.Helpers.fake_analytics_model;
using static ImpliciX.TimeMath.Tests.TimeMathFactoryHelper;

namespace ImpliciX.TimeMath.Tests;

[TestFixture]
public class AccumulatorComputersTests
{
  [SetUp]
  public void Init()
  {
    _timeMathWriter = Mock.Of<ITimeMathWriter>();
    var tsReaderMock = new Mock<ITimeMathReader>
    {
      DefaultValueProvider = new OptionDefaultValueProvider()
    };
    _timeMathReader = tsReaderMock.Object;

    ReaderReturnLastUpdateAt(
        tsReaderMock,
        TemperatureResult,
        AccumulatedUrn
      )
      ;
    ReaderReturnLastUpdateAt(
        tsReaderMock,
        TemperatureResult,
        AccumulatedUrn
      )
      ;
    tsReaderMock
      .Setup(reader => reader.ReadEndAt(TemperatureResult))
      .Returns(PublishAt)
      ;
    tsReaderMock
      .Setup(reader => reader.ReadStartAt(It.IsAny<string>()))
      .Returns(T._0)
      ;
  }

  private static readonly TimeHelper T = TimeHelper.Minutes();
  private ITimeMathWriter _timeMathWriter;

  private ITimeMathReader _timeMathReader;

  private static readonly TimeSpan PublishAt = T._5;

  [Test]
  public void should_not_publish_accumulator_computer_when_value_is_empty()
  {
    var computer = CreateSut();
    var resultingEvents = computer.Publish(PublishAt);
    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsSome).As("resultingEvent exist").IsTrue();
    Check.That(resultingEvents.GetValue()).IsNotNull();
    Check.That(resultingEvents.GetValue()).HasSize(2);
  }

  [Test]
  public void should_publish_value_received_before_publish_time()
  {
    //given
    var sut = CreateSut();

    var readerMock = Mock.Get(_timeMathReader);
    var publishAt = T._6;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        publishAt,
        242
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        CountUrn,
        publishAt,
        2
      )
      ;
    //when
    var resultingEvents = sut.Publish(publishAt);

    //then
    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureUrnAccumulated,
        242,
        0,
        5
      ),
      PDH.CreateMetricValueProperty(
        TemperatureUrnCount,
        2,
        0,
        5
      )
    };

    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsSome).As("resultingEvent exist").IsTrue();
    Check.That(resultingEvents.GetValue()).IsNotNull();
    Check.That(resultingEvents.GetValue()).HasSize(2);
    Check.That(resultingEvents.GetValue()).IsEqualTo(expected);
  }

  [Test]
  public void should_publish_zero_values_more_recent_than_publish_time()
  {
    //given
    var publishAt = T._6;
    var sut = CreateSut();
    var updateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(242f),
      publishAt
    );

    var resultingEvents = sut.Publish(publishAt);

    //when
    sut.Update(updateValue);

    //then


    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsSome).As("resultingEvent exist").IsTrue();
    Check.That(resultingEvents.GetValue()).IsNotNull();
    Check.That(resultingEvents.GetValue()).HasSize(2);
  }

  [Test]
  public void should_keep_values_more_recent_after_publish()
  {
    //given
    var updateAt = T._9;
    var sut = CreateSut();
    var updateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(242f),
      updateAt
    );

    sut.Update(updateValue);

    var resultingEvents = sut.Publish(PublishAt);
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnStartAt(
        readerMock,
        TemperatureResult,
        PublishAt
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        CountUrn,
        PublishAt,
        2
      )
      ;
    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsSome).As("resultingEvent exist").IsTrue();
    Check.That(resultingEvents.GetValue()).IsNotNull();
    Check.That(resultingEvents.GetValue()).HasSize(2);

    readerMock
      .Setup(reader => reader.ReadEndAt(TemperatureResult))
      .Returns(updateAt)
      ;
    /*
    ReaderReturnFloatAt(
      tsReaderMock,
      EndTemperatureUrn,
      updateAt,
      0
    );
    */
    sut.Update(updateAt);

    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureUrnAccumulated,
        242,
        5,
        9
      ),
      PDH.CreateMetricValueProperty(
        TemperatureUrnCount,
        2,
        5,
        9
      )
    };
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        updateAt,
        242
      )
      ;

    //then
    resultingEvents = sut.Publish(updateAt).GetValue();

    //then
    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsSome).As("resultingEvent exist").IsTrue();
    Check.That(resultingEvents.GetValue()).IsNotNull();
    Check.That(resultingEvents.GetValue()).HasSize(2);
    Check.That(resultingEvents.GetValue()).IsEqualTo(expected);
  }

  [Test]
  public void should_not_keep_values_in_storage_after_publish()
  {
    //given
    var sut = CreateSut();
    var firstPublish = T._6;
    var secondPublish = T._9;
    var firstUpdateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(42f),
      firstPublish
    );

    var secondUpdateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(242f),
      secondPublish
    );

    sut.Update(firstUpdateValue);
    sut.Update(firstPublish);

    var readerMock = Mock.Get(_timeMathReader);

    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        firstPublish,
        42
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        CountUrn,
        firstPublish,
        2
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        firstPublish
      )
      ;

    //when
    var p1 = sut.Publish(firstPublish).GetValueOrDefault(Array.Empty<Property<MetricValue>>());

    ReaderReturnStartAt(
        readerMock,
        TemperatureResult,
        firstPublish
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        secondPublish,
        242
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        CountUrn,
        secondPublish,
        3
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        secondPublish
      )
      ;

    sut.Update(secondUpdateValue);
    sut.Update(secondPublish);
    var p2 = sut.Publish(secondPublish).GetValueOrDefault(Array.Empty<Property<MetricValue>>());

    //then

    readerMock.Verify(
      reader =>
        reader.ReadLastUpdate(
          TemperatureResult,
          AccumulatedUrn
        )
      , Times.AtLeastOnce
    );
    readerMock.Verify(
      reader =>
        reader.ReadLastUpdate(
          TemperatureResult,
          AccumulatedUrn
        )
      , Times.AtLeastOnce
    );
    Check.That(p1.Length).As("nb values").Equals(2);
    var expectedP1 = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureUrnAccumulated,
        42,
        0,
        6
      ),
      PDH.CreateMetricValueProperty(
        TemperatureUrnCount,
        2,
        0,
        6
      )
    };
    Check.That(p1).ContainsExactly(expectedP1);
    var expectedP2 = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureUrnAccumulated,
        242,
        6,
        9
      ),
      PDH.CreateMetricValueProperty(
        TemperatureUrnCount,
        3,
        6,
        9
      )
    };
    Check.That(p2).ContainsExactly(expectedP2);

    // _tsReader.ReadAll(fake_analytics_model.temperature).CheckIsSomeAnd((it)=>Check.That(it).HasSize(0));
  }

  [Test]
  public void should_write_update_time_received_to_time_series_db()
  {
    //given
    var updateAt = T._1;
    var sut = CreateSut();
    //            factory =>factory.Create(ModuleName, It.IsAny<ApplicationRuntimeDefinition>() )
    var writerMock = Mock.Get(_timeMathWriter);
    writerMock.Setup(
      writer => writer.UpdateEndAt(
        TemperatureResult,
        updateAt
      )
    );
    //when
    sut.Update(updateAt);

    //then
    writerMock.Verify(
      writer =>
        writer.UpdateEndAt(
          TemperatureResult,
          updateAt
        )
      , Times.AtLeastOnce
    );
  }

  [Test]
  public void should_write_value_received_to_time_series_db()
  {
    //given
    var sut = CreateSut();
    var writerMock = Mock.Get(_timeMathWriter);
    var updateAt = T._4;
    writerMock.Setup(
      writer => writer.UpdateLastMetric(
        TemperatureResult,
        AccumulatedUrn,
        updateAt,
        It.IsAny<float>()
      )
    );
    var updateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(242f),
      updateAt
    );

    //when
    sut.Update(updateValue);

    //then
    writerMock.Verify(
      writer =>
        writer.UpdateLastMetric(
          TemperatureResult,
          AccumulatedUrn,
          updateAt,
          242
        )
      , Times.Exactly(1)
    );
  }

  [Test]
  public void should_read_previous_value_from_time_series_db_on_value_update()
  {
    //given
    var sut = CreateSut();
    var readerMock = Mock.Get(_timeMathReader);
    var initialUpdateAt = T._4;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        PublishAt,
        242
      )
      ;
    var writerMock = Mock.Get(_timeMathWriter);
    writerMock.Setup(
        writer => writer.UpdateLastMetric(
          TemperatureResult,
          AccumulatedUrn,
          PublishAt,
          It.IsAny<float>()
        )
      )
      .Callback(
        (
            string rootUrn,
            string suffix,
            TimeSpan time,
            float f
          ) =>
          TestContext.Progress.WriteLine($"Incoming call: {rootUrn} {suffix} {time} {f}")
      )
      ;
    var updateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(242f),
      PublishAt
    );

    //when
    sut.Update(updateValue);

    //then
    readerMock.Verify(
      reader =>
        reader.ReadLastUpdate(
          TemperatureResult,
          AccumulatedUrn
        )
      , Times.Once
    );
    readerMock.Verify(
      reader =>
        reader.ReadLastUpdate(
          TemperatureResult,
          CountUrn
        )
      , Times.Once
    );
    writerMock.Verify(
      writer =>
        writer.UpdateLastMetric(
          TemperatureResult,
          AccumulatedUrn,
          PublishAt,
          484
        )
      , Times.Exactly(1)
    );
  }

  [Test]
  public void should_write_cumulated_value_on_value_update_to_time_series_db()
  {
    //given
    var initialUpdateAt = T._4;
    var sut = CreateSut();
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        PublishAt,
        242
      )
      ;
    var writerMock = Mock.Get(_timeMathWriter);
    writerMock.Setup(
      writer => writer.UpdateLastMetric(
        TemperatureResult,
        AccumulatedUrn,
        initialUpdateAt,
        It.IsAny<float>()
      )
    );
    var updateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(242f),
      PublishAt
    );

    //when
    sut.Update(updateValue);

    //then
    writerMock.Verify(
      writer =>
        writer.UpdateLastMetric(
          TemperatureResult,
          AccumulatedUrn,
          PublishAt,
          484
        )
      , Times.Exactly(1)
    );
    readerMock.Verify(
      reader =>
        reader.ReadLastUpdate(
          TemperatureResult,
          AccumulatedUrn
        )
      , Times.Once
    );
  }

  [Test]
  public void should_read_time_series_db_on_publish()
  {
    //given
    var initialUpdateAt = T._4;
    var publishAt = T._6;
    var sut = CreateSut();
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        initialUpdateAt,
        242
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        CountUrn,
        initialUpdateAt,
        2
      )
      ;

    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureUrnAccumulated,
        242,
        0,
        5,
        4
      ),
      PDH.CreateMetricValueProperty(
        TemperatureUrnCount,
        2,
        0,
        5,
        4
      )
    };

    //when
    var resultingEvents = sut.Publish(publishAt);

    //then
    readerMock.Verify(
      reader =>
        reader.ReadLastUpdate(
          TemperatureResult,
          AccumulatedUrn
        )
      , Times.Once
    );

    readerMock.Verify(
      reader =>
        reader.ReadLastUpdate(
          TemperatureResult,
          CountUrn
        )
      , Times.Once
    );

    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsSome).As("resultingEvent don’t exist").IsTrue();
    Check.That(resultingEvents.GetValue()).IsNotNull();
    Check.That(resultingEvents.GetValue()).IsEqualTo(expected);
  }

  [Test]
  public void should_read_start_time_series_db_on_publish()
  {
    //given
    var initialUpdateAt = T._4;
    var publishAt = T._6;
    var sut = CreateSut();
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
      readerMock,
      TemperatureResult,
      AccumulatedUrn,
      publishAt,
      242
    );
    ReaderReturnStartAt(
        readerMock,
        TemperatureResult,
        T._0
      )
      ;

    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureUrnAccumulated,
        242,
        0,
        4,
        6
      )
    };

    //when
    var resultingEvents = sut.Publish(publishAt);

    //then
    readerMock.Verify(
      reader =>
        reader.ReadStartAt(TemperatureResult)
      , Times.Once
    );

    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsSome).As("resultingEvent don’t exist").IsTrue();
    Check.That(resultingEvents.GetValue()).IsNotNull();
    Check.That(resultingEvents.GetValue()).HasSize(1);
    Check.That(resultingEvents.GetValue()[0].At).IsEqualTo(expected[0].At);
    Check.That(resultingEvents.GetValue()[0].Value.SamplingStartDate).IsEqualTo(expected[0].Value.SamplingStartDate);
    Check.That(resultingEvents.GetValue()[0].Value.Value).IsEqualTo(expected[0].Value.Value);
  }

  [Test]
  public void should_read_end_time_series_db_on_publish()
  {
    //given
    var initialUpdateAt = T._4;
    var publishAt = T._6;
    var sut = CreateSut();
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
      readerMock,
      TemperatureResult,
      AccumulatedUrn,
      initialUpdateAt,
      242
    );

    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        initialUpdateAt
      )
      ;

    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        initialUpdateAt
      )
      ;

    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureUrnAccumulated,
        242,
        0,
        4,
        6
      )
    };

    //when
    var resultingEvents = sut.Publish(publishAt);

    //then
    readerMock.Verify(
      reader =>
        reader.ReadEndAt(TemperatureResult)
      , Times.Once
    );

    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsSome).As("resultingEvent don’t exist").IsTrue();
    Check.That(resultingEvents.GetValue()).IsNotNull();
    Check.That(resultingEvents.GetValue()).HasSize(1);
    Check.That(resultingEvents.GetValue()[0].At).IsEqualTo(expected[0].At);
    Check.That(resultingEvents.GetValue()[0].Value.SamplingEndDate).IsEqualTo(expected[0].Value.SamplingEndDate);
    Check.That(resultingEvents.GetValue()[0].Value.Value).IsEqualTo(expected[0].Value.Value);
  }

  [Test]
  public void should_change_end_time_series_db_on_update()
  {
    //given
    var sut = CreateSut();

    //when
    sut.Update(PublishAt);

    //then
    var writerMock = Mock.Get(_timeMathWriter);
    writerMock.Verify(
      writer => writer.UpdateEndAt(
        TemperatureResult,
        PublishAt
      )
      , Times.Once
    );
  }

  [Test]
  public void should_change_end_time_series_db_on_value_update()
  {
    //given
    var initialUpdateAt = T._4;
    var sut = CreateSut();
    var updateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(242f),
      initialUpdateAt
    );

    //when
    sut.Update(updateValue);

    //then
    var writerMock = Mock.Get(_timeMathWriter);
    writerMock.Verify(
      writer => writer.UpdateEndAt(
        TemperatureResult,
        initialUpdateAt
      )
      , Times.Once
    );
  }

  [Test]
  public void should_change_start_time_series_db_on_publish()
  {
    //given
    var initialPublishAt = T._3;
    var sut = CreateSut();

    //when
    sut.Publish(initialPublishAt);

    //then
    var writerMock = Mock.Get(_timeMathWriter);
    writerMock.Verify(
      writer => writer.UpdateStartAt(
        TemperatureResult,
        initialPublishAt
      )
      , Times.Once
    );
  }

  [Test]
  public void should_not_change_start_time_series_db_on_computer_start()
  {
    //given
    var writerMock = Mock.Get(_timeMathWriter);

    //when
    var unused = CreateSut();

    //then
    writerMock.Verify(
      writer => writer.UpdateStartAt(
        TemperatureResult,
        It.IsAny<TimeSpan>()
      )
      , Times.Never
    );
  }

  [Test]
  [Ignore("Remove should be automatic now")]
  public void should_remove_data_before_start_time_series_db_on_publish()
  {
    //given
    var initialUpdateAt = T._2;
    var initialPublishAt = T._3;
    var sut = CreateSut();
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        initialUpdateAt,
        242
      )
      ;

    ReaderReturnEndAt(
      readerMock,
      TemperatureResult,
      initialUpdateAt
    );

    //when
    sut.Publish(initialPublishAt);

    //then
    /*
    var writerMock = Mock.Get(_timeMathWriter);
    writerMock.Verify(
      writer => writer.RemoveMetricBefore(
        TemperatureUrn, accumulatedUrn,
        initialPublishAt
      )
      , Times.Once
    );
  */
  }

  [Test]
  public void should_read_start_time_series_on_service_initialisation()
  {
    //given
    var restartAt = T._4;
    var startAt = T._1;
    var endAt = T._2;
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        endAt,
        242
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        endAt
      )
      ;
    ReaderReturnStartAt(
        readerMock,
        TemperatureResult,
        startAt
      )
      ;

    //when
    var sut = new AccumulatorComputer(
      TemperatureUrn,
      _timeMathWriter,
      _timeMathReader,
      T._0,
      restartAt
    );
    sut.Publish(restartAt);
    //then

    readerMock.Verify(
      reader =>
        reader.ReadStartAt(TemperatureResult)
      , Times.AtLeastOnce
    );
  }

  [Test]
  public void should_read_time_series_db_on_creation()
  {
    //given
    var restartAt = T._4;
    var startAt = T._1;
    var endAt = T._2;
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        endAt,
        242
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        endAt
      )
      ;
    ReaderReturnStartAt(
        readerMock,
        TemperatureResult,
        startAt
      )
      ;

    //when
    var sut = new AccumulatorComputer(
      TemperatureUrn,
      _timeMathWriter,
      _timeMathReader,
      T._0,
      restartAt
    );
    sut.Publish(restartAt);

    //then

    readerMock.Verify(
      reader =>
        reader.ReadEndAt(TemperatureResult)
      , Times.AtLeastOnce
    );
  }

  [Test]
  public void should_publish_value_received_on_restart_but_after_publish_time()
  {
    //given
    var lastPublishAt = T._3;
    var endAt = T._5;
    var restartAt = T._7;
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        endAt,
        242
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        endAt
      )
      ;
    ReaderReturnStartAt(
        readerMock,
        TemperatureResult,
        lastPublishAt
      )
      ;
    //when
    var sut = new AccumulatorComputer(
      TemperatureUrn,
      _timeMathWriter,
      _timeMathReader,
      T._0,
      restartAt
    );
    var resultingEvents = sut.Publish(restartAt);

    //then
    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureUrnAccumulated,
        242,
        lastPublishAt,
        endAt
      )
    };

    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsSome).As("resultingEvent exist").IsTrue();
    Check.That(resultingEvents.GetValue()).IsNotNull();
    Check.That(resultingEvents.GetValue()).HasSize(1);
    Check.That(resultingEvents.GetValue()).IsEqualTo(expected);
  }

  private AccumulatorComputer CreateSut()
  {
    return new AccumulatorComputer(
      TemperatureUrn,
      _timeMathWriter,
      _timeMathReader,
      T._0,
      TimeSpan.Zero
    );
  }
}
