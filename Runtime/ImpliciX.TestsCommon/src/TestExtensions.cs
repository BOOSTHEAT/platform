using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.TestsCommon
{
    public static class TestExtensions
    {
        public static bool ContainsMeasuresFailureProperties(this IEnumerable<DomainEvent> propertiesChanged)
        {
            return HasMeasuresStatusProperties(propertiesChanged, MeasureStatus.Failure);
        }
        
        public static bool ContainsMeasuresSuccessProperties(this IEnumerable<DomainEvent> propertiesChanged)
        {
            return HasMeasuresStatusProperties(propertiesChanged, MeasureStatus.Success);
        }
        
       public static bool ContainsProperty<T>(this IEnumerable<DomainEvent> propertiesChanged, PropertyUrn<T> urn, T value)
        {
            return propertiesChanged.FilterEvents<PropertiesChanged>()
                .Any(it => it.ContainsProperty(urn,value));
        }

        private static bool HasMeasuresStatusProperties(IEnumerable<DomainEvent> propertiesChanged, MeasureStatus measureStatus)
        {
            var result = propertiesChanged.Cast<PropertiesChanged>()
                .SelectMany(pc => pc.ModelValues)
                .Any(mv => mv.ModelValue() is MeasureStatus status && status==measureStatus);
            return result;
        }
    }
}