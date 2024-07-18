using NModbus;

namespace ImpliciX.RTUModbus.Controllers.Infrastructure
{
    public class ModbusLogger : IModbusLogger
    {
        public void Log(LoggingLevel _, string message) =>
            ImpliciX.Language.Core.Log.Debug($"NModbus Logger : {message}");

        public bool ShouldLog(LoggingLevel level) => true;
    }
}