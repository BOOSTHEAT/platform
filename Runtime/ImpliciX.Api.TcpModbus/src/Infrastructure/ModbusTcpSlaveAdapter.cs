#nullable enable
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ImpliciX.Language.Core;
using NModbus;

namespace ImpliciX.Api.TcpModbus.Infrastructure;

public class ModbusTcpSlaveAdapter : IModbusTcpSlaveAdapter
{
  private readonly ModbusTcpSettings _settings;
  private readonly TcpListener _tcpListener;
  private readonly IModbusSlaveNetwork _modbusSlaveNetwork;
  private CancellationTokenSource? _cancellationTokenSource;
  private readonly ModbusSlaveSpy _slaveSpy;
  private Task? _runner;

  public event EventHandler<(ushort startAddress, ushort[] data)> OnHoldingRegisterUpdate;

  public ModbusTcpSlaveAdapter(ModbusTcpSettings settings)
  {
    if (settings.TCPPort <= 1024)
      Log.Warning(
        $"Api.ModbusTcp : the TCP port is {settings.TCPPort} (<= 1024) this requires to execute the process as root.");

    _settings = settings;
    _tcpListener = new TcpListener(IPAddress.Any, settings.TCPPort);

    var factory = new ModbusFactory();
    _modbusSlaveNetwork = factory.CreateSlaveNetwork(_tcpListener);
    _slaveSpy = new ModbusSlaveSpy(factory.CreateSlave(settings.SlaveId));

    _modbusSlaveNetwork.AddSlave(_slaveSpy);
    _runner = default;
    _slaveSpy.DataStoreWrittenTo += (sender, args) => { OnHoldingRegisterUpdate?.Invoke(this, args); };
  }

  public void Start()
  {
    if (_runner is null || _runner.Status != TaskStatus.Running)
    {
      _cancellationTokenSource = new CancellationTokenSource();
      _runner = Task.Run(() => _modbusSlaveNetwork.ListenAsync(_cancellationTokenSource.Token),
        _cancellationTokenSource.Token);
    }

    Log.Information("Api Modbus TCP Started on port {@port}, slave id {@slaveId}", _settings.TCPPort,
      _settings.SlaveId);
  }

  public void Stop()
  {
    _cancellationTokenSource?.Cancel();
    Log.Information("Api Modbus TCP Stopped");
  }

  public void Dispose()
  {
    _cancellationTokenSource?.Cancel();
    _tcpListener.Stop();
    _modbusSlaveNetwork.Dispose();
  }

  public void WriteInHoldingRegister(ushort register, ushort[] value)
  {
    _slaveSpy.DataStore.HoldingRegisters.WritePoints(register, value);
  }
    
  public void WriteInDiscreteInputs(ushort register, bool[] value)
  {
    _slaveSpy.DataStore.CoilDiscretes.WritePoints(register, value);
  }
}