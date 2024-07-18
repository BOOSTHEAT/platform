using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RTUModbus.Controllers.Helpers
{
    public static class Extensions
    {
        public static bool IsPresence(this DomainEvent @event, IHardwareDevice hardwareDevice, Presence presenceValue)
        {
            return @event is PropertiesChanged pc
                   && pc.ContainsProperty(hardwareDevice.presence, presenceValue);
        }

        public static bool IsTimeout(this DomainEvent @event, PropertyUrn<Duration> timerUrn)
        {
            return @event is TimeoutOccured timeoutOccured
                   && timeoutOccured.TimerUrn == timerUrn;
        }
    }
}