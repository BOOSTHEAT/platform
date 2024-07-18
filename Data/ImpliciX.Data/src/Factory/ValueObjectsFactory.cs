using System;
using System.Diagnostics.Contracts;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;


namespace ImpliciX.Data.Factory
{
    public static class ValueObjectsFactory
    {
        
        public static Result<object> FromString(Type urnType, string value)
        {
            var targetedType = ValueTypeFromUrnType(urnType);
            Contract.Assert(IsValueObject(targetedType), $"Missing 'ValueObject' attribute on '{targetedType.FullName}'.");
            Contract.Assert(value != null, "value != null");

            return CreateValueObject(targetedType, value);
        }

        public static Result<object> CreateValueObject(Type targetedType, string value)
        {
            return targetedType switch
            {
                var t when t.IsEnum => SafeEnum.TryParse(targetedType, value, (msg) => new ModelFactoryError(msg)),
                var t when t == typeof(FunctionDefinition) => FunctionDefinition.FromString(value).GetValueOrDefault(),
                var t when t.IsValueType => Reflector.CreateInstance(targetedType, new object[] {value})
                    .Match(err => err, Reflector.ExtractResultValue),
                _ => new ModelFactoryError($"Could not create {targetedType.Name}.")
            };
        }

        public static Result<object> FromHashValue(Type urnType, (string Name, string Value)[] values)
        {
            var targetedType = ValueTypeFromUrnType(urnType);
            Contract.Assert(IsValueObject(targetedType), $"({targetedType.Name}) : Only value types are supported.");
            Contract.Assert(values.Length > 0, "At least one value is required");

            return targetedType switch
            {
                var t when t.IsEnum => SafeEnum.TryParse(targetedType, values[0].Value, (msg) => new ModelFactoryError(msg)),
                var t when t == typeof(FunctionDefinition) =>  Reflector.CreateInstance(targetedType, new object[] {values}).Match(err => err, Reflector.ExtractResultValue),
                var t when t.IsValueType => Reflector.CreateInstance(targetedType, new object[] {values[0].Value}).Match(err => err, Reflector.ExtractResultValue),
                _ => new ModelFactoryError($"Could not create {targetedType.Name}.")
            };
        }

        public static Type ValueTypeFromUrnType(Type urnType)
        {
            return urnType switch
            {
                var t when t == typeof(MetricUrn) => typeof(MetricValue),
                _ => Reflector.GenericTypeArgumentOf(urnType)
            };
        }
        private static bool IsValueObject(Type targetedType)
        {
            return Reflector.HasAttribute(targetedType, typeof(ValueObject));
        }
        
        
    }
}