using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]

namespace ImpliciX.MmiHost.DBusProxies
{
    [DBusInterface("de.pengutronix.rauc.Installer")]
    public interface IRaucSlotProxy : IDBusObject
    {
        public const string SlotIdentifierBooted = "booted";
        public const string SlotIdentifierOther = "other";
        public const string ServiceName = "de.pengutronix.rauc";
        public const string Path = "/";
        public const string MarkStateGood = "good";
        public const string MarkStateActive = "active";
        
        Task<(string slotName, string message)> MarkAsync(string state, string slotIdentifier);
        Task<string> GetPrimaryAsync();
    }
}

