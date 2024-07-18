using System;
using System.Collections.Generic;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.Language.StdLib;
using ImpliciX.ReferenceApp.Model;
using ImpliciX.ReferenceApp.Model.Tree;

namespace ImpliciX.ReferenceApp.App;

public class SomeSlave
{
  private static readonly HardwareDeviceNode HwSwDeviceNode = device._.other;


  public static readonly Func<ModbusSlaveDefinition> Definition = () => new(HwSwDeviceNode, SlaveKind.Vendor)
  {
    Name = nameof(SomeSlave),
    SettingsUrns = new Urn[]
    {
      HwSwDeviceNode.presence,
    },
    ReadPropertiesMaps = new Dictionary<MapKind, IRegistersMap>()
    {
      [MapKind.MainFirmware] =
      // @formatter:off
      RegistersMap.Create()
          .RegistersSegmentsDefinitions(new RegistersSegmentsDefinition(RegisterKind.Holding){StartAddress = 0, RegistersToRead = 6})
          .For(monitoring.modboss.temperature).DecodeRegisters(0, 2, DebuggingDecoder.FloatMswLast<Temperature>())
          .For(monitoring.modboss.percentage).DecodeRegisters(2, 2, DebuggingDecoder.FloatMswLast<Percentage>())
          .For(monitoring.modboss.value1).DecodeRegisters(4, 1, EnumDecoder<SomeEnum>())
          .For(monitoring.modboss.value2).DecodeRegisters(5, 1, EnumDecoder<SomeEnum>())
      // @formatter:on
    },
    CommandMap = CommandMap.Empty()
  };
  
  public static MeasureDecoder EnumDecoder<T>() where T : struct, Enum => 
    (measureUrn, statusUrn, registers, currentTime, _) => 
      Measure<T>.Create(measureUrn, statusUrn, Enum.GetValues<T>()[registers[0]] , currentTime);
  
}