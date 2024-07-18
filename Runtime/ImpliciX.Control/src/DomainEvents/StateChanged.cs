using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Control.DomainEvents
{
    public class StateChanged : PrivateDomainEvent
    {
        public static StateChanged Create(IDataModelValue modelValues, TimeSpan at)
        {
            return new StateChanged(Guid.NewGuid(), at, new []{modelValues});
        }

        public IEnumerable<IDataModelValue> ModelValues { get; }

        private StateChanged(Guid eventId, TimeSpan at, IEnumerable<IDataModelValue> modelValues) : base(eventId, at)
        {
            ModelValues = modelValues;
        }

        protected bool Equals(StateChanged other)
        {
            return ModelValues.SequenceEqual(other.ModelValues);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StateChanged) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (1 * 397) ^ (ModelValues != null ? ModelValues.GetHashCode() : 0);
            }
        }

        public static bool operator ==(StateChanged left, StateChanged right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(StateChanged left, StateChanged right)
        {
            return !Equals(left, right);
        }
    }
}