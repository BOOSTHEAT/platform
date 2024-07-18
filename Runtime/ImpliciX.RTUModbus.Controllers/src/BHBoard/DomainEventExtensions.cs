using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RTUModbus.Controllers.BHBoard
{
    internal static class DomainEventExtensions
    {
        public static bool IsProtocolErrorOccured(this DomainEvent @event) => @event is ProtocolErrorOccured;

        public static bool IsExitBootloaderCommandSucceeded(this DomainEvent @event) =>
            @event is ExitBootloaderCommandSucceeded;

        internal static bool IsSystemTicked(this DomainEvent @event) => @event is SystemTicked;

        internal static bool IsFirmwareUpdateCommand(this DomainEvent @event) => @event is CommandRequested
        {
            Urn: CommandUrn<PackageContent> _
        };

        internal static bool Is<T>(this DomainEvent @event) => @event is T;

        internal static bool IsCommand(this DomainEvent @event, CommandNode<NoArg> cmd) =>
            @event is CommandRequested commandRequested
            && commandRequested.Urn.Equals(cmd.command);

        internal static bool IsRegulationEntered(this DomainEvent @event) => @event is RegulationEntered;
        internal static bool IsRegulationExited(this DomainEvent @event) => @event is RegulationExited;
    }
}