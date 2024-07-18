using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.SystemSoftware.States
{
    public class Updating : BaseState<Context>
    {
        protected override DomainEvent[] OnEntry(Context context, DomainEvent @event)
        {
            var cmdRequested = (CommandRequested)@event;
            var packageLocation = (PackageLocation)@cmdRequested.Arg;
            Log.Information("[SystemSoftware] is updating using package {@package}.", packageLocation.Value);
            return
                (
                    from package in _loader(packageLocation, k => _moduleDefinition.SoftwareMap[k])
                    let _ = SelectSoftwareToUpdate(package, context)
                    from __ in CheckApplicationPackageAllowed(_moduleDefinition.IsPackageAllowedForUpdate, package.ApplicationName)
                    from cmdMeasure in _domainEventFactory.NewEventResult(_moduleDefinition.GeneralUpdateCommand.measure, packageLocation)
                    from devicesUpdateCommands in BuildUpdateCommands(package)
                    from updateProgress in InitializeUpdateProgress(package)
                    let ___ = context.SetCurrentUpdatePackage(package)
                    select devicesUpdateCommands.Prepend(cmdMeasure).Append(updateProgress).ToArray()
                )
                .Match(
                    err =>
                    {
                        Log.Error("[SystemSoftware] Error occured and update could not start. {@Error}", err.Message);
                        return new DomainEvent[] { new UpdateCanceled(TimeSpan.Zero) };
                    },
                    @events =>
                    {
                        if (!@events.Any(@evt => @evt is CommandRequested))
                        {
                            Log.Information(
                                "[SystemSoftware] Nothing to update. All software devices current versions are identical with the package versions.");
                            return new DomainEvent[] { new UpdateCanceled(TimeSpan.Zero) };
                        }

                        return @events;
                    }
                );
        }

        private Result<Unit> CheckApplicationPackageAllowed(Func<string, bool> isPackageAllowed, string targetApplicationName) =>
            isPackageAllowed(targetApplicationName)
                ? default
                : new Error("SystemSoftware", $"Update to target application '{targetApplicationName}' not allowed.");

        protected override DomainEvent[] OnState(Context context, DomainEvent @event) =>
            @event is PropertiesChanged propertiesChanged
                ? Handle(propertiesChanged)
                : base.OnState(context, @event);

        private DomainEvent[] Handle(PropertiesChanged @event)
        {
            foreach (var device in SoftwareToBeUpdated)
            {
                @event.GetPropertyValue<Percentage>(device.update_progress)
                    .Tap(p =>
                    {
                        if (Percentage.ONE.Equals(p))
                            MarkFullyUpdated(device);
                    });
            }

            return IsUpdateCompleted() ? new DomainEvent[] { new UpdateCompleted(@event.At) } : Array.Empty<DomainEvent>();
        }

        public Transition<BaseState<Context>, (Context, DomainEvent)> WhenUpdateCompletedForAllDevices(BaseState<Context> targetState)
        {
            return new Transition<BaseState<Context>, (Context, DomainEvent)>(this, targetState, x => x.Item2 is UpdateCompleted);
        }

        public Transition<BaseState<Context>, (Context, DomainEvent)> WhenUpdateCanceled(BaseState<Context> targetState)
        {
            return new Transition<BaseState<Context>, (Context, DomainEvent)>(this, targetState, x => x.Item2 is UpdateCanceled);
        }

        protected override string GetStateName()
        {
            return nameof(Updating);
        }

        public override bool CanHandle(DomainEvent @event) =>
            @event switch
            {
                UpdateCompleted _ => true,
                UpdateCanceled _ => true,
                PropertiesChanged pc => pc.ContainsAny(SoftwareToBeUpdated.Select(s => s.update_progress)),
                _ => false
            };

        private Result<DomainEvent[]> BuildUpdateCommands(Package package) =>
            SoftwareToBeUpdated
                .Select(sd => _domainEventFactory.NewEventResult(sd._update.command, package[sd].GetValue()))
                .ToArray()
                .Traverse();

        private Result<PropertiesChanged> InitializeUpdateProgress(Package package)
        {
            var properties = SoftwareToBeUpdated.Select(sd => ((Urn)sd.update_progress,(object)Percentage.FromFloat(0.0f).Value));
            return _domainEventFactory.NewEventResult(properties);
        }

        private Unit SelectSoftwareToUpdate(Package package, Context context)
        {
            var supportedUpdateCommands = context.SupportedForUpdate.Select(sd => sd._update.command).ToHashSet();
            SoftwareToBeUpdated =
                package.SoftwareDevices
                    .Where(sd => CanUpdateSoftwareDevice(sd, package, supportedUpdateCommands, context))
                    .ToHashSet();
            SoftwareWithUpdateCompleted = new HashSet<SoftwareDeviceNode>();
            return default(Unit);
        }

        private bool CanUpdateSoftwareDevice(SoftwareDeviceNode softwareDeviceNode, Package package, HashSet<CommandUrn<PackageContent>> supportedDevices,
            Context context)
        {
            if (!supportedDevices.Contains(softwareDeviceNode._update))
                return false;

            var content = package[softwareDeviceNode];
            if (content.IsNone)
                return false;

            if (context.AlwaysUpdate.Contains(softwareDeviceNode))
                return true;

            var versionStr = content.GetValue().Revision;
            return (
                    from versionToUpdate in SoftwareVersion.FromString(versionStr)
                    from fallbackVersion in context.GetFallbackVersion(softwareDeviceNode).ToResult("not found")
                    select !versionToUpdate.Equals(fallbackVersion)
                )
                .GetValueOrDefault(true);
        }

        private void MarkFullyUpdated(SoftwareDeviceNode deviceNode)
        {
            SoftwareWithUpdateCompleted.Add(deviceNode);
        }

        private bool IsUpdateCompleted()
        {
            return SoftwareToBeUpdated.SetEquals(SoftwareWithUpdateCompleted);
        }

        public Updating(SystemSoftwareModuleDefinition moduleDefinition, Loader loader, IDomainEventFactory domainEventFactory) : base(moduleDefinition, domainEventFactory)
        {
            _moduleDefinition = moduleDefinition;
            _loader = loader;
            _domainEventFactory = domainEventFactory;
            SoftwareToBeUpdated = new HashSet<SoftwareDeviceNode>();
            SoftwareWithUpdateCompleted = new HashSet<SoftwareDeviceNode>();
        }

        private readonly SystemSoftwareModuleDefinition _moduleDefinition;
        private readonly Loader _loader;
        private readonly IDomainEventFactory _domainEventFactory;
        public HashSet<SoftwareDeviceNode> SoftwareToBeUpdated { get; set; }
        private HashSet<SoftwareDeviceNode> SoftwareWithUpdateCompleted { get; set; }
    }

    public class UpdateCompleted : PrivateDomainEvent
    {
        public UpdateCompleted(TimeSpan at) : base(Guid.NewGuid(), at)
        {
        }
    }

    public class UpdateCanceled : PrivateDomainEvent
    {
        public UpdateCanceled(TimeSpan at) : base(Guid.NewGuid(), at)
        {
        }
    }
}