using System;
using System.Collections.Concurrent;
using System.Linq;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Factory;

public static class DynamicModelFactory
{
    private static readonly ConcurrentDictionary<string, Type> TypesIndex = new();
    
    public static IIMutableDataModelValue Create(string typeName, Urn urn, object value, TimeSpan at)
    {
        var valueType = TypesIndex.GetOrAdd(typeName, t =>
        {
            var correspondingType = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes()).FirstOrDefault(it=>it.FullName!.Equals(typeName));
            if (correspondingType == null)
                throw new Exception($"Could not find type {typeName}");
            return correspondingType;
        });

        return Create(valueType, urn, value, at);
    }

    public static IIMutableDataModelValue Create(Type valueType, Urn urn, object value, TimeSpan at)
    {
        var genericType = typeof(DataModelValue<>).MakeGenericType(valueType);
        
        if (valueType == value.GetType())
        {
            return (IIMutableDataModelValue) Activator.CreateInstance(genericType, urn, value, at);
        }

        var result = ValueObjectsFactory.CreateValueObject(valueType, value.ToString());
        if (result.IsError)
            throw new Exception($"Could not create value object {valueType.FullName} from {value}");
        
        return (IIMutableDataModelValue) Activator.CreateInstance(genericType, urn, result.Value, at);
    }
}