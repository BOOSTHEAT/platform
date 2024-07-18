using System;
using ImpliciX.Language.Core;
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

[TestFixture(Category = "ExcludeFromCI")]
[NonParallelizable]
public class GaugeComputersTests
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
    ReaderReturnStartAt(
      tsReaderMock,
      TemperatureMeasure,
      T._0
    );
    ReaderReturnLastUpdateAt(
        tsReaderMock,
        TemperatureMeasure,
        ""
      )
      ;
    ReaderReturnEndAt(
      tsReaderMock,
      TemperatureMeasure,
      PublishAt
    );
    /*
    tsReaderMock
      .Setup(reader => reader.ReadEndAt(It.IsAny<string>()))
      .Callback(
        (
            string urn
          ) =>
          TestContext.Progress.WriteLine($"Incoming call: {urn}")
      )
      .Returns(PublishAt)
      ;
    tsReaderMock
      .Setup(reader => reader.ReadStartAt(It.IsAny<string>()))
      .Callback(
        (
            string urn
          ) =>
          TestContext.Progress.WriteLine($"Incoming call: {urn}")
      )
      .Returns(T._0)
      ;
    */
  }

  private static readonly TimeHelper T = TimeHelper.Minutes();
  private ITimeMathWriter _timeMathWriter;

  private ITimeMathReader _timeMathReader;

  private static readonly TimeSpan PublishAt = T._5;

  [Test]
  public void should_not_publish_gauge_computer_when_value_is_empty()
  {
    var computer = CreateSut();
    var resultingEvents = computer.Publish(PublishAt);
    Check.That(resultingEvents).IsEqualTo(Option<Property<MetricValue>[]>.None());
  }

  [Test]
  public void should_publish_value_received_before_publish_time()
  {
    //given
    var sut = CreateSut();

    var tsReaderMock = Mock.Get(_timeMathReader);
    var publishAt = T._5;
    tsReaderMock
      .Setup(
        reader =>
          reader.ReadStartAt(TemperatureMeasure)
      )
      .Returns(T._0)
      ;
    tsReaderMock
      .Setup(
        reader =>
          reader.ReadEndAt(TemperatureMeasure)
      )
      .Callback(
        (
            string urn
          ) =>
          TestContext.Progress.WriteLine($"Incoming call: {urn}")
      )
      .Returns(publishAt)
      ;
    tsReaderMock
      .Setup(
        reader =>
          reader.ReadLastUpdate(
            TemperatureMeasure,
            ""
          )
      )
      .Callback(
        (
            string rootUrn,
            string suffix
          ) =>
          TestContext.Progress.WriteLine($"Incoming call: {rootUrn} {suffix}")
      )
      .Returns(
        Option<FloatValueAt>
          .Some(
            new FloatValueAt(
              publishAt,
              242
            )
          )
      )
      ;
    //when
    var resultingEvents = sut.Publish(publishAt);

    //then
    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureMeasure,
        242,
        0,
        5
      )
    };

    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsSome).As("resultingEvent exist").IsTrue();
    Check.That(resultingEvents.GetValue()).IsNotNull();
    Check.That(resultingEvents.GetValue()).HasSize(1);
    Check.That(resultingEvents.GetValue()).IsEqualTo(expected);
  }

  [Test]
  public void should_not_publish_values_more_recent_than_publish_time()
  {
    var publishAt = T._5;
    var sut = CreateSut();
    var updateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(242f),
      publishAt
    );

    var resultingEvents = sut.Publish(publishAt);

    sut.Update(updateValue);

    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsNone).IsTrue();
  }

  [Test]
  public void should_keep_values_more_recent_after_publish()
  {
    //given
    var firstUpdateAt = T._9;
    var sut = CreateSut();
    var updateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(242f),
      firstUpdateAt
    );

    sut.Update(updateValue);
    var updateAt = T._10;

    var resultingEvents = sut.Publish(PublishAt);
    var tsReaderMock = Mock.Get(_timeMathReader);
    tsReaderMock
      .Setup(reader => reader.ReadStartAt(TemperatureMeasure))
      .Returns(PublishAt)
      ;
    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsNone).IsTrue();

    tsReaderMock
      .Setup(reader => reader.ReadEndAt(TemperatureMeasure))
      .Returns(updateAt)
      ;
    sut.Update(updateAt);

    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureMeasure,
        242,
        5,
        10
      )
    };
    Mock.Get(_timeMathReader)
      .Setup(
        reader => reader.ReadLastUpdate(
          TemperatureMeasure,
          ""
        )
      )
      .Returns(
        Option<FloatValueAt>.Some(
          new FloatValueAt(
            TimeSpan.FromMinutes(9),
            242
          )
        )
      )
      ;

    //then
    resultingEvents = sut.Publish(updateAt).GetValue();

    //then
    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsSome).As("resultingEvent exist").IsTrue();
    Check.That(resultingEvents.GetValue()).IsNotNull();
    Check.That(resultingEvents.GetValue()).HasSize(1);
    Check.That(resultingEvents.GetValue()).IsEqualTo(expected);
  }

  [Test]
  public void should_not_keep_values_in_storage_after_publish()
  {
    //given
    var sut = CreateSut();
    var firstPublish = T._9;
    var secondPublish = T._10;
    var secondUpdateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(242f),
      secondPublish
    );

    /*
    var firstUpdateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(42f),
      firstPublish
    );

    sut.Update(firstUpdateValue);
    sut.Update(firstPublish);
    */

    var readerMock = Mock.Get(_timeMathReader);
    readerMock
      .Setup(
        reader => reader.ReadLastUpdate(
          TemperatureMeasure,
          ""
        )
      )
      .Returns(
        Option<FloatValueAt>.Some(
          new FloatValueAt(
            firstPublish,
            42
          )
        )
      )
      ;
    readerMock
      .Setup(reader => reader.ReadEndAt(TemperatureMeasure))
      .Returns(firstPublish)
      ;

    //when
    var p1 = sut.Publish(firstPublish).GetValueOrDefault(Array.Empty<Property<MetricValue>>());
    /*
    sut.Update(secondUpdateValue);
    sut.Update(secondPublish);
    */
    readerMock
      .Setup(
        reader => reader.ReadLastUpdate(
          TemperatureMeasure,
          ""
        )
      )
      .Returns(
        Option<FloatValueAt>.Some(
          new FloatValueAt(
            secondPublish,
            242
          )
        )
      )
      ;
    readerMock
      .Setup(reader => reader.ReadStartAt(TemperatureMeasure))
      .Returns(firstPublish)
      ;
    readerMock
      .Setup(reader => reader.ReadEndAt(TemperatureMeasure))
      .Returns(secondPublish)
      ;

    var p2 = sut.Publish(secondPublish).GetValueOrDefault(Array.Empty<Property<MetricValue>>());

    //then
    Check.That(p1.Length).As("nb values").Equals(1);
    Check.That(p1).ContainsExactly(
      PDH.CreateMetricValueProperty(
        TemperatureMeasure,
        42,
        0,
        9
      )
    );
    Check.That(p2).ContainsExactly(
      PDH.CreateMetricValueProperty(
        TemperatureMeasure,
        242,
        9,
        10
      )
    );

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
        TemperatureMeasure,
        updateAt
      )
    );
    //when
    sut.Update(updateAt);

    //then
    writerMock.Verify(
      writer =>
        writer.UpdateEndAt(
          TemperatureMeasure,
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
          TemperatureMeasure,
          "",
          updateAt,
          242
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
      updateAt
    );

    //when
    sut.Update(updateValue);

    //then
    writerMock.Verify(
      writer =>
        writer.UpdateLastMetric(
          TemperatureMeasure,
          "",
          updateAt,
          242
        )
      , Times.Exactly(1)
    );
    writerMock.Verify(
      writer =>
        writer.UpdateEndAt(
          TemperatureMeasure,
          updateAt
        )
      , Times.Exactly(1)
    );
  }

  [Test]
  public void should_read_time_series_db_on_publish()
  {
    //given
    var sut = CreateSut();
    var updateAt = T._4;
    var publishAt = T._6;
    var returnValue = Option<FloatValueAt>.Some(
      new FloatValueAt(
        updateAt,
        242
      )
    );
    var readerMock = Mock.Get(_timeMathReader);
    readerMock
      .Setup(
        reader => reader.ReadLastUpdate(
          TemperatureMeasure,
          ""
        )
      )
      .Returns(returnValue)
      ;

    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureMeasure,
        242,
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
          TemperatureMeasure,
          ""
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
    var sut = CreateSut();
    var updateAt = T._4;
    var publishAt = T._6;
    var returnValue = Option<FloatValueAt>.Some(
      new FloatValueAt(
        updateAt,
        242
      )
    );
    var readerMock = Mock.Get(_timeMathReader);
    readerMock
      .Setup(
        reader => reader.ReadLastUpdate(
          TemperatureMeasure,
          ""
        )
      )
      .Returns(returnValue)
      ;

    readerMock
      .Setup(reader => reader.ReadStartAt(TemperatureMeasure))
      .Returns(T._0)
      ;

    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureMeasure,
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
        reader.ReadStartAt(TemperatureMeasure)
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
    var updateAt = T._4;
    var publishAt = T._6;
    var sut = CreateSut();
    var returnValue = Option<FloatValueAt>.Some(
      new FloatValueAt(
        updateAt,
        242
      )
    );
    var readerMock = Mock.Get(_timeMathReader);
    readerMock
      .Setup(
        reader => reader.ReadLastUpdate(
          TemperatureMeasure,
          ""
        )
      )
      .Returns(returnValue)
      ;

    readerMock
      .Setup(reader => reader.ReadEndAt(TemperatureMeasure))
      .Returns(updateAt)
      ;

    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureMeasure,
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
        reader.ReadEndAt(TemperatureMeasure)
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
        TemperatureMeasure,
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
        TemperatureMeasure,
        initialUpdateAt
      )
      , Times.Once
    );
  }

  [Test]
  public void should_save_to_publish_time_series_db_on_publish()
  {
    //given
    var startAt = T._0;
    var updateAt = T._2;
    var publishAt = T._3;
    var sut = CreateSut();
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureMeasure,
        "",
        updateAt,
        242
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureMeasure,
        updateAt
      )
      ;
    ReaderReturnStartAt(
        readerMock,
        TemperatureMeasure,
        startAt
      )
      ;

    //when
    var resultingEvents = sut.Publish(publishAt);

    //then
    var writerMock = Mock.Get(_timeMathWriter);
    writerMock.Verify(
      writer => writer.AddValueAtPublish(
        TemperatureMeasure,
        "",
        publishAt,
        242
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
        TemperatureMeasure,
        initialPublishAt
      )
      , Times.Once
    );
  }

  [Test]
  [Ignore("Remove should be automatic now")]
  public void should_remove_data_before_start_time_series_db_on_publish()
  {
    //given
    var updateAt = T._2;
    var publishAt = T._3;
    var sut = CreateSut();
    var returnValue = Option<FloatValueAt>.Some(
      new FloatValueAt(
        updateAt,
        242
      )
    );
    var readerMock = Mock.Get(_timeMathReader);
    readerMock
      .Setup(
        reader => reader.ReadLastUpdate(
          TemperatureMeasure,
          ""
        )
      )
      .Returns(returnValue)
      ;

    readerMock
      .Setup(reader => reader.ReadEndAt(TemperatureMeasure))
      .Returns(updateAt)
      ;

    //when
    sut.Publish(publishAt);

    //then
    var writerMock = Mock.Get(_timeMathWriter);
    /*
    writerMock.Verify(
      writer => writer.RemoveMetricBefore(
        TemperatureMeasure,
        publishAt
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
        TemperatureMeasure,
        "",
        endAt,
        242
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureMeasure,
        endAt
      )
      ;
    ReaderReturnStartAt(
        readerMock,
        TemperatureMeasure,
        startAt
      )
      ;

    //when
    var sut = new GaugeComputer(
      TemperatureMeasureUrn,
      _timeMathWriter,
      _timeMathReader,
      restartAt
    );
    sut.Publish(restartAt);
    //then

    readerMock.Verify(
      reader =>
        reader.ReadStartAt(TemperatureMeasure)
      , Times.AtLeastOnce
    );
  }

  [Test]
  public void should_read_lastPublished_time_series_on_service_initialisation()
  {
    //given
    var restartAt = T._4;
    var startAt = T._1;
    var endAt = T._2;
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureMeasure,
        "",
        endAt,
        242
      )
      ;
    ReaderReturnFirstValueAtPublish(
        readerMock,
        TemperatureMeasure,
        "",
        endAt,
        242
      )
      ;
    ReaderReturnLastPublishAt(
        readerMock,
        TemperatureMeasure,
        startAt
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureMeasure,
        endAt
      )
      ;
    ReaderReturnStartAt(
        readerMock,
        TemperatureMeasure,
        startAt
      )
      ;

    //when
    var sut = new GaugeComputer(
      TemperatureMeasureUrn,
      _timeMathWriter,
      _timeMathReader,
      restartAt
    );
    sut.IsPublishTimePassed(
      restartAt,
      T._3
    );
    //then

    readerMock.Verify(
      reader =>
        reader.ReadLastPublishedInstant(TemperatureMeasure)
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
        TemperatureMeasure,
        "",
        endAt,
        242
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureMeasure,
        endAt
      )
      ;
    ReaderReturnStartAt(
        readerMock,
        TemperatureMeasure,
        startAt
      )
      ;

    //when
    var sut = new GaugeComputer(
      TemperatureMeasureUrn,
      _timeMathWriter,
      _timeMathReader,
      restartAt
    );
    sut.Publish(restartAt);

    //then

    readerMock.Verify(
      reader =>
        reader.ReadEndAt(TemperatureMeasure)
      , Times.AtLeastOnce
    );
  }

  [Test]
  public void should_publish_value_received_on_restart_after_publish_time()
  {
    //given
    var lastPublishAt = T._3;
    var endAt = T._5;
    var restartAt = T._7;
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureMeasure,
        "",
        endAt,
        242
      )
      ;

    ReaderReturnStartAt(
        readerMock,
        TemperatureMeasure,
        lastPublishAt
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureMeasure,
        endAt
      )
      ;

    //when
    var sut = new GaugeComputer(
      TemperatureMeasureUrn,
      _timeMathWriter,
      _timeMathReader,
      restartAt
    );
    var resultingEvents = sut.Publish(restartAt);

    //then
    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureMeasure,
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

  [Test]
  public void should_not_publish_value_received_on_restart_before_publish_time()
  {
    //given
    var startAt = T._3;
    var endAt = T._4;
    var restartAt = T._5;
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureMeasure,
        "",
        endAt,
        242
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureMeasure,
        endAt
      )
      ;
    ReaderReturnStartAt(
        readerMock,
        TemperatureMeasure,
        startAt
      )
      ;
    //when
    var sut = new GaugeComputer(
      TemperatureMeasureUrn,
      _timeMathWriter,
      _timeMathReader,
      restartAt
    );
    var resultingEvents = sut.Publish(restartAt);

    //then
    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureMeasure,
        242,
        startAt,
        endAt
      )
    };

    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsSome).As("resultingEvent exist").IsTrue();
    Check.That(resultingEvents.GetValue()).IsNotNull();
    Check.That(resultingEvents.GetValue()).HasSize(1);
    Check.That(resultingEvents.GetValue()).IsEqualTo(expected);
  }

  [Test]
  public void should_change_start_time_series_db_on_computer_start()
  {
    //given
    var startAt = T._1;
    var readerMock = Mock.Get(_timeMathReader);
    var writerMock = Mock.Get(_timeMathWriter);
    ReaderReturnLastPublishAt(
        readerMock,
        TemperatureMeasure,
        T._0
      )
      ;

    //when
    var sut = CreateSut();
    sut.IsPublishTimePassed(
      startAt,
      T._3
    );

    //then
    writerMock.Verify(
      writer => writer.UpdateStartAt(
        TemperatureMeasure,
        startAt
      )
      , Times.Never
    );
  }

  [Test]
  public void should_update_start_value_on_restart_after_publish_time()
  {
    //given
    var lastPublishAt = T._3;
    var endAt = T._5;
    var restartAt = T._7;
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureMeasure,
        "",
        endAt,
        30
      )
      ;
    ReaderReturnFirstValueAtPublish(
        readerMock,
        TemperatureMeasure,
        "",
        lastPublishAt,
        13
      )
      ;

    ReaderReturnStartAt(
        readerMock,
        TemperatureMeasure,
        lastPublishAt
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureMeasure,
        endAt
      )
      ;
    //when
    var sut = new GaugeComputer(
      TemperatureMeasureUrn,
      _timeMathWriter,
      _timeMathReader,
      restartAt
    );
    var resultingEvents = sut.Publish(restartAt);

    //then
    var writerMock = Mock.Get(_timeMathWriter);

    writerMock.Verify(
      writer => writer.UpdateStartAt(
        TemperatureMeasure,
        restartAt
      )
      , Times.Once
    );
    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureMeasure,
        30,
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

  private GaugeComputer CreateSut()
  {
    return new GaugeComputer(
      TemperatureMeasureUrn,
      _timeMathWriter,
      _timeMathReader,
      TimeSpan.Zero
    );
  }
}

public class OptionDefaultValueProvider : DefaultValueProvider
{
  protected override object GetDefaultValue(
    Type type,
    Mock mock
  )
  {
    return Option<FloatValueAt>.None();
  }
}
