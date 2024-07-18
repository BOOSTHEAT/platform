using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;

namespace ImpliciX.RTUModbus.Controllers;

public class RegistersSegment
{
  public bool IsValid { get; }
  public ushort[] Data { get; }
  public int Length => Data.Length;

  public RegistersSegment(RegistersSegmentsDefinition segDef, ushort[] data)
  {
    IsValid = segDef.RegistersToRead == data.Length;
    Data = data;
  }

  public Result<ushort[]> GetRegisters(int fromIndex, int count) =>
    IsValid ? Data.Skip(fromIndex).Take(count).ToArray() : IncompleteSegment;

  private static readonly Result<ushort[]> IncompleteSegment =
    Result<ushort[]>.Create(new DecodeError("incomplete segment"));
}