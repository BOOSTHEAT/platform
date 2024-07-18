using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.TimeSeries;

internal class ModbusTimeSeriesInfo
{
  public static TimeSeriesUrn CreateUrn(ModbusSlaveTimeSeries modbusSlaveTimeSeries)
  {
    RegistersMap.Factory = () => new RegistersMapImpl();
    CommandMap.Factory = () => new CommandMapImpl();
    var slaveDefinition = modbusSlaveTimeSeries.GetDefinition();
    var registerMap = (RegistersMapImpl)slaveDefinition.ReadPropertiesMaps[MapKind.MainFirmware];
    var urn = Urn.BuildUrn(slaveDefinition.DeviceNode.Urn,slaveDefinition.Name);
    return new TimeSeriesUrn(urn, registerMap.Urns, TimeSpan.Zero);
  }

  class RegistersMapImpl : IRegistersMap
  {
    public IConversionDefinition For<T>(MeasureNode<T> node)
    {
      Urns.Add(node.measure);
      Urns.Add(node.status);
      return new ConversionDefinitionImpl(this);
    }

    public List<Urn> Urns { get; } = new();

    public IRegistersMap RegistersSegmentsDefinitions(params RegistersSegmentsDefinition[] segDef) => this;

    public IEnumerable<IConversionDefinition> Conversions { get; } = null;
    public RegistersSegmentsDefinition[] SegmentsDefinition { get; } = null;
  }

  class ConversionDefinitionImpl : IConversionDefinition
  {
    private readonly IRegistersMap _registersMap;

    public ConversionDefinitionImpl(IRegistersMap registersMap) => _registersMap = registersMap;
    public IRegistersMap DecodeRegisters((ushort startIndex, ushort count)[] slices, MeasureDecoder func) => _registersMap;

    public IRegistersMap DecodeRegisters(ushort startIndex, ushort count, MeasureDecoder func) => _registersMap;

    public Result<IMeasure> Decode(ushort[] measureRegisters, TimeSpan currentTime, IDriverStateKeeper driverStateKeeper) =>
      throw new NotSupportedException();

    public Slice[] Slices { get; }
    public Urn MeasureUrn { get; }
    public Urn StatusUrn { get; }
  }

  class CommandMapImpl : ICommandMap
  {
    public ICommandMap Add<T>(CommandNode<T> commandNode, CommandActuator actuatorFunc) => this;
    public CommandActuator ModbusCommandFactory(Urn urn) => throw new NotSupportedException();
    public bool ContainsKey(Urn commandUrn) => throw new NotSupportedException();
    public IMeasure Measure(Urn commandUrn) => throw new NotSupportedException();
  }

}