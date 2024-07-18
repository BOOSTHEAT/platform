using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace ImpliciX.MmiHost.DBusProxies
{
    [DBusInterface("org.freedesktop.systemd1.Manager")]
    interface IRestartUnitProxy : IDBusObject
    {
        Task<ObjectPath> RestartUnitAsync(string Name, string Mode);
       
        public const string ServiceName = "org.freedesktop.systemd1";
        public const string Path = "/org/freedesktop/systemd1";
    }
}