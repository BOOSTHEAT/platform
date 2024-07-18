using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Driver.Common
{
    public interface IBoardSlave
    {
        Result2<IDataModelValue[], CommunicationDetails> ExecuteCommand(Urn commandUrn, object arg);

        Result2<IDataModelValue[], CommunicationDetails> ReadProperties(MapKind mapKind); 

        bool IsConcernedByCommandRequested(Urn crUrn) => false;
    
        uint ReadPaceInSystemTicks { get; }
    
        DeviceNode DeviceNode { get; }
        HardwareAndSoftwareDeviceNode HardwareAndSoftwareDeviceNode => DeviceNode as HardwareAndSoftwareDeviceNode;
        IHardwareDevice HardwareDevice => DeviceNode as IHardwareDevice;
        string Name { get; }
        Urn[] SettingsUrns { get; }
    }
}