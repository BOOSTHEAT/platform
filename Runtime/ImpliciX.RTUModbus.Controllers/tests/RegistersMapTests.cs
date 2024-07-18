using System;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using NUnit.Framework;

namespace ImpliciX.RTUModbus.Controllers.Tests;

public class RegistersMapTests
{
  [SetUp]
  public void EachTestMustRunAsIfTheApplicationWasJustStarted()
  {
    RegistersMap.Factory = null;
  }

  [Test]
  public void DeclareMap()
  {
    RegistersMap.Factory = () => new RegistersMapImpl();

    var map = RegistersMap.Create()
      .RegistersSegmentsDefinitions(new RegistersSegmentsDefinition(RegisterKind.Holding){StartAddress = 10, RegistersToRead = 60})
      .For(_measureNode1)
      .DecodeRegisters(10, 20, GetDecoder(data => Pressure.FromFloat(42).Value))
      .For(_measureNode2)
      .DecodeRegisters(30, 40, GetDecoder(data => Temperature.FromFloat(43).Value));

    Assert.That(map.Conversions.Count(), Is.EqualTo(2));
    var conversion1 = map.Conversions.First();
    Assert.That(conversion1.MeasureUrn, Is.EqualTo(_measureNode1.measure));
    Assert.That(conversion1.StatusUrn, Is.EqualTo(_measureNode1.status));
    Assert.That(conversion1.Slices, Is.EqualTo(new [] { new Slice(0,0, 10, 20) }));
    Assert.That(
      conversion1.Decode(new ushort[] { }, TimeSpan.Zero, null).Value,
      Is.EqualTo(Measure<Pressure>.Create(_measureNode1.measure, _measureNode1.status, Pressure.FromFloat(42),
        TimeSpan.Zero))
    );
    var conversion2 = map.Conversions.Last();
    Assert.That(conversion2.MeasureUrn, Is.EqualTo(_measureNode2.measure));
    Assert.That(conversion2.StatusUrn, Is.EqualTo(_measureNode2.status));
    Assert.That(conversion2.Slices, Is.EqualTo(new [] { new Slice(0,20, 30, 40) }));
    Assert.That(
      conversion2.Decode(new ushort[] { }, TimeSpan.Zero, null).Value,
      Is.EqualTo(Measure<Temperature>.Create(_measureNode2.measure, _measureNode2.status, Temperature.FromFloat(43),
        TimeSpan.Zero))
    );
  }

  [Test]
  public void DecodingCanReturnNull_MaybeThisHasToBeRedesigned()
  {
    RegistersMap.Factory = () => new Controllers.RegistersMapImpl();

    var map = RegistersMap.Create()
      .RegistersSegmentsDefinitions(new RegistersSegmentsDefinition(RegisterKind.Holding){StartAddress = 10, RegistersToRead = 20})
      .For(_measureNode1)
      .DecodeRegisters(10, 20, (measureUrn, measureStatus, measureRegisters, currentTime, driverStateKeeper) => null);

    var conversion = map.Conversions.First();
    var decoded = conversion.Decode(new ushort[] { }, TimeSpan.Zero, null);
    Assert.That(decoded, Is.EqualTo(null));
  }

  private readonly MeasureNode<Pressure> _measureNode1 = new("pressure", new RootModelNode("root"));
  private readonly MeasureNode<Temperature> _measureNode2 = new("temperature", new RootModelNode("root"));

  private MeasureDecoder GetDecoder<T>(Func<ushort[], T> convert) =>
  (measureUrn, measureStatus, measureRegisters,
    currentTime, driverStateKeeper) => Result<IMeasure>
    .Create(Measure<T>.Create(measureUrn, measureStatus, Result<T>.Create(convert(measureRegisters)), currentTime));
}