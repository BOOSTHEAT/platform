using System;
using System.Linq;
using NModbus;
using NModbus.Data;
using NModbus.Message;

namespace ImpliciX.Api.TcpModbus.Infrastructure;

public class ModbusSlaveSpy : IModbusSlave
{
  public ModbusSlaveSpy(IModbusSlave slave)
  {
    Slave = slave;
  }

  public IModbusSlave Slave { get; }
  public byte UnitId => Slave.UnitId;
  public ISlaveDataStore DataStore => Slave.DataStore;

  public IModbusMessage ApplyRequest(IModbusMessage request)
  {
    var result = Slave.ApplyRequest(request);
    HandleRequest(request);
    return result;
  }

  private void HandleRequest(IModbusMessage request)
  {
    switch (request)
    {
      case WriteMultipleRegistersRequest multiple:
      {
        NotifyRegisterWrite(multiple.StartAddress, multiple.Data);
        break;
      }
      case WriteSingleRegisterRequestResponse single:
      {
        NotifyRegisterWrite(single.StartAddress, single.Data);
        break;
      }
    }
  }

  private void NotifyRegisterWrite(ushort startAddress, RegisterCollection dataStore) =>
    DataStoreWrittenTo?.Invoke(this, (startAddress, dataStore.ToArray()));

  public event EventHandler<(
    ushort startAddress,
    ushort[] DataStore
    )> DataStoreWrittenTo;
}