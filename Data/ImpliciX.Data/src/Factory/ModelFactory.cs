using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Factory
{
    public class ModelFactory
    {
        private Assembly[] ModelAssemblies { get; }
        private readonly IDictionary<string, Urn> _backwardCompatibility;

        public ModelFactory(Assembly modelAssemblies) : this(new[] {modelAssemblies}) { }

        public ModelFactory(Assembly[] modelAssemblies) : this(modelAssemblies, new Dictionary<string, Urn>()) { }

        public ModelFactory(Assembly modelAssembly, IDictionary<string, Urn> backwardCompatibility) : this(new[] {modelAssembly}, backwardCompatibility) { }

        private ModelFactory(Assembly[] modelAssemblies, IDictionary<string, Urn> backwardCompatibility)
        {
            ModelAssemblies = modelAssemblies;
            _backwardCompatibility = backwardCompatibility ?? new Dictionary<string, Urn>();
        }

        public Result<object> Create(object urnCandidate, object valueCandidate, TimeSpan? at = null)
        {
            Contract.Assert(urnCandidate != null, "urnCandidate!=null");
            Contract.Assert(valueCandidate != null, "valueCandidate!=null");
            var result = from urn in CreateUrnWithBackwardCompatibility(urnCandidate)
                from vo in CreateValueObject(urn, valueCandidate)
                from modelInstance in CreateModelInstance(urn, vo, at)
                select modelInstance;

            return result;
        }

        public Result<object> CreateWithLog(object urnCandidate, object valueCandidate, TimeSpan? at = null)
        {
            var result = Create(urnCandidate, valueCandidate, at);
            var match = result
                .Match(err =>
                    {
                        Log.Error("ModelFactory: {@msg}", err.Message);
                        return Result<object>.Create(err);
                    },
                    whenSuccess: Result<object>.Create);

            return match;
        }

        public Result<object> Create(HashValue hashValue)
        {
            return
                from at in hashValue.At(msg => new ModelFactoryError(msg))
                let urnCandidate = hashValue.Key
                from modelObj in Create(urnCandidate, hashValue, at)
                select modelObj;
        }

        public static string ValueAsString(object arg)
            => arg is float f
                ? f.ToString(CultureInfo.InvariantCulture)
                : arg?.ToString();

        private static Result<object> CreateValueObject(Urn urn, object valueCandidate)
        {
            if (IsModelValueObject(valueCandidate)) return valueCandidate;

            return valueCandidate is HashValue hv
                ? from vo in ValueObjectsFactory.FromHashValue(urn.GetType(), hv.ValuesWithoutAtField)
                select vo
                : from vo in ValueObjectsFactory.FromString(urn.GetType(), ValueAsString(valueCandidate))
                select vo;
        }

        public Result<Urn> CreateUrnWithBackwardCompatibility(object urnCandidate)
        {
            var result = CreateUrn(urnCandidate);
            if (result.IsSuccess)
                return result;

            if (urnCandidate is string strUrn && _backwardCompatibility.TryGetValue(strUrn, out Urn actualUrn))
                return actualUrn;

            return result;
        }

        public Result<Urn> CreateUrn(object urnCandidate)
        {
            if (IsUrn(urnCandidate)) return (Urn) urnCandidate;
            if (urnCandidate is string or Urn)
            {
                return from urnType in FindUrnType(urnCandidate)
                    from urnInstance in UrnFactory.Create(urnType, urnCandidate.ToString())
                    select (Urn) urnInstance;
            }

            return new ModelFactoryError($"Could not create an valid urn for {urnCandidate}");
        }

        public Result<Type> FindUrnType(object urnCandidate)
        {
            if (urnCandidate is not string && urnCandidate is not Urn)
                return Result<Type>.Create(new Error(nameof(FindUrnType),
                    $"{nameof(urnCandidate)} type is {urnCandidate.GetType()} must be a string or a {nameof(Urn)}"));

            var tokens = Urn.Deconstruct(urnCandidate.ToString());
            return from root in Reflector.Root(ModelAssemblies, tokens[0])
                from urnType in FindUrnType(root, tokens.Skip(1).ToArray(), urnCandidate.ToString())
                select urnType;
        }

        public static Result<object> CreateModelInstance(Urn urnInstance, object valueObject, TimeSpan? at)
        {
            var mapping = new Dictionary<Type, (Type modelType, Func<object[]> fnArgs)?>()
            {
                {typeof(CommandUrn<>), (typeof(Command<>), () => new[] {urnInstance, valueObject})},
                {typeof(PropertyUrn<>), (typeof(Property<>), () => new[] {urnInstance, valueObject, at.GetValueOrDefault()})},
                {typeof(VersionSettingUrn<>), (typeof(Property<>), () => new[] {urnInstance, valueObject, at.GetValueOrDefault()})},
                {typeof(UserSettingUrn<>), (typeof(Property<>), () => new[] {urnInstance, valueObject, at.GetValueOrDefault()})},
                {typeof(FactorySettingUrn<>), (typeof(Property<>), () => new[] {urnInstance, valueObject, at.GetValueOrDefault()})},
                {typeof(PersistentCounterUrn<>), (typeof(Property<>), () => new[] {urnInstance, valueObject, at.GetValueOrDefault()})},
                {typeof(MetricUrn), (typeof(Property<>), () => new[] {urnInstance, valueObject, at.GetValueOrDefault()})},
            };

            var targetedTypeMap = new Dictionary<Type, Type>()
            {
                {typeof(MetricUrn), typeof(MetricValue)},
            };

            var urnType = urnInstance.GetType();
            var urnTypeDef = Reflector.TypeDefinition(urnInstance);
            var mappingDef = mapping.GetValueOrDefault(urnTypeDef);
            if (mappingDef == null) return new ModelFactoryError($"{urnType.Name} not supported");

            var targetedType = Reflector.GenericTypeArgumentOf(urnType, targetedTypeMap);
            var modelTypeDef = mappingDef.Value.modelType.MakeGenericType(targetedType);
            var args = mappingDef.Value.fnArgs();
            return Reflector.CreateInstance(modelTypeDef, args);
        }

        private Result<Type> FindUrnType(Type rootType, string[] tokens, string urn)
        {
            if (tokens.Length == 0) return new ModelFactoryError($"{urn} not found in the model");
            var token = tokens[0];
            var propertyNameCandidates = PropertyNameCandidates(token);
            var type = Reflector.GetPropertyType(rootType, propertyNameCandidates) ?? Reflector.PrivateNodeType(ModelAssemblies, propertyNameCandidates);
            if (type is null)
                return new ModelFactoryError($"{urn} not found in the model");

            if (tokens.Length != 1) return FindUrnType(type, tokens.Skip(1).ToArray(), urn);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(CommandNode<>))
            {
                var t = Reflector.GenericTypeArgumentOf(type);
                type = typeof(CommandUrn<>).MakeGenericType(t);
            }

            return Result<Type>.Create(type);
        }

        private static string[] PropertyNameCandidates(string token)
        {
            var lower = token.ToLower();
            return new[] {lower, $"_{lower}"};
        }

        public bool UrnExists(string urn)
        {
            var tokens = Urn.Deconstruct(urn);
            var result = from root in Reflector.Root(ModelAssemblies, tokens[0])
                from urnType in FindUrnType(root, tokens.Skip(1).ToArray(), urn)
                select urnType;

            return result.IsSuccess;
        }

        public IEnumerable<Urn> GetAllUrns()
        {
            IEnumerable<T> GetProperties<T>(IEnumerable<ModelNode> nodes)
                => from node in nodes
                    from pi in node.GetType().GetProperties().Where(p => typeof(T).IsAssignableFrom(p.PropertyType))
                    let p = (T) pi.GetValue(node)
                    where p != null
                    select p;

            void MergeAllSubModelNodes(IEnumerable<ModelNode> nodes, HashSet<ModelNode> merged)
            {
                var subNodes = GetProperties<ModelNode>(nodes).Distinct().Except(merged).ToArray();
                if (!subNodes.Any())
                    return;

                merged.UnionWith(subNodes);
                MergeAllSubModelNodes(subNodes, merged);
            }

            IEnumerable<ModelNode> GetStaticNodesOfRootNodes(Type rootNodeType) =>
                rootNodeType.GetProperties(BindingFlags.Static | BindingFlags.Public)
                    .Where(p => typeof(ModelNode).IsAssignableFrom(p.PropertyType))
                    .Select(p => p.GetValue(null))
                    .Cast<ModelNode>();

            IEnumerable<ModelNode> GetInstanceNodesOfRootNodes(Type rootNodeType) =>
                from rootProperty in rootNodeType.GetProperties(BindingFlags.Static | BindingFlags.Public)
                where rootProperty.PropertyType == rootNodeType
                let rootNode = rootProperty.GetValue(null)
                from property in rootNode.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                where typeof(ModelNode).IsAssignableFrom(property.PropertyType) && property.DeclaringType != typeof(ModelNode)
                select (ModelNode) property.GetValue(rootNode);

            var allRootNodes = Reflector.RootNodes(ModelAssemblies).Cast<Type>().ToList();
            var allModelNodes =
                allRootNodes.SelectMany(GetStaticNodesOfRootNodes)
                .Concat(allRootNodes.SelectMany(GetInstanceNodesOfRootNodes))
                .ToHashSet();

            MergeAllSubModelNodes(allModelNodes, allModelNodes);
            var urns = GetProperties<Urn>(allModelNodes).Distinct().OrderBy(u => u.Value).ToArray();
            //File.WriteAllLines("/tmp/all_urns.txt",urns.Select(u => u.Value).OrderBy(v => v));
            return urns;
        }

        private static bool IsUrn(object urnCandidate) =>
            Reflector.IsGenericTypeOf(urnCandidate, typeof(PropertyUrn<>))
            || Reflector.IsGenericTypeOf(urnCandidate, typeof(CommandUrn<>));


        private static bool IsModelValueObject(object modelValue) =>
            Reflector.HasAttribute(modelValue.GetType(), typeof(ValueObject));
    }

    public class ModelFactoryError : Error
    {
        public ModelFactoryError(string message) : base(nameof(ModelFactory), message)
        {
        }
    }
}