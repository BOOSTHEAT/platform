using ImpliciX.Language.Core;
using ImpliciX.MmiHost.DBusProxies;
using Tmds.DBus;

namespace ImpliciX.MmiHost.Services
{
    public static class SystemService
    {

        public static void RestartSystem()
        {
            SideEffect.TryRun(() =>
            {
                var system = Connection.System.CreateProxy<IRebootOsProxy>(IRebootOsProxy.ServiceName, IRebootOsProxy.Path);
                system.RebootAsync(true).GetAwaiter().GetResult();
            }, exception => Log.Error(exception, $"Error during restart of the system"));
        }
    }
}