using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ImpliciX.MmiHost.DBusProxies
{
    [DBusInterface("de.pengutronix.rauc.Installer")]
    public interface IRaucInstallProxy : IDBusObject
    {
        public const string ServiceName = "de.pengutronix.rauc";
        public const string Path = "/";

        Task InstallBundleAsync(string Source, IDictionary<string, object> Args);
        Task<T> GetAsync<T>(string prop);
    }
}