using ImpliciX.Language.Model;

namespace ImpliciX.Motors.Controllers.Definitions
{
    public class MotorsSlaveDefinition
    {
        public MotorsSlaveDefinition(DeviceNode deviceNode, RegistersMap registersMap, CommandMap commandMap, Urn[] settingsUrn)
        {
            DeviceNode = deviceNode;
            RegistersMap = registersMap;
            CommandMap = commandMap;
            SettingsUrn = settingsUrn;
        }

        public DeviceNode DeviceNode { get; }
        public RegistersMap RegistersMap { get; }
        public CommandMap CommandMap { get; }
        public Urn[] SettingsUrn { get; }
    }
}