using System;
using ImpliciX.Control.DomainEvents;
using ImpliciX.Language.Model;

namespace ImpliciX.Control.Tests.TestUtilities
{
    public class ControlEventHelper
    {
        public static StateChanged EventStateChanged(Urn urn, Enum[] states, TimeSpan currentTime)
        {
            IDataModelValue CreateCompositeStateProperty()
            {
                var value = EnumSequence.Create(states);
                return new DataModelValue<EnumSequence>(urn, value, currentTime); 
            }

            return StateChanged.Create(CreateCompositeStateProperty(), currentTime);
        }
    }
}