using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Factory
{
    public static class Reflector
    {
        internal static Result<Type> Root(Assembly[] assembly, string typeName)
        {
            var rootType = RootNodes(assembly)
                .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            if (rootType != null)
            {
                return rootType;
            }

            return new ModelFactoryError($"No root model definition for {typeName}");
        }

        internal static Type PrivateNodeType(Assembly[] assemblies, string[] typeNameCandidates)
        {
            return assemblies.SelectMany(a => 
                a.DefinedTypes.Where(t=>typeNameCandidates.Any(c=>c.Equals(t.Name, StringComparison.Ordinal))))
                .FirstOrDefault(t=> t.IsSubclassOf(typeof(PrivateModelNode)));
           
        }
        public static TypeInfo[] RootNodes(Assembly[] assemblies)
        {
            return assemblies.SelectMany(a => a.DefinedTypes.Where(t => t.IsSubclassOf(typeof(RootModelNode))))
                .ToArray();
        }

        public static MethodInfo GetFactoryMethod(Type type)
        {
            var factory = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.CustomAttributes.Any(a => a.AttributeType == typeof(ModelFactoryMethod)));
            return factory;
        }

        internal static Type GenericTypeArgumentOf(Type type, Dictionary<Type, Type>? fallbackMap = null)
        {
            if (type.IsGenericType)
            {
                return type.GenericTypeArguments[0];
            }
            return fallbackMap?.GetValueOrDefault(type,typeof(object)) ?? typeof(object);
        }

        internal static Type GetPropertyType(Type rootType, string[] propertyNameCandidates)
        {
            var propertiesFound =
                rootType.GetProperties().Where(p => propertyNameCandidates.Contains(p.Name.ToLower()));
            var propertyInfos = propertiesFound as PropertyInfo[] ?? propertiesFound.ToArray();
            if (propertyInfos.Length == 0) return null; 
            if (propertyInfos.Length != 1)
            {
                var propertyNames = $"<{string.Join(", ", propertyInfos.Select(p => p.Name))}>";
                Log.Error("Multiple URN tokens ({@Count}) are candidate in root type {@TypeName}: {@Tokens}",
                    propertyInfos.Length, rootType.Name, propertyNames);
                return null;
            }
            else
            {
                return propertyInfos[0].PropertyType;
            }
        }

        internal static Result<object> CreateInstance(Type type, object[] args)
        {
            var factoryMethod = GetFactoryMethod(type);
            if (factoryMethod == null)
                return new ModelFactoryError($"{type.Name} do not have factory a method.");
            return SideEffect.TryRun(
                () => factoryMethod.Invoke(null, args),
                (ex) => new ModelFactoryError($"{type.Name} failed to execute build Factory. Exception: {ex.CascadeMessage()}"));
        }


        internal static object ExtractResultValue(object result)
        {
            if (result.GetType().IsGenericType && result.GetType().GetGenericTypeDefinition() == typeof(Result<>))
            {
                return result.GetType().GetMethod("Extract")?.Invoke(result, new object[] { });
            }

            return result;
        }

        public static bool HasAttribute(Type type, Type attributeType)
        {
            return type.CustomAttributes.Any(a => a.AttributeType == attributeType);
        }

        public static bool IsGenericTypeOf(object obj, Type type)
        {
            var objType = obj.GetType();
            return objType.IsGenericType && objType.GetGenericTypeDefinition() == type;
        }

        public static Type TypeDefinition(object obj)
        {
            var type = obj.GetType();
            return type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        }
    }
}