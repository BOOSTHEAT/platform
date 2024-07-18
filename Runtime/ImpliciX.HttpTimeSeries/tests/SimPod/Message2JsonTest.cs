using ImpliciX.HttpTimeSeries.SimPod;
using ImpliciX.RuntimeFoundations.Events;
using Moq;
using NFluent;

namespace ImpliciX.HttpTimeSeries.Tests.SimPod;

public class Message2JsonTest
{
  private readonly TimeSpan epoch = DateTimeOffset.UnixEpoch.Offset;
  private Mock<IDataService> _dataServiceMock;

  [SetUp]
  public  void Setup()
  {
    _dataServiceMock = new Mock<IDataService>();
  }

  [Test]
  public  void CheckNoMetricsReturnEmptyResult()
  {
    //given
    var urn = "system:metrics:electrical:heat_service_state:_10Minutes:Disabled:occurrence";
    var datapoints = new Dictionary<string, IEnumerable<TimeSeriesValue>>();

    //when
    JsonTimeSeriesMessages messages = new (datapoints);
    var res = messages.ToJson();

    //then
    var expected = "[]";
    Check.That(expected).IsEqualTo(res);
  }

  [Test]
  public  void CheckEmptyMetricsReturnEmptyArray()
  {
    //given
    var urn = "system:metrics:electrical:heat_service_state:_10Minutes:Disabled:occurrence";
    var datapoints = new Dictionary<string, IEnumerable<TimeSeriesValue>>();
    datapoints[urn] = Array.Empty<TimeSeriesValue>();

    //when
    JsonTimeSeriesMessages messages = new (datapoints);
    var res = messages.ToJson();
    //then
    var expected = "[\n" +
                   "  {\n" +
                   "    \"target\": \"" + urn + "\",\n" +
                   "    \"datapoints\": []\n" +
                   "  }\n" +
                   "]";
    Check.That(expected).IsEqualTo(res);
  }

  [Test]
  public  void CheckOneMetricsReturnOneElementArray()
  {
    //given
    var urn = "system:metrics:electrical:heat_service_state:_10Minutes:Disabled:occurrence";
    var datapoints = new Dictionary<string, IEnumerable<TimeSeriesValue>>();
    TimeSeriesValue[] timeSeriesValues =
    {
      new (
        TimeSpan.FromMinutes(1) + TimeSpan.FromMilliseconds(62135596800000L ),
        5.0f
      )
    };
    datapoints[urn] = timeSeriesValues;

    //when
    JsonTimeSeriesMessages messages = new (datapoints);
    var res = messages.ToJson();
    //then
    var expected = "[\n" +
                   "  {\n" +
                   "    \"target\": \"" + urn + "\",\n" +
                   "    \"datapoints\": [\n" +
                   "      [\n" +
                   "        5,\n" +
                   "        60000\n" +
                   "      ]\n" +
                   "    ]\n" +
                   "  }\n" +
                   "]";
    Check.That(res).IsEqualTo(expected);
  }

  [Test]
  public  void CheckFloatOneMetricsReturnOneElementArray()
  {
    //given
    var urn = "system:metrics:electrical:heat_service_state:_10Minutes:Disabled:occurrence";
    var datapoints = new Dictionary<string, IEnumerable<TimeSeriesValue>>();
    TimeSeriesValue[] timeSeriesValues =
    {
      new (
        TimeSpan.FromMinutes(1) + TimeSpan.FromMilliseconds(62135596800000L ),
        5.009765625f
      )
    };
    datapoints[urn] = timeSeriesValues;

    //when
    JsonTimeSeriesMessages messages = new (datapoints);
    var res = messages.ToJson();
    //then
    var expected = "[\n" +
                   "  {\n" +
                   "    \"target\": \"" + urn + "\",\n" +
                   "    \"datapoints\": [\n" +
                   "      [\n" +
                   "        5.009765625,\n" +
                   "        60000\n" +
                   "      ]\n" +
                   "    ]\n" +
                   "  }\n" +
                   "]";
    Check.That(res).IsEqualTo(expected);
  }
}
