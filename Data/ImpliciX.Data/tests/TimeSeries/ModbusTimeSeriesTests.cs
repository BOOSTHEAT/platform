using System;
using System.Collections.Generic;
using ImpliciX.Data.TimeSeries;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.Language.StdLib;
using NUnit.Framework;

namespace ImpliciX.Data.Tests.TimeSeries;

public class ModbusTimeSeriesTests
{
  private Func<ModbusSlaveDefinition> _slave;

  [Test]
  public void GetInfosForTimeSeriesDefinedWithModbusRegisterMap()
  {
    Init();
    var urn = _slave.TimeSeries().Urn();
    Assert.That(urn, Is.EqualTo(Urn.BuildUrn("root", "my_device", "Slave1")));
    Assert.That(urn.Members, Is.EqualTo( new []
    {
      Urn.BuildUrn("root", "property1", "measure"),
      Urn.BuildUrn("root", "property1", "status"),
      Urn.BuildUrn("root", "property2", "measure"),
      Urn.BuildUrn("root", "property2", "status"),
    }));
    Assert.That(urn.Retention, Is.EqualTo(TimeSpan.Zero));
  }

  [Test]
  public void GetInfosForTimeSeriesWithRetentionDefinedWithModbusRegisterMap()
  {
    Init();
    var urn = _slave.TimeSeries().Over.ThePast(5).Days.Urn();
    Assert.That(urn, Is.EqualTo(Urn.BuildUrn("root", "my_device", "Slave1")));
    Assert.That(urn.Members, Is.EqualTo( new []
    {
      Urn.BuildUrn("root", "property1", "measure"),
      Urn.BuildUrn("root", "property1", "status"),
      Urn.BuildUrn("root", "property2", "measure"),
      Urn.BuildUrn("root", "property2", "status"),
    }));
    Assert.That(urn.Retention, Is.EqualTo(TimeSpan.FromDays(5)));
  }

  [SetUp]
  public void Init()
  {
    var root = new RootModelNode("root");
    var property1 = new MeasureNode<Temperature>("property1", root);
    var property2 = new MeasureNode<Pressure>("property2", root);
    var deviceNode = new HardwareDeviceNode("my_device", root);
    var cmd1 = CommandNode<NoArg>.Create("cmd1", root);
    _slave = () => new ModbusSlaveDefinition(deviceNode)
    {
      Name = "Slave1",
      SettingsUrns = new Urn[] { deviceNode.presence },
      ReadPropertiesMaps = new Dictionary<MapKind, IRegistersMap>()
      {
        [MapKind.MainFirmware] =
          // @formatter:off
          RegistersMap.Create()
            .RegistersSegmentsDefinitions(new RegistersSegmentsDefinition(RegisterKind.Holding){StartAddress = 0, RegistersToRead = 4})
            .For(property1).DecodeRegisters(0, 2, DebuggingDecoder.FloatMswLast<Temperature>())
            .For(property2).DecodeRegisters(2, 2, DebuggingDecoder.FloatMswLast<Temperature>())
        // @formatter:on
      },
      CommandMap = CommandMap.Empty()
        .Add(cmd1,null),
    };
  }
}