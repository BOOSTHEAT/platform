using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace ImpliciX.MmiHost.DBusProxies
{
    [DBusInterface("org.freedesktop.login1.Manager")]
    interface IRebootOsProxy : IDBusObject
    {
        public const string ServiceName = "org.freedesktop.login1";
        public const string Path = "/org/freedesktop/login1";

        Task RebootAsync(bool interactive);
    }
}