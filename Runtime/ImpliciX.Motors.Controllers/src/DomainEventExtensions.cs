using ImpliciX.Driver.Common;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Motors.Controllers
{
    public static class DomainEventExtensions
    {
        public static bool isPresence(this DomainEvent @event, Presence presence,
            UserSettingUrn<Presence> presenceUrn) =>
            @event is PropertiesChanged pc &&
            pc.ContainsProperty(presenceUrn, presence);

        public static bool IsSlaveCommand(this DomainEvent @event, IBoardSlave slave)
        {
            return @event is CommandRequested cr && slave.IsConcernedByCommandRequested(cr.Urn);
        }
        public static bool Is<T>(this DomainEvent @event) => @event is T;
    }
}