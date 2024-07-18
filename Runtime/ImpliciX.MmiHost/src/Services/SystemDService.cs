using ImpliciX.Language.Core;
using ImpliciX.MmiHost.DBusProxies;
using Tmds.DBus;
using static ImpliciX.MmiHost.Constants;


namespace ImpliciX.MmiHost.Services
{
    public static class SystemDService
    {
        public static void RestartBoilerAppUnit()
        {
            SideEffect.TryRun(() =>
            {
                var systemdManager = Connection.System.CreateProxy<IRestartUnitProxy>(IRestartUnitProxy.ServiceName, IRestartUnitProxy.Path);
                systemdManager.RestartUnitAsync(BOILER_APP_UNIT_NAME, "replace").GetAwaiter().GetResult();
            }, exception => Log.Error(exception, $"Error during restart of boiler_app unit"));
        }
    }
}