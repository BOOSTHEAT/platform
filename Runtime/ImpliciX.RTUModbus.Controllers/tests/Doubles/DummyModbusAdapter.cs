using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Modbus;

namespace ImpliciX.RTUModbus.Controllers.Tests.Doubles
{
    public class DummyModbusAdapter : IModbusAdapter
    {
        private readonly (ushort startAddress, object simulatedOutcome)[] _readSimulations;
        private readonly (ushort startAddress, object simulatedOutcome)[] _writeSimulations;

        public DummyModbusAdapter(
            (ushort startAddress, object simulatedOutcome)[] readSimulations = null,
            (ushort startAddress, object simulatedOutcome)[] writeSimulations = null)
        {
            _readSimulations = readSimulations;
            _writeSimulations = writeSimulations;
            Writes = new List<(ushort startAddress, ushort[] registersToWrite)>();
        }

        public int WriteCount => Writes.Count;
        public List<(ushort startAddress, ushort[] registersToWrite)> Writes { get; }

        public ushort[] ReadRegisters(string factoryName, RegisterKind kind, ushort startAddress, ushort registersToRead)
        {
            var outcome = _readSimulations.Single(s=>s.startAddress==startAddress).simulatedOutcome;
            if (outcome is Exception boom)
                throw boom;
            return (ushort[])outcome;
        }

        public void WriteRegisters(string factoryName, ushort startAddress, ushort[] registersToWrite)
        {
            Writes.Add((startAddress, registersToWrite));
            var outcome = _writeSimulations.Single(s=>s.startAddress==startAddress).simulatedOutcome;
            if (outcome is Exception boom)
                throw boom;
        }
    }
}