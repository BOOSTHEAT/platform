using System;
using System.Collections.Generic;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Control.Helpers
{
    public class PropertiesChangedBuffer
    {
        private Dictionary<Urn, IDataModelValue> DataModelValues { get; set; }
        private TimeSpan LastAt { get; set; }

        public PropertiesChangedBuffer()
        {
            DataModelValues = new Dictionary<Urn, IDataModelValue>();
        }

        public void ReceivedPropertiesChanged(PropertiesChanged propertiesChanged)
        {
            LastAt = propertiesChanged.At;
            foreach (var dataModelValue in propertiesChanged.ModelValues)
            {
                DataModelValues[dataModelValue.Urn] = dataModelValue;
            }
        }

        public PropertiesChanged ReleasePropertiesChanged()
        {
            var result = PropertiesChanged.Create(DataModelValues.Values, LastAt);
            DataModelValues.Clear();
            return result;
        }
    }
}