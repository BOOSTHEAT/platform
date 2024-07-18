using System;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.TestsCommon
{
    public static class PropertiesChangedHelper 
    {
      public static PropertiesChanged CreatePropertyChanged(TimeSpan changeTime, params (Urn urn, object value)[] properties)
        {
            var changedValues = properties.Select(p => new DataModelValue<object>(p.urn, p.value, changeTime)).ToArray();
            return PropertiesChanged.Create(changedValues, changeTime);
        }

        public static PropertiesChanged CreatePropertyChanged(TimeSpan changeTime, Urn urn, object fakeValue)
        {
            var changedValues = new IDataModelValue[] { new DataModelValue<object>(urn, fakeValue, changeTime) };
            return PropertiesChanged.Create(changedValues, changeTime);
        }
    }
}