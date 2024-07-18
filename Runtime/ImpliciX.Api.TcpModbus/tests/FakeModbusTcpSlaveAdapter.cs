using System;
using System.Collections.Concurrent;

namespace ImpliciX.Api.TcpModbus.Tests
{
    public class FakeModbusTcpSlaveAdapter: IModbusTcpSlaveAdapter
    {

        private ConcurrentDictionary<ushort, ushort[]> holdingRegistersStore = new ConcurrentDictionary<ushort, ushort[]>();
        private ConcurrentDictionary<ushort, bool[]> discreteInputsStore = new ConcurrentDictionary<ushort, bool[]>();

        public event EventHandler<(ushort startAddress, ushort[] data)> OnHoldingRegisterUpdate;

        public void Start()
        {
            HasBeenStarted = true;
        }

        public bool HasBeenStarted { get; set; }

        public void WriteInHoldingRegister(ushort register, ushort[] value)
        {
            holdingRegistersStore.AddOrUpdate(register, _=>value,(_,__)=>value);
        }

        public void Stop()
        {
            HasBeenStopped = true;
        }

        public void WriteInDiscreteInputs(ushort register, bool[] value)
        {
            discreteInputsStore.AddOrUpdate(register, _=>value,(_,__)=>value);
        }

        public bool HasBeenStopped { get; set; }

        public ushort[] GetHoldingRegisterValue(ushort register)
        {
            ushort[] value;
            holdingRegistersStore.TryGetValue(register, out value);
            return value;
        }

        public bool[] GetDiscreteInputValue(ushort register)
        {
            
            discreteInputsStore.TryGetValue(register, out var value);
            return value;
        }
        public void Dispose()
        {
            return;
        }

    }
}