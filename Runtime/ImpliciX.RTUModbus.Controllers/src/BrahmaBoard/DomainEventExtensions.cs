using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RTUModbus.Controllers.BrahmaBoard
{
    internal static class DomainEventExtensions
    {
        public static bool IsSupply(this DomainEvent @this, BurnerNode genericBurner,PowerSupply powerSupply) =>
            @this is CommandRequested cr &&
            cr.Urn.Equals(genericBurner._supply) &&
            cr.Arg.Equals(powerSupply);
        
       public static bool IsStartIgnition(this DomainEvent @this, BurnerNode genericBurner) =>
            @this is CommandRequested cr &&
            cr.Urn.Equals(genericBurner._start_ignition);
       
       public static bool IsFanThrottle(this DomainEvent @this, BurnerNode genericBurner) =>
           @this is CommandRequested cr &&
           cr.Urn.Equals(genericBurner.burner_fan._throttle);
       
       public static bool IsStopIgnition(this DomainEvent @this, BurnerNode genericBurner) =>
           @this is CommandRequested cr &&
           cr.Urn.Equals(genericBurner._stop_ignition);

        public static bool IsNotFaultedDetected(this DomainEvent @this, BurnerNode genericBurner) =>
            @this is NotFaultedDetected notFaultedDetected &&
            notFaultedDetected.GenericBurner.Equals(genericBurner);

        public static bool IsFaultedDetected(this DomainEvent @this, BurnerNode genericBurner) =>
            @this is FaultedDetected faultedDetected &&
            faultedDetected.GenericBurner.Equals(genericBurner);

        public static bool IsManualResetting(this DomainEvent @this, BurnerNode genericBurner) =>
            @this is CommandRequested cr &&
            cr.Urn.Equals(genericBurner._manual_reset);
    }
}