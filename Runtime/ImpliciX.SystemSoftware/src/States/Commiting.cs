using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Tools;

namespace ImpliciX.SystemSoftware.States
{
    public class Commiting : BaseState<Context>
    {
        private readonly SystemSoftwareModuleDefinition _moduleDefinition;
        private readonly IDomainEventFactory _domainEventFactory;
        private readonly HashSet<CommandUrn<NoArg>> _commandsToSendOnEntry;
        private readonly HashSet<PropertyUrn<MeasureStatus>> _successesToTrack;
        private readonly HashSet<PropertyUrn<MeasureStatus>> _successesReceived;

        public Commiting(SystemSoftwareModuleDefinition moduleDefinition, IDomainEventFactory domainEventFactory) : base(moduleDefinition,domainEventFactory)
        {
            _moduleDefinition = moduleDefinition;
            _domainEventFactory = domainEventFactory;
            
            _commandsToSendOnEntry = new HashSet<CommandUrn<NoArg>>()
            {
                moduleDefinition.CommitUpdateCommand.command,
                moduleDefinition.CleanVersionSettings.command,
            };

            _successesToTrack = new HashSet<PropertyUrn<MeasureStatus>>()
            {
                moduleDefinition.CleanVersionSettings.status,
            };
            _successesReceived = new HashSet<PropertyUrn<MeasureStatus>>();
        }

        protected override DomainEvent[] OnEntry(Context context, DomainEvent @event)
        {
            Reset();

            (from currentPackage in context.CurrentUpdatePackage.ToResult("No package found in context")
             select currentPackage.CopyManifest(context.UpdateManifestPath)).UnWrap()
                .LogWhenError("Error occured while coping manifest file")
                .LogDebugOnSuccess($"The current update manifest file was copied to {context.UpdateManifestPath}");
                
            return _commandsToSendOnEntry
                .Select(x => _domainEventFactory.NewEventResult(x, default(NoArg)))
                .ToArray()
                .Traverse().GetValueOrDefault(Array.Empty<DomainEvent>());
        }

        protected override DomainEvent[] OnState(Context context, DomainEvent @event)
        => @event is PropertiesChanged propertiesChanged ? 
            Handle(propertiesChanged) : 
            base.OnState(context, @event);

        public override bool CanHandle(DomainEvent @event)
        {
            return @event switch
            {
                PropertiesChanged pc => pc.ContainsAny(_successesToTrack),
                _ => false
            }; 
        }

        protected override string GetStateName()
        {
            return nameof(Commiting);
        }

        private DomainEvent[] Handle(PropertiesChanged @event)
        {
            foreach (var prop in _successesToTrack)
            {
                @event.GetPropertyValue<MeasureStatus>(prop)
                    .Tap(p =>
                    {
                        if (MeasureStatus.Success.Equals(p))
                            _successesReceived.Add(prop);
                    });
            }

            
            return AllSuccessesReceived() ? 
                _domainEventFactory
                    .NewEventResult(_moduleDefinition.RebootCommand,default(NoArg))
                    .Match(_=> Array.Empty<DomainEvent>(),x=>new DomainEvent[]{x}) 
                 : Array.Empty<DomainEvent>();
        }

        private bool AllSuccessesReceived() =>
            _successesToTrack.SetEquals(_successesReceived);

        private Unit Reset()
        {
            _successesReceived.Clear();
            return default;
        }
    }
}