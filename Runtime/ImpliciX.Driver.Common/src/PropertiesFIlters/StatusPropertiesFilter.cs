using System.Collections.Generic;
using System.Linq;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;

namespace ImpliciX.Driver.Common.PropertiesFIlters
{
    public static class StatusPropertiesFilter
    {
        public static PropertiesChanged FilterPropertiesChanged(
            DriverStateKeeper stateKeeper,
            IDomainEventFactory domainEventFactory,
            IEnumerable<IDataModelValue> modelValues)
        {
            var segregatedProperties = StatusPropertiesFilter.SegregateMeasureAndStatus(modelValues.ToArray());
            var filteredMeasuresStatus = StatusPropertiesFilter.Filter(stateKeeper, segregatedProperties.statusProperties);
            return domainEventFactory.NewEvent(segregatedProperties.measuresProperties.Concat(filteredMeasuresStatus));
        }

        public static List<IDataModelValue> Filter(DriverStateKeeper stateKeeper, List<IDataModelValue> statusProperties)
        {
            var outputProperties = new List<IDataModelValue>();

            foreach (var sp in statusProperties)
            {
                var state = stateKeeper.Read(sp.Urn);
                var result = state.GetValue<MeasureStatus>("value");
                if (state.IsEmpty || result.IsError)
                {
                    state = new DriverState(sp.Urn).WithValue("value", sp.ModelValue());
                    outputProperties.Add(sp);
                }
                else if (!result.Value.Equals(sp.ModelValue()))
                {
                    outputProperties.Add(sp);
                    state.WithValue("value", sp.ModelValue());
                }

                stateKeeper.Update(state);
            }

            return outputProperties;
        }

        public static (List<IDataModelValue> measuresProperties, List<IDataModelValue> statusProperties) SegregateMeasureAndStatus(IDataModelValue[] properties)
        {
            var statusProperties = new List<IDataModelValue>();
            var measuresProperties = new List<IDataModelValue>();
            foreach (var property in properties)
            {
                if (property.ModelValue() is MeasureStatus)
                {
                    statusProperties.Add(property);
                }
                else
                {
                    measuresProperties.Add(property);
                }
            }

            return (measuresProperties, statusProperties);
        }
    }
}