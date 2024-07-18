#nullable enable
using System;

namespace ImpliciX.Api.TcpModbus;

public interface IModbusTcpSlaveAdapter : IDisposable
{
  public void Start();
  void WriteInHoldingRegister(ushort register, ushort[] value);
  void Stop();
  void WriteInDiscreteInputs(ushort register, bool[] value);
  event EventHandler<(ushort startAddress, ushort[] data)> OnHoldingRegisterUpdate;
}