using ImpliciX.Language.Modbus;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.RTUModbus.Controllers.Tests;

[TestFixture]
public class ModbusRegistersTests
{
  [SetUp]
  public void Init()
  {
    _map = new RegistersMapImpl();
    _map.RegistersSegmentsDefinitions(
      new RegistersSegmentsDefinition(RegisterKind.Holding) { StartAddress = 0, RegistersToRead = 3 },
      new RegistersSegmentsDefinition(RegisterKind.Holding) { StartAddress = 10, RegistersToRead = 2 },
      new RegistersSegmentsDefinition(RegisterKind.Holding) { StartAddress = 20, RegistersToRead = 3 }
    );
  }

  private RegistersMapImpl _map;

  [Test]
  public void get_registers_when_registers_are_in_the_first_segment()
  {
    var segmentsReadResults = new RegistersSegment[]
    {
      new (_map.SegmentsDefinition[0], new ushort[] { 1, 5, 10 }),
      new (_map.SegmentsDefinition[1], new ushort[] { 12, 13 })
    };
    var sut = ModbusRegisters.Create(segmentsReadResults);
    var result = sut.Extract(new[] { _map.CreateSlice(1, 2) });
    var expected = new ushort[] { 5, 10 };
    Check.That(result.Value).ContainsExactly(expected);
  }

  [Test]
  public void get_registers_when_registers_are_in_the_third_segment()
  {
    var segmentsReadResults = new RegistersSegment[]
    {
      new (_map.SegmentsDefinition[0], new ushort[] { 1, 5, 10 }),
      new (_map.SegmentsDefinition[1], new ushort[] { 12, 13 }),
      new (_map.SegmentsDefinition[2], new ushort[] { 20, 21, 22 })
    };

    var sut = ModbusRegisters.Create(segmentsReadResults);
    var result = sut.Extract(new [] { _map.CreateSlice(21, 2) });
    var expected = new ushort[] { 21, 22 };
    Check.That(result.Value).ContainsExactly(expected);
  }

  [Test]
  public void get_registers_when_registers_are_in_the_first_and_third_segment()
  {
    var segmentsReadResults = new RegistersSegment[]
    {
      new (_map.SegmentsDefinition[0], new ushort[] { 1, 5, 10 }),
      new (_map.SegmentsDefinition[1], new ushort[] { 12, 13 }),
      new (_map.SegmentsDefinition[2], new ushort[] { 20, 21, 22 })
    };

    var sut = ModbusRegisters.Create(segmentsReadResults);
    var result = sut.Extract(new [] { _map.CreateSlice(1, 1), _map.CreateSlice(21, 2) });
    var expected = new ushort[] { 5, 21, 22 };
    Check.That(result.Value).ContainsExactly(expected);
  }
}