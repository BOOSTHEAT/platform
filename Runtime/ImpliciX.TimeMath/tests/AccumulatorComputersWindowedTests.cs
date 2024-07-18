using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Collections;
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
public class AccumulatorComputersWindowedTests
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
        TemperatureResult,
        T._0
      )
      ;
    ReaderReturnEndAt(
        tsReaderMock,
        TemperatureResult,
        PublishAt
      )
      ;
    ReaderReturnLastUpdateAt(
        tsReaderMock,
        TemperatureResult,
        AccumulatedUrn
      )
      ;
    ReaderReturnLastUpdateAt(
        tsReaderMock,
        TemperatureResult,
        CountUrn
      )
      ;
  }

  private static readonly TimeHelper T = TimeHelper.Minutes();
  private ITimeMathWriter _timeMathWriter;

  private ITimeMathReader _timeMathReader;

  // private static readonly string StartTemperatureUrn = TemperatureUrn + "$samplingStartAt";
  // private static readonly string EndTemperatureUrn = TemperatureUrn + "$samplingEndAt";

  // private static readonly string ValueAtPublishedTemperatureAccumulatedUrn =
  //   TemperatureUrnAccumulated + "$valueAtPublished";

  // private static readonly string ValueAtPublishedTemperatureCountUrn = TemperatureUrnCount + "$valueAtPublished";
  private static readonly TimeSpan PublishAt = T._5;

  [Test]
  public void should_publish_zero_values_accumulator_computer_when_value_is_empty()
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
  public void should_not_publish_values_more_recent_than_publish_time()
  {
    var publishAt = T._6;
    var sut = CreateSut();
    var updateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(242f),
      publishAt
    );

    var resultingEvents = sut.Publish(publishAt);

    sut.Update(updateValue);

    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents.IsSome).As("resultingEvent exist").IsTrue();
    Check.That(resultingEvents.GetValue()).IsNotNull();
    Check.That(resultingEvents.GetValue()).HasSize(2);
  }

  [Test]
  public void should_keep_values_more_recent_after_publish()
  {
    //given
    var lastPublishedAt = T._5;
    var publishingAt = T._9;
    var sut = CreateSut();

    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnStartAt(
        readerMock,
        TemperatureResult,
        lastPublishedAt
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        publishingAt,
        242
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        CountUrn,
        publishingAt,
        2
      )
      ;
    ReaderReturnFirstValueAtPublish(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        lastPublishedAt,
        0
      )
      ;
    ReaderReturnFirstValueAtPublish(
        readerMock,
        TemperatureResult,
        CountUrn,
        lastPublishedAt,
        0
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        publishingAt
      )
      ;

    //then
    var resultingEvents = sut.Publish(publishingAt).GetValue();

    //then
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
    Check.That(resultingEvents).IsNotNull();
    Check.That(resultingEvents).IsEqualTo(expected);
  }

  [Test]
  public void should_keep_values_in_period_in_storage_after_publish()
  {
    //given
    var sut = CreateSut();
    var initialPublish = T._3;
    var firstPublish = T._6;
    var secondPublish = T._9;

    var readerMock = Mock.Get(_timeMathReader);

    ReaderReturnStartAt(
        readerMock,
        TemperatureResult,
        T._0
      )
      ;
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
    ReaderReturnFirstValueAtPublish(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        firstPublish,
        0
      )
      ;
    ReaderReturnFirstValueAtPublish(
        readerMock,
        TemperatureResult,
        CountUrn,
        firstPublish,
        0
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        firstPublish
      )
      ;

    var p1 = sut.Publish(firstPublish).GetValueOrDefault(Array.Empty<Property<MetricValue>>());

    ReaderReturnStartAt(
        readerMock,
        TemperatureResult,
        initialPublish
      )
      ;
    ReaderReturnFirstValueAtPublish(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        initialPublish,
        0
      )
      ;
    ReaderReturnFirstValueAtPublish(
        readerMock,
        TemperatureResult,
        CountUrn,
        initialPublish,
        0
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        secondPublish,
        284
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

    //when

    var p2 = sut.Publish(secondPublish).GetValueOrDefault(Array.Empty<Property<MetricValue>>());

    //then
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
          CountUrn
        )
      , Times.AtLeastOnce
    );
    var expectedP2 = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureUrnAccumulated,
        284,
        3,
        9
      ),
      PDH.CreateMetricValueProperty(
        TemperatureUrnCount,
        3,
        3,
        9
      )
    };
    Check.That(p2).ContainsExactly(expectedP2);

    // _tsReader.ReadAll(fake_analytics_model.temperature).CheckIsSomeAnd((it)=>Check.That(it).HasSize(0));
  }

  [Test]
  public void should_not_keep_values_in_storage_after_publish()
  {
    //given
    var sut = CreateSut();
    var firstPublish = T._3;
    var secondPublish = T._9;

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

    ReaderReturnFirstValueAtPublish(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        firstPublish,
        42
      )
      ;
    ReaderReturnFirstValueAtPublish(
        readerMock,
        TemperatureResult,
        CountUrn,
        firstPublish,
        2
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
    ReaderReturnStartAt(
        readerMock,
        TemperatureResult,
        firstPublish
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        secondPublish
      )
      ;

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
        3
      ),
      PDH.CreateMetricValueProperty(
        TemperatureUrnCount,
        2,
        0,
        3
      )
    };
    Check.That(p1).ContainsExactly(expectedP1);
    var expectedP2 = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureUrnAccumulated,
        200,
        3,
        9
      ),
      PDH.CreateMetricValueProperty(
        TemperatureUrnCount,
        1,
        3,
        9
      )
    };
    Check.That(p2).ContainsExactly(expectedP2);
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
        initialUpdateAt,
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
        initialUpdateAt,
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
  public void should_write_cumulated_value_on_value_update_after_publish_to_time_series_db()
  {
    //given
    var sut = CreateSut();
    var firstPublish = T._6;
    var secondPublish = T._9;

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
        1
      )
      ;
    ReaderReturnEndAt(
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
        42
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        CountUrn,
        secondPublish,
        1
      )
      ;
    ReaderReturnStartAt(
        readerMock,
        TemperatureResult,
        firstPublish
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        secondPublish
      )
      ;

    var p1 = sut.Publish(firstPublish).GetValueOrDefault(Array.Empty<Property<MetricValue>>());
    var writerMock = Mock.Get(_timeMathWriter);
    writerMock.Setup(
        writer => writer.UpdateLastMetric(
          It.IsAny<string>(),
          It.IsAny<string>(),
          It.IsAny<TimeSpan>(),
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
    writerMock.Setup(
        writer => writer.UpdateLastMetric(
          TemperatureResult,
          AccumulatedUrn,
          secondPublish,
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

    //when
    sut.Publish(firstPublish);

    var updateValue = Property<Temperature>.Create(
      fake_model.temperature.measure,
      Temperature.Create(242f),
      secondPublish
    );

    sut.Update(updateValue);

    //then
    writerMock.Verify(
        writer =>
          writer.UpdateLastMetric(
            TemperatureResult,
            AccumulatedUrn,
            secondPublish,
            284
          )
        , Times.Exactly(1)
      )
      ;
    writerMock.Verify(
      writer =>
        writer.UpdateLastMetric(
          TemperatureResult,
          CountUrn,
          secondPublish,
          2
        )
      , Times.Exactly(1)
    );
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
        reader.ReadFirstValueAtPublish(
          TemperatureResult,
          AccumulatedUrn,
          T._0
        )
      , Times.AtLeastOnce
    );
  }

  [Test]
  public void should_read_time_series_db_on_publish()
  {
    //given
    var initialUpdateAt = T._3;
    var publishAt = T._6;
    var sut = CreateSut();
    var returnValue = Option<FloatValueAt>.Some(
      new FloatValueAt(
        initialUpdateAt,
        242
      )
    );
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
        3
      ),
      PDH.CreateMetricValueProperty(
        TemperatureUrnCount,
        2,
        0,
        5,
        3
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
          AccumulatedUrn
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
    var initialUpdateAt = T._0;
    var publishAt = T._6;
    var sut = CreateSut();
    var readerMock = Mock.Get(_timeMathReader);
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        publishAt,
        242
      )
      ;
    ReaderReturnFirstValueAtPublish(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        initialUpdateAt,
        242
      )
      ;
    ReaderReturnStartAt(
        readerMock,
        TemperatureResult,
        initialUpdateAt
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        publishAt
      )
      ;

    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureUrnAccumulated,
        0,
        0,
        3,
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
    Check.That(resultingEvents.GetValue()[0].Value.SamplingStartDate).IsEqualTo(expected[0].Value.SamplingStartDate);
    Check.That(resultingEvents.GetValue()[0].Value.Value).IsEqualTo(expected[0].Value.Value);
  }

  [Test]
  public void should_read_end_time_series_db_on_publish()
  {
    //given
    var initialUpdateAt = T._3;
    var publishAt = T._6;
    var sut = CreateSut();
    var returnValue = Option<FloatValueAt>.Some(
      new FloatValueAt(
        initialUpdateAt,
        242
      )
    );
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
      )
      ;

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
      )
      ;

    var expected = new[]
    {
      PDH.CreateMetricValueProperty(
        TemperatureUrnAccumulated,
        242,
        0,
        3,
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
    var initialUpdateAt = T._3;
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
  public void should_change_start_time_to_zero_on_series_db_on_publish()
  {
    //given
    var initialPublishAt = T._6;
    var sut = CreateSut();
    var writerMock = Mock.Get(_timeMathWriter);
    writerMock.Setup(
        writer => writer.UpdateLastMetric(
          It.IsAny<string>(),
          It.IsAny<string>(),
          It.IsAny<TimeSpan>(),
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

    //when
    sut.Publish(initialPublishAt);

    //then
    var startAt = TimeSpan.Zero;
    writerMock.Verify(
      writer => writer.UpdateStartAt(
        TemperatureResult,
        startAt
      )
      , Times.Once
    );
  }

  [Test]
  public void should_change_start_time_after_period_on_series_db_on_publish()
  {
    //given
    var startAt = T._3;
    var initialPublishAt = T._9;
    var sut = CreateSut();
    var writerMock = Mock.Get(_timeMathWriter);
    writerMock.Setup(
        writer => writer.UpdateLastMetric(
          It.IsAny<string>(),
          It.IsAny<string>(),
          It.IsAny<TimeSpan>(),
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

    //when
    sut.Publish(initialPublishAt);

    //then
    writerMock.Verify(
      writer => writer.UpdateStartAt(
        TemperatureResult,
        startAt
      )
      , Times.Once
    );
  }

  [Test]
  public void should_change_start_time_series_db_on_computer_start()
  {
    //given
    var writerMock = Mock.Get(_timeMathWriter);

    //when
    var sut = CreateSut();

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
    var lastPublish = T._9;
    var sut = CreateSut();
    var returnValue = Option<FloatValueAt>.Some(
      new FloatValueAt(
        initialUpdateAt,
        242
      )
    );
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
      )
      ;

    //when
    sut.Publish(lastPublish);

    //then
    var writerMock = Mock.Get(_timeMathWriter);
    /*
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
  [Ignore("Remove should be automatic now")]
  public void should_keep_data_in_period_time_series_db_on_publish()
  {
    //given
    var initialUpdateAt = T._2;
    var initialPublishAt = T._0;
    var lastPublish = T._6;
    var sut = CreateSut();
    var returnValue = Option<FloatValueAt>.Some(
      new FloatValueAt(
        initialUpdateAt,
        242
      )
    );
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
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        initialUpdateAt,
        242
      )
      ;

    //when
    sut.Publish(lastPublish);

    //then
    var writerMock = Mock.Get(_timeMathWriter);
    /*
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
  public void should_reduce_accumulation_by_data_before_start_time_series_db_on_publish()
  {
    //given
    var initialPublishAt = T._3;
    var lastPublish = T._9;
    var sut = CreateSut();
    var readerMock = Mock.Get(_timeMathReader);

    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        initialPublishAt,
        42
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        lastPublish,
        242
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        CountUrn,
        lastPublish,
        4
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        lastPublish
      )
      ;

    //when
    var writerMock = Mock.Get(_timeMathWriter);
    writerMock.Setup(
        writer => writer.UpdateLastMetric(
          It.IsAny<string>(),
          It.IsAny<string>(),
          It.IsAny<TimeSpan>(),
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
    var e = sut.Publish(lastPublish);

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
    Check.That(e).IsNotNull();
    Check.That(e.IsSome).IsTrue();
    Check.That(e.GetValue()).IsNotNull();
    Check.That(e.GetValue().IsEmpty()).IsFalse();
  }

  [Test]
  [Ignore("Remove should be automatic now")]
  public void should_reduce_accumulation_by_data_before_start_time_on_second_period_series_db_on_publish()
  {
    //given
    var readerMock = Mock.Get(_timeMathReader);
    var firstAt = T._3;
    var firstValue = new FloatValueAt(
      firstAt,
      42
    );

    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        AccumulatedUrn,
        firstAt,
        42
      )
      ;
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureResult,
        CountUrn,
        firstAt,
        1
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureResult,
        firstAt
      )
      ;

    var secondAt = T._6;
    var secondValue = new FloatValueAt(
      secondAt,
      100
    );
    readerMock
      .Setup(
        reader => reader.ReadLastUpdate(
          TemperatureResult,
          AccumulatedUrn
        )
      )
      .Returns(secondValue)
      ;

    readerMock
      .Setup(
        reader => reader.ReadLastUpdate(
          TemperatureResult,
          CountUrn
        )
      )
      .Returns(
        Option<FloatValueAt>.Some(
          new FloatValueAt(
            secondAt,
            2
          )
        )
      )
      ;

    readerMock
      .Setup(reader => reader.ReadEndAt(TemperatureResult))
      .Returns(secondAt)
      ;
    var thirdAt = T._9;
    var thirdValue = new FloatValueAt(
      thirdAt,
      242
    );
    readerMock
      .Setup(
        reader => reader.ReadLastUpdate(
          TemperatureResult,
          AccumulatedUrn
        )
      )
      .Returns(thirdValue)
      ;

    readerMock
      .Setup(
        reader => reader.ReadLastUpdate(
          TemperatureResult,
          CountUrn
        )
      )
      .Returns(
        Option<FloatValueAt>.Some(
          new FloatValueAt(
            thirdAt,
            3
          )
        )
      )
      ;

    readerMock
      .Setup(reader => reader.ReadEndAt(TemperatureResult))
      .Returns(thirdAt)
      ;
    var lastPublish = T._12;

    var sut = CreateSut();
    Option<FloatValueAt>.Some(
        new FloatValueAt(
          lastPublish,
          242
        )
      )
      ;

    //when
    var writerMock = Mock.Get(_timeMathWriter);
    writerMock.Setup(
        writer => writer.UpdateLastMetric(
          It.IsAny<string>(),
          It.IsAny<string>(),
          It.IsAny<TimeSpan>(),
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
    sut.Publish(thirdAt);
    sut.Publish(lastPublish);

    //then
    /*
    writerMock.Verify(
      writer => writer.RemoveMetricBefore(
        TemperatureUrn, accumulatedUrn,
        secondAt
      )
      , Times.Once
    );
  */
  }

  private AccumulatorComputer CreateSut()
  {
    return new AccumulatorComputer(
      TemperatureUrn,
      _timeMathWriter,
      _timeMathReader,
      T._6,
      TimeSpan.Zero
    );
  }
}
