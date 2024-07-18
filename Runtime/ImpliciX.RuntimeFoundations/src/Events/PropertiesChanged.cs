using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;
using Debug = ImpliciX.Data.Debug;

namespace ImpliciX.RuntimeFoundations.Events
{
 
    public class PropertiesChanged : PublicDomainEvent
    {
        public static PropertiesChanged Create(IEnumerable<IDataModelValue> modelValues, TimeSpan at) => CreateImpl(null, modelValues, at);

        public static PropertiesChanged Create(Urn group, IEnumerable<IDataModelValue> modelValues, TimeSpan at) => CreateImpl(group, modelValues, at);

        public static PropertiesChanged Create<T>(PropertyUrn<T> urn, T value, TimeSpan at) => Create(null, urn, value, at);

        public static PropertiesChanged Create<T>(Urn group, PropertyUrn<T> urn, T value, TimeSpan at) =>
            CreateImpl(group, new[]
                {
                    Property<T>.Create(urn, value, at)
                }
                , at);

        private static PropertiesChanged CreateImpl(Urn group, IEnumerable<IDataModelValue> modelValues, TimeSpan at)
        {
            var dataModelValues = modelValues as IDataModelValue[] ?? modelValues.ToArray();
            var concurrentChanges = ConcurrentChanges(dataModelValues);
            Debug.PreCondition(
                ()=>!concurrentChanges.Any(),
                ()=>$"Concurrent changes detected for urns: {string.Join("; ",concurrentChanges.Select(u=>u.ToString()))}"
            );
            return new PropertiesChanged(Guid.NewGuid(), group, at, dataModelValues);
        }

        public static PropertiesChanged Empty(in TimeSpan at)
        {
            return new PropertiesChanged(Guid.NewGuid(), null, at, Array.Empty<IDataModelValue>());   
        }
        public Urn Group { get; }
        public IEnumerable<IDataModelValue> ModelValues { get; }
        
        public Option<T> GetPropertyValue<T>(Urn propertyUrn)
        {
            if(!ContainsProperty(propertyUrn)) return Option<T>.None();
            return ModelValues.Where(mv => mv.Urn.Equals(propertyUrn))
                .Select(mv => mv.ModelValue())
                .Cast<T>()
                .FirstOrDefault()
                .ToOption();
        }
        
        public Option<object> GetPropertyValue(Urn propertyUrn)
        {
            if(!ContainsProperty(propertyUrn)) return Option<object>.None();
            return ModelValues.Where(mv => mv.Urn.Equals(propertyUrn))
                .Select(mv => mv.ModelValue())
                .FirstOrDefault()
                .ToOption();
        }

        public bool TryGetPropertyValue(Urn propertyUrn, out object value)
        {
            value = null;
            if (!ContainsProperty(propertyUrn)) return false;
            value = ModelValues.Where(mv => mv.Urn.Equals(propertyUrn))
                .Select(mv => mv.ModelValue())
                .FirstOrDefault();
            return true;
        }

        public bool ContainsProperty(Urn propertyUrn) => PropertiesUrns.Any(urn=>urn.Equals(propertyUrn));
        
        public bool ContainsProperty(Urn propertyUrn, object propertyValue) => 
            ModelValues.Any(mv=>mv.Urn.Equals(propertyUrn) && mv.ModelValue().Equals(propertyValue));

        public static Option<PropertiesChanged> Join(PropertiesChanged[] propertiesChanged)
        {
            if (propertiesChanged.Length == 0)
                return Option<PropertiesChanged>.None();
            else
            {
                var mergedValues = new List<IDataModelValue>();
                foreach (var changed in propertiesChanged) 
                    mergedValues.AddRange(changed.ModelValues);
                return Create(mergedValues, propertiesChanged[0].At);
            }
        }
        
        
        public Urn[] PropertiesUrns => ModelValues.Select(m => m.Urn).ToArray();

        private static Urn[] ConcurrentChanges(IEnumerable<IDataModelValue> modelValues) => 
            modelValues.GroupBy(m => m.Urn)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToArray();

        private PropertiesChanged(Guid eventId, Urn group, TimeSpan at, IEnumerable<IDataModelValue> modelValues) : base(eventId, at)
        {
            Group = group;
            ModelValues = modelValues;
        }

//        private bool Equals(PropertiesChanged other) => At.Equals(other.At) && ModelValues.SequenceEqual(other.ModelValues);
        private bool Equals(PropertiesChanged other) => (Group == other.Group) && ModelValues.SequenceEqual(other.ModelValues);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PropertiesChanged) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (1 * 397) ^ (ModelValues != null ? ModelValues.GetHashCode() : 0);
            }
        }

        public static bool operator ==(PropertiesChanged left, PropertiesChanged right) => Equals(left, right);
        public static bool operator !=(PropertiesChanged left, PropertiesChanged right) => !Equals(left, right);
        public override string ToString()
            => (Group == null ? "" : Group + ", ")
                + ModelValues.Aggregate(At.ToString(), (acc, mv) => $"{acc}{(acc != "" ? ", " : "")}{mv.Urn}, {mv.ModelValue()}");

        public bool ContainsAny(IEnumerable<Urn> urns)
        {
            return urns!=null && PropertiesUrns.Intersect(urns).Any();
        }
    }
}