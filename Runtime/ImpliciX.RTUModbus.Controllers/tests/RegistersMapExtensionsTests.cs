
using System;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using NUnit.Framework;

namespace ImpliciX.RTUModbus.Controllers.Tests;

public class RegistersMapExtensionsTests
{
  [SetUp]
  public void Init()
  {
    RegistersMap.Factory = () => new RegistersMapImpl();
  }
  
  [Test]
  public void DecodeSimpleMapping()
  {
    var segDef = new RegistersSegmentsDefinition(RegisterKind.Holding){StartAddress = 0, RegistersToRead = 1};
    var map = RegistersMap.Create()
      .RegistersSegmentsDefinitions(segDef)
      .For(_measureNode1)
      .DecodeRegisters(0, 1, GetDecoder(data => Temperature.FromFloat(data[0]*2).Value ));
    var result = ModbusRegisters.Create(new[] { new RegistersSegment(segDef, new[] { (ushort)666 }) });
    var conversion = map.Eval(result, TimeSpan.Zero, null);
    Assert.That(conversion.IsSuccess);
    var actual = (Temperature)conversion.Value.First().ModelValue();
    Assert.That(actual, Is.EqualTo(Temperature.FromFloat(1332).Value));
  }
  
  [Test]
  public void FailedMeasureWhenResultDataIsMissing()
  {
    var segDef = new RegistersSegmentsDefinition(RegisterKind.Holding){StartAddress = 0, RegistersToRead = 1};
    var map = RegistersMap.Create()
      .RegistersSegmentsDefinitions(segDef)
      .For(_measureNode1)
      .DecodeRegisters(0, 1, GetDecoder(data => Temperature.FromFloat(data[0] * 2).Value));
    var result = ModbusRegisters.Create(new[] { new RegistersSegment(segDef, new ushort[] {}) });
    var conversion = map.Eval(result, TimeSpan.Zero, null);
    Assert.That(conversion.IsSuccess);
    var actual = conversion.Value.First().ModelValue();
    Assert.That(actual, Is.EqualTo(MeasureStatus.Failure));
  }
  
  [Test]
  public void FailedMeasureWhenOnlySomeResultDataIsMissing()
  {
    var segDef = new RegistersSegmentsDefinition(RegisterKind.Holding){StartAddress = 0, RegistersToRead = 2};
    var map = RegistersMap.Create()
      .RegistersSegmentsDefinitions(segDef)
      .For(_measureNode1)
      .DecodeRegisters(0, 1, GetDecoder(data => Temperature.FromFloat(data[0] * 2).Value))
      .For(_measureNode2)
      .DecodeRegisters(1, 1, GetDecoder(data => Temperature.FromFloat(data[0] * 3).Value));
    var result = ModbusRegisters.Create(new[] { new RegistersSegment(segDef, new ushort[] {5}) });
    var conversion = map.Eval(result, TimeSpan.Zero, null);
    Assert.That(conversion.IsSuccess);
    var actual = conversion.Value.First().ModelValue();
    Assert.That(actual, Is.EqualTo(MeasureStatus.Failure));
  }
  
  [Test]
  public void FailedMeasureWhenMultipleRegisterValueIsIncomplete()
  {
    var segDef = new RegistersSegmentsDefinition(RegisterKind.Holding){StartAddress = 0, RegistersToRead = 2};
    var map = RegistersMap.Create()
      .RegistersSegmentsDefinitions(segDef)
      .For(_measureNode1)
      .DecodeRegisters(0, 2, GetDecoder(data => Temperature.FromFloat(data[0] * 100 + data[1]).Value));
    var result = ModbusRegisters.Create(new[] { new RegistersSegment(segDef, new ushort[] {5}) });
    var conversion = map.Eval(result, TimeSpan.Zero, null);
    Assert.That(conversion.IsSuccess);
    var actual = conversion.Value.First().ModelValue();
    Assert.That(actual, Is.EqualTo(MeasureStatus.Failure));
  }
  
  [Test]
  public void FailedMeasureWhenResultDecoderReturnsNothing()
  {
    var segDef = new RegistersSegmentsDefinition(RegisterKind.Holding){StartAddress = 0, RegistersToRead = 1};
    var map = RegistersMap.Create()
      .RegistersSegmentsDefinitions(segDef)
      .For(_measureNode1)
      .DecodeRegisters(0, 1, (measureUrn, measureStatus, measureRegisters,
        currentTime, driverStateKeeper) => null)
      .For(_measureNode2)
      .DecodeRegisters(0, 1, GetDecoder(data => Temperature.FromFloat(data[0]*2).Value ));
    var result = ModbusRegisters.Create(new[] { new RegistersSegment(segDef, new[] { (ushort)666 }) });
    var conversion = map.Eval(result, TimeSpan.Zero, null);
    Assert.That(conversion.IsSuccess);
    Assert.That(conversion.Value.ElementAt(0).ModelValue(), Is.EqualTo(MeasureStatus.Failure));
    Assert.That(conversion.Value.ElementAt(1).ModelValue(), Is.EqualTo(Temperature.FromFloat(1332).Value));
    Assert.That(conversion.Value.ElementAt(2).ModelValue(), Is.EqualTo(MeasureStatus.Success));
  }
  
  MeasureNode<Temperature> _measureNode1 = new ("temp1", new RootModelNode("root"));
  MeasureNode<Temperature> _measureNode2 = new ("temp2", new RootModelNode("root"));

  private MeasureDecoder GetDecoder<T>(Func<ushort[], T> convert) =>
  (measureUrn, measureStatus, measureRegisters,
    currentTime, driverStateKeeper) => Result<IMeasure>
    .Create(Measure<T>.Create(measureUrn, measureStatus, Result<T>.Create(convert(measureRegisters)), currentTime));
}