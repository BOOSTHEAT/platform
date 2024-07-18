using ImpliciX.Language.Modbus;

namespace ImpliciX.RTUModbus.Controllers
{
    public class ModbusSettings
    {
        public TcpSettings TcpSettings { get; set; }
        public ModbusSlaveSettings[] Slaves { get; set; }
        public bool Buffered { get; set; } = true;
    }

    public class TcpSettings
    {
        public string IpAddress {get; set;}
        public int Port {get; set;}
    }
}