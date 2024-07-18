using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Driver.Common.State
{
    public static class SettingsStorage
    {
        public static void Store(Urn[] settingsUrns, DriverStateKeeper driverStateKeeper, PropertiesChanged propertiesChanged)
        {
            foreach (var settingsUrn in settingsUrns)
            {
                var _ =
                    from oldState in driverStateKeeper.TryRead(settingsUrn)
                    from value in propertiesChanged.GetPropertyValue(settingsUrn).ToResult("setting not found")
                    let newState = oldState.WithValue("value", value)
                    select driverStateKeeper.TryUpdate(newState);
            }
        }
    }
}