using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Api.TcpModbus;

public class ModbusMapping
{
  public ModbusMapping(TcpModbusApiModuleDefinition moduleDefinition)
  {
    Presence = moduleDefinition.Presence;
    MeasuresMap = moduleDefinition.MeasuresMap;
    AlarmsMap = moduleDefinition.AlarmsMap;
    AllPropertiesUrns = MeasuresMap.Keys
      .Concat(AlarmsMap.Keys)
      .Append(Presence)
      .ToHashSet();
    RegisterToUrn = MeasuresMap.ToDictionary(
      kv => kv.Value,
      kv => new UrnSlot(kv.Key)
    );
  }

  public PropertyUrn<Presence> Presence { get; }
  public HashSet<Urn> AllPropertiesUrns { get; }
  public IReadOnlyDictionary<Urn, ushort> AlarmsMap { get; }
  public IReadOnlyDictionary<Urn, ushort> MeasuresMap { get; }
  public IReadOnlyDictionary<ushort, UrnSlot> RegisterToUrn { get; }

  public readonly struct UrnSlot
  {
    public UrnSlot(Urn urn)
    {
      Urn = urn;
      ValueType = ValueObjectsFactory.ValueTypeFromUrnType(urn.GetType());
      if (ValueType.IsAssignableTo(typeof(Enum)))
      {
        RegistersToObject = RegistersToEnum;
        Size = 1;
      }
      else if (ValueType.IsAssignableTo(typeof(IFloat)))
      {
        RegistersToObject = RegistersToFloat;
        Size = 2;
      }
      else
      {
        throw new NotSupportedException($"Unsupported type {ValueType} for urn {Urn}");
      }
    }

    public Urn Urn { get; }
    public Type ValueType { get; }
    public ushort Size { get; }
    public Func<ushort,ushort[],Result<object>> RegistersToObject { get; }
  }

  public static ushort[] ModelValueToRegisters(object modelValue)
  {
    switch (modelValue)
    {
      case IFloat @float:
        return ToUshort(@float.ToFloat());
      case Enum @enum:
        return ToUshort(Convert.ToInt16((object)@enum));
      default:
        throw new NotSupportedException();
    }
  }

  private static ushort[] ToUshort(float f)
  {
    byte[] bytes = BitConverter.GetBytes(f);
    return new ushort[2]
    {
      BitConverter.ToUInt16(bytes, 0),
      BitConverter.ToUInt16(bytes, 2)
    };
  }

  private static ushort[] ToUshort(short s) =>
    new ushort[1]
    {
      BitConverter.ToUInt16(BitConverter.GetBytes(s), 0)
    };


  public IEnumerable<object> RegistersToDataModelValues(
    ushort startAddress, ushort[] data, TimeSpan time
  )
  {
    ushort index = 0;
    while (index < data.Length)
    {
      var result = RegistersToDataModel(startAddress, index, data, time);
      if (result.IsError)
      {
        Log.Warning("Api.TcpModbus: {@msg}", result.Error.Message);
        index++;
        continue;
      }
      yield return result.Value.Instance;
      index += result.Value.Slot.Size;
    }
  }

  public Result<(UrnSlot Slot, object Instance)> RegistersToDataModel(
    ushort startAddress, ushort index, ushort[] data, TimeSpan time
  )
  {
    var registerIndex = (ushort)(startAddress + index);
    if (!RegisterToUrn.TryGetValue(registerIndex, out var urnSlot))
      return MappingError.NotFound(registerIndex);
    if(index + urnSlot.Size > data.Length)
      return MappingError.Missing(urnSlot.ValueType, data);
    var result =
      from obj in urnSlot.RegistersToObject(index, data)
      let strValue = ModelFactory.ValueAsString(obj)
      from value in ValueObjectsFactory.FromString(urnSlot.Urn.GetType(), strValue)
      from instance in ModelFactory.CreateModelInstance(urnSlot.Urn, value, time)
      select (urnSlot,instance);
    return result;
  }

  private static Result<object> RegistersToEnum(ushort index, ushort[] data) => data[index];
  private static Result<object> RegistersToFloat(ushort index, ushort[] data)
  {
    byte[] ushort1 = BitConverter.GetBytes(data[index]);
    byte[] ushort2 = BitConverter.GetBytes(data[index + 1]);
    float value = BitConverter.ToSingle(ushort1.Concat(ushort2).ToArray(), 0);
    return value;
  }
  
  public class MappingError : Error
  {
    public static MappingError NotFound(ushort address) => new($"no urn found for address {address}");

    public static MappingError Missing(Type type, IEnumerable<ushort> data) =>
      new($"missing data for type {type}");

    private MappingError(string message) : base(nameof(MappingError), message)
    {
    }
  }
}