using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using NUnit.Framework;

namespace ImpliciX.Api.TcpModbus.Tests;

public class ModbusMappingTests
{
  [Test]
  public void AllMappedUrnsAreAvailable()
  {
    Assert.That(ModbusMapping.AllPropertiesUrns, Is.EqualTo(new Urn[]
    {
      test_model.temp1.measure,
      test_model.temp2.measure,
      test_model.temp3.measure,
      test_model.dummy,
      MetricUrn.BuildDuration(test_model.counter1, DummyState.B.ToString()),
      MetricUrn.BuildDuration(test_model.counter1, DummyState.A.ToString()),
      test_model.c001,
      test_model.c002,
      test_model.presence,
    }));
  }
  
  [Test]
  public void ReverseUrnMappingIsAvailable()
  {
    Assert.That(ModbusMapping.RegisterToUrn, Is.EqualTo(new Dictionary<ushort,ModbusMapping.UrnSlot>
    {
      [1] = new (test_model.temp1.measure),
      [3] = new (test_model.temp2.measure),
      [5] = new (test_model.temp3.measure),
      [7] = new (test_model.dummy),
      [20] = new (MetricUrn.BuildDuration(test_model.counter1, DummyState.B.ToString())),
      [22] = new (MetricUrn.BuildDuration(test_model.counter1, DummyState.A.ToString())),
    }));
  }
  
  private static readonly ModbusMapping ModbusMapping = new ModbusMapping(new TcpModbusApiModuleDefinition()
  {
    Presence = test_model.presence,
    MeasuresMap = new Dictionary<Urn, ushort>
    {
      { test_model.temp1.measure, 1 },
      { test_model.temp2.measure, 3 },
      { test_model.temp3.measure, 5 },
      { test_model.dummy, 7 },
      { MetricUrn.BuildDuration(test_model.counter1, DummyState.B.ToString()), 20 },
      { MetricUrn.BuildDuration(test_model.counter1, DummyState.A.ToString()), 22 },
    },
    AlarmsMap = new Dictionary<Urn, ushort>
    {
      { test_model.c001, 11 },
      { test_model.c002, 12 }
    },
  });

  private static (object, ushort, ushort[])[] _encoded = new (object, ushort, ushort[])[]
  {
    (Temperature.Create(20), 5, new ushort[] {0,16800}),
    (Temperature.Create(30), 5, new ushort[] {0,16880}),
    (Temperature.Create(9850), 5, new ushort[] {59392, 17945}),
    (Temperature.Create(0.156f), 5, new ushort[] {48759, 15903}),
    (Temperature.Create(0.755f), 5, new ushort[] {18350, 16193}),
    (DummyState.A, 7, new [] {(ushort)DummyState.A}),
    (DummyState.B, 7, new [] {(ushort)DummyState.B}),
  };
  
  [TestCaseSource(nameof(_encoded))]
  public void ModelValueToRegisters((object modelValue, ushort _, ushort[] expectedRegisters) _)
  {
    Assert.That(ModbusMapping.ModelValueToRegisters(_.modelValue), Is.EqualTo(_.expectedRegisters));
  }
  
  [Test]
  public void RegistersNotFoundToDataModelValue()
  {
    var result = ModbusMapping.RegistersToDataModel(254, 0, new ushort[]{0,0}, TimeSpan.Zero);
    Assert.True(result.IsError);
    Assert.That(result.Error.Message, Is.EqualTo("MappingError:no urn found for address 254"));
  }
  
  [Test]
  public void RegistersMissingDataToDataModelValue()
  {
    var result = ModbusMapping.RegistersToDataModel(3, 0, new ushort[]{0}, TimeSpan.Zero);
    Assert.True(result.IsError);
    Assert.That(result.Error.Message, Is.EqualTo("MappingError:missing data for type ImpliciX.Language.Model.Temperature"));
  }
  
  [TestCaseSource(nameof(_encoded))]
  public void RegistersToDataModelValue((object expectedModelValue, ushort address, ushort[] data) _)
  {
    var time = new TimeSpan(Random.Shared.NextInt64());
    var result = ModbusMapping.RegistersToDataModel(_.address, 0, _.data, time);
    Assert.True(result.IsSuccess);
    var dmv = (IDataModelValue)result.Value.Instance;
    Assert.That(dmv.Urn, Is.EqualTo(ModbusMapping.RegisterToUrn[_.address].Urn));
    Assert.That(dmv.ModelValue(), Is.EqualTo(_.expectedModelValue));
    Assert.That(dmv.At, Is.EqualTo(time));
  }
  
  [TestCaseSource(nameof(_encoded))]
  public void RegistersAtIndexToDataModelValue((object expectedModelValue, ushort address, ushort[] data) _)
  {
    var time = new TimeSpan(Random.Shared.NextInt64());
    var moreData = (new ushort[] { 0, 0, 0 }.Concat(_.data)).ToArray();
    var result = ModbusMapping.RegistersToDataModel((ushort)(_.address-3), 3, moreData, time);
    Assert.True(result.IsSuccess);
    var dmv = (IDataModelValue)result.Value.Instance;
    Assert.That(dmv.Urn, Is.EqualTo(ModbusMapping.RegisterToUrn[_.address].Urn));
    Assert.That(dmv.ModelValue(), Is.EqualTo(_.expectedModelValue));
    Assert.That(dmv.At, Is.EqualTo(time));
  }
  
  [Test]
  public void RegistersToMultipleDataModelValues()
  {
    var time = new TimeSpan(Random.Shared.NextInt64());
    var values = ModbusMapping.RegistersToDataModelValues(
      3, new ushort[] { 59392, 17945, 59292, 17946, (ushort)DummyState.A }, time
    ).ToArray();
    Assert.That(values.Length, Is.EqualTo(3));
    
    var dmv0 = (IDataModelValue)values[0];
    Assert.That(dmv0.Urn, Is.EqualTo(test_model.temp2.measure));
    Assert.That(dmv0.ModelValue(), Is.EqualTo(Temperature.Create(9850)));
    Assert.That(dmv0.At, Is.EqualTo(time));
    
    var dmv1 = (IDataModelValue)values[1];
    Assert.That(dmv1.Urn, Is.EqualTo(test_model.temp3.measure));
    Assert.That(dmv1.ModelValue(), Is.EqualTo(Temperature.Create(9913.90234f)));
    Assert.That(dmv1.At, Is.EqualTo(time));

    var dmv2 = (IDataModelValue)values[2];
    Assert.That(dmv2.Urn, Is.EqualTo(test_model.dummy));
    Assert.That(dmv2.ModelValue(), Is.EqualTo(DummyState.A));
    Assert.That(dmv2.At, Is.EqualTo(time));
  }
  
  [Test]
  public void RegistersToMultipleDataModelValuesCanSkipWrongAddresses()
  {
    var time = new TimeSpan(Random.Shared.NextInt64());
    var values = ModbusMapping.RegistersToDataModelValues(
      2, new ushort[] { 0, 59392, 17945, 59292, 17946, (ushort)DummyState.A, 0 }, time
    ).ToArray();
    Assert.That(values.Length, Is.EqualTo(3));
    
    var dmv0 = (IDataModelValue)values[0];
    Assert.That(dmv0.Urn, Is.EqualTo(test_model.temp2.measure));
    Assert.That(dmv0.ModelValue(), Is.EqualTo(Temperature.Create(9850)));
    Assert.That(dmv0.At, Is.EqualTo(time));
    
    var dmv1 = (IDataModelValue)values[1];
    Assert.That(dmv1.Urn, Is.EqualTo(test_model.temp3.measure));
    Assert.That(dmv1.ModelValue(), Is.EqualTo(Temperature.Create(9913.90234f)));
    Assert.That(dmv1.At, Is.EqualTo(time));

    var dmv2 = (IDataModelValue)values[2];
    Assert.That(dmv2.Urn, Is.EqualTo(test_model.dummy));
    Assert.That(dmv2.ModelValue(), Is.EqualTo(DummyState.A));
    Assert.That(dmv2.At, Is.EqualTo(time));
  }

}