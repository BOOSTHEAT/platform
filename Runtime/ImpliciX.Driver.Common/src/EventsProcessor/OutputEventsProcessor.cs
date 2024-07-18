using System.Collections.Generic;
using System.Linq;
using ImpliciX.Driver.Common.PropertiesFIlters;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Driver.Common.EventsProcessor
{
  public static class OutputEventsProcessor
  {
    public static DomainEvent[] MergeAndFilterPropertiesChanged(
      DriverStateKeeper stateKeeper,
      IDomainEventFactory domainEventFactory,
      DomainEvent[] @events)
    {
      var propertiesChangedWithGroup = new Dictionary<Urn, List<IDataModelValue>>();
      var otherPropertiesChanged = new List<IDataModelValue>();
      var resultingEvents = new List<DomainEvent>();
      foreach (var @event in events)
      {
        if (@event is PropertiesChanged pcEvent)
          SortProperties(
            StatusPropertiesFilter.Filter(stateKeeper, pcEvent.ModelValues.ToList()),
            pcEvent.Group,
            otherPropertiesChanged,
            propertiesChangedWithGroup);
        else
          resultingEvents.Add(@event);
      }

      if (otherPropertiesChanged.Any())
        resultingEvents.Add(domainEventFactory.NewEvent(otherPropertiesChanged));
      resultingEvents.AddRange(propertiesChangedWithGroup.Select(kv => domainEventFactory.NewEvent(kv.Key, kv.Value)));
      return resultingEvents.ToArray();
    }

    private static void SortProperties(
      List<IDataModelValue> properties,
      Urn group,
      List<IDataModelValue> otherPropertiesChanged,
      Dictionary<Urn, List<IDataModelValue>> propertiesChangedWithGroup)
    {
      if (!properties.Any())
        return;
      if (group == null)
      {
        otherPropertiesChanged.AddRange(properties);
        return;
      }
      if (propertiesChangedWithGroup.TryGetValue(group, out var existingGroup))
        existingGroup.AddRange(properties);
      else
        propertiesChangedWithGroup[group] = new List<IDataModelValue>(properties);
    }
  }
}