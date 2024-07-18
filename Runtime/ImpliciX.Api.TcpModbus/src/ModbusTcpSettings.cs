namespace ImpliciX.Api.TcpModbus
{
  public class ModbusTcpSettings
  {
      public int TCPPort { get; set; } = 5002;
      public byte SlaveId { get; set; } = 1;
  }
}