using System.Collections.Generic;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Driver.Dumb
{
    public class DumbActuators
    {
        public static DomainEventHandler<CommandRequested> ExecuteCommand(Clock clock, ModelFactory modelFactory)
        {
            return commandEvt =>
            {
                Log.Debug("Virtual driver executes : {@Urn}, {@Arg}", commandEvt.Urn, commandEvt.Arg);
                var measurePropUrn = Urn.BuildUrn(commandEvt.Urn,"measure");
                var statusPropUrn = Urn.BuildUrn(commandEvt.Urn,"status");
                
                var outcomeEvents = new List<DomainEvent>();
                if(modelFactory.UrnExists(measurePropUrn))
                {
                    var successEvent = 
                        from measureProperty in modelFactory.CreateWithLog(measurePropUrn, commandEvt.Arg, clock())
                        from statusProperty in modelFactory.CreateWithLog(statusPropUrn, MeasureStatus.Success, clock())
                        select PropertiesChanged.Create(new[] {(IDataModelValue) measureProperty, (IDataModelValue)statusProperty}, clock());
                    successEvent.Tap(evt=>outcomeEvents.Add(evt));
                }
                return outcomeEvents.ToArray();
            };
        }
    }
}