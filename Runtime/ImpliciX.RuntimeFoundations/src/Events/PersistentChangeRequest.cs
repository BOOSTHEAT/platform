using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RuntimeFoundations.Events
{
    public class PersistentChangeRequest : PublicDomainEvent
    {
        public static PersistentChangeRequest Create(IEnumerable<IDataModelValue> modelValues, TimeSpan at)
        {
            return new PersistentChangeRequest(Guid.NewGuid(), at, modelValues);
        }

        public IEnumerable<IDataModelValue>  ModelValues { get; }

        private PersistentChangeRequest(Guid eventId, TimeSpan at, IEnumerable<IDataModelValue>  modelValues) : base(eventId, at)
        {
            ModelValues = modelValues;
        }

        protected bool Equals(PersistentChangeRequest other)
        {
            return ModelValues.SequenceEqual(other.ModelValues);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PersistentChangeRequest) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (1 * 397) ^ (ModelValues != null ? ModelValues.GetHashCode() : 0);
            }
        }

        public static bool operator ==(PersistentChangeRequest left, PersistentChangeRequest right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PersistentChangeRequest left, PersistentChangeRequest right)
        {
            return !Equals(left, right);
        }
        
        public override string ToString()
            => ModelValues.Aggregate(string.Empty, (acc, mv) => $"{acc}, {mv.Urn}, {mv.ModelValue()}");
    }
}