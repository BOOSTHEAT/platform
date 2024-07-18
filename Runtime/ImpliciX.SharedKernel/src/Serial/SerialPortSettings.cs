namespace ImpliciX.SharedKernel.Serial
{
    public class SerialPortSettings
    {
        public string SerialPort { get; set; }
        public int BaudRate { get; set; }
        public string Parity { get; set; }
        public string StopBits { get; set; }
        public int DataBits { get; set; }
    }
}