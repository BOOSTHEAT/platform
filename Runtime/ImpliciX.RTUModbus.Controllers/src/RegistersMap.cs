using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using Debug = System.Diagnostics.Debug;

namespace ImpliciX.RTUModbus.Controllers;

public class RegistersMapImpl : IRegistersMap
{
  private readonly List<ConversionDefinition> _conversionDefinitions;

  public RegistersMapImpl()
  {
    _conversionDefinitions = new List<ConversionDefinition>();
    SegmentsDefinition = Array.Empty<RegistersSegmentsDefinition>();
  }

  public IConversionDefinition For<T>(MeasureNode<T> node) =>
    new ConversionDefinition(this, node.measure, node.status);

  public IEnumerable<IConversionDefinition> Conversions => _conversionDefinitions;

  internal IRegistersMap AddMapping(ConversionDefinition decodeMapping)
  {
    _conversionDefinitions.Add(decodeMapping);
    return this;
  }

  public IRegistersMap RegistersSegmentsDefinitions(params RegistersSegmentsDefinition[] segDef)
  {
    SegmentsDefinition = segDef;
    return this;
  }
  
  public RegistersSegmentsDefinition[] SegmentsDefinition { get; private set; }
  
  public Slice CreateSlice(ushort start, ushort count)
  {
    var segDef = SegmentsDefinition
      .Select((sd,idx) => (sd,idx))
      .FirstOrDefault(x => start >= x.sd.StartAddress && start+count <= x.sd.StartAddress + x.sd.RegistersToRead);
    if (segDef == default)
      throw new ArgumentException($"Data at {start} with length {count} cannot be read in segments definitions");
    var dataIndex = start - segDef.sd.StartAddress;
    return new Slice((ushort)segDef.idx, (ushort)dataIndex, start, count);
  }

}

internal class ConversionDefinition : IConversionDefinition
{
  internal ConversionDefinition(RegistersMapImpl map, Urn measureUrn,
    Urn? measureStatusUrn = null)
  {
    Map = map;
    MeasureUrn = measureUrn;
    Debug.Assert(measureStatusUrn != null, nameof(measureStatusUrn) + " != null");
    StatusUrn = measureStatusUrn;
    _decodeFunc = (_, _, _, _, _) => Result<IMeasure>.Create(new Error(nameof(ConversionDefinition),"Undefined decoder"));
    Slices = Array.Empty<Slice>();
  }

  public IRegistersMap DecodeRegisters((ushort startIndex, ushort count)[] slices,
    MeasureDecoder func)
  {
    Debug.Assert(func != null, nameof(func) + " != null");
    Array.Sort(slices, (a, b) => a.startIndex - b.startIndex);
    Slices = slices.Select(x => Map.CreateSlice(x.startIndex, x.count)).ToArray();
    _decodeFunc = func;
    return Map.AddMapping(this);
  }

  public IRegistersMap DecodeRegisters(ushort startIndex, ushort count, MeasureDecoder func)
  {
    Debug.Assert(func != null, nameof(func) + " != null");
    Slices = new [] { Map.CreateSlice(startIndex, count) };
    _decodeFunc = func;
    return Map.AddMapping(this);
  }

  public Result<IMeasure> Decode(ushort[] measureRegisters, TimeSpan currentTime, IDriverStateKeeper driverStateKeeper)
    => _decodeFunc(MeasureUrn, StatusUrn, measureRegisters, currentTime, driverStateKeeper);

  private MeasureDecoder _decodeFunc;
  public Slice[] Slices { get; private set; }
  private RegistersMapImpl Map { get; }
  public Urn MeasureUrn { get; }
  public Urn StatusUrn { get; }
}
