using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Modbus;

namespace ImpliciX.RTUModbus.Controllers
{
  public class ModbusRegisters
  {
    private readonly RegistersSegment[] _successfulSegments;

    public static ModbusRegisters Create(RegistersSegment[] segments) => new (segments);
    
    private ModbusRegisters(RegistersSegment[] segments) => _successfulSegments = segments;

    public override string ToString() =>
      _successfulSegments
        .SelectMany(slice => slice.Data)
        .Aggregate("", (current, v) => current + v);

    public Result<ushort[]> Extract(Slice[] slices) =>
      slices
        .Select(slice =>
          _successfulSegments[slice.SegmentIndex].GetRegisters(slice.DataIndexInSegment, slice.RegisterCount))
        .Traverse()
        .Select(groupsOfRegisters => groupsOfRegisters.SelectMany(x => x))
        .Select(registers => registers.ToArray());
  }
}