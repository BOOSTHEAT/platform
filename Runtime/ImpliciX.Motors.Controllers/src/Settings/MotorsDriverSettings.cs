using ImpliciX.SharedKernel.Serial;

namespace ImpliciX.Motors.Controllers.Settings
{
    public class MotorsDriverSettings
    {
        public int ReadPaceInSystemTicks { get; set; }
        public SerialPortSettings SerialPortSettings { get; set; }
        public string Factory { get; set; }
        public TimeoutSettings TimeoutSettings { get; set; }
    }
}