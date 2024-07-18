using System;
using System.Net.Sockets;
using ImpliciX.Language.Modbus;
using NModbus;
using Serilog;

namespace ImpliciX.RTUModbus.Controllers.Infrastructure
{
    public class ModbusAdapterTcp : IModbusAdapter
    {
        private readonly TcpSettings _tcpSettings;
        private readonly ModbusSlaveSettings _slaveSettings;

        public static ModbusAdapterTcp Create(TcpSettings tcpSettings, ModbusSlaveSettings slaveSettings)
        {
            return new ModbusAdapterTcp(tcpSettings, slaveSettings);
        }

        public ushort[] ReadRegisters(string _, RegisterKind kind, ushort startAddress, ushort registersToRead)
        {
            try
            {
                return kind switch
                {
                    RegisterKind.Input => Master.ReadInputRegisters(_slaveSettings.Id, startAddress, registersToRead),
                    RegisterKind.Holding => Master.ReadHoldingRegisters(_slaveSettings.Id, startAddress, registersToRead),
                    _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
                };
            }
            catch (Exception)
            {
                ResetCommunicationContext();
                throw;
            }
        }

        public void WriteRegisters(string _, ushort startAddress, ushort[] registersToWrite)
        {
            try
            {
                Master.WriteMultipleRegisters(_slaveSettings.Id, startAddress, registersToWrite);
            }
            catch (Exception)
            {
                ResetCommunicationContext();
                throw;
            }
        }


        private const int OverrideNModbusRetryPolicy = 0;
        private IModbusMaster _master;
        private TcpClient _tcpClient;

        private ModbusAdapterTcp(TcpSettings tcpSettings, ModbusSlaveSettings slaveSettings)
        {
            _tcpSettings = tcpSettings;
            _slaveSettings = slaveSettings;
            CreateCommunicationContext();
        }

        private void CreateCommunicationContext()
        {
            _tcpClient = new TcpClient(_tcpSettings.IpAddress, _tcpSettings.Port);
            _tcpClient.SendTimeout = _slaveSettings.TimeoutSettings.Timeout;
            _tcpClient.ReceiveTimeout = _slaveSettings.TimeoutSettings.Timeout;
            var factory = new ModbusFactory(null, true, new ModbusLogger());
            _master = factory.CreateMaster(_tcpClient);
            _master.Transport.Retries = OverrideNModbusRetryPolicy;
        }

        private void ResetCommunicationContext()
        {
            Log.Debug("Reset communication context for slave {@slave}.", _slaveSettings.Id);
            _tcpClient.Dispose();
            _master.Dispose();
            CreateCommunicationContext();
        }

        private IModbusMaster Master => _master;

    }

    
}