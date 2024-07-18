using ImpliciX.Data.Factory;
using ImpliciX.Data.Records;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Records;

public class InstantValues
{
    private readonly Records _records;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly Dictionary<Urn, IDataModelValue> _instantValues = new ();
    private readonly HashSet<Urn> _concernedUrns;

    public InstantValues(Records records, HashSet<Urn> modelUrns, IIdentityGenerator identityGenerator)
    {
        _records = records;
        _identityGenerator = identityGenerator;
        _concernedUrns = _records.FormUrns.SelectMany(formUrn => modelUrns.Where(formUrn.IsPartOf)).ToHashSet();
    }

    public void Update(PropertiesChanged propertiesChanged)
    {
        var props = _concernedUrns.Intersect(propertiesChanged.PropertiesUrns).ToHashSet();
        if(props.Count == 0) return;
       
        var selectedProps = propertiesChanged.ModelValues.Where(mv => props.Contains(mv.Urn)).ToArray(); 
        foreach (var mv in selectedProps)
        {
            _instantValues[mv.Urn] = mv;
        }
    }

    public Option<Snapshot> Snapshot(Urn recordUrn, CommandUrn<NoArg> commandUrn, TimeSpan at)
    {
        var formUrn = _records.FormUrnForCommand(commandUrn) ?? "";
        var selectedModelValue = _instantValues
            .Where(it => formUrn.IsPartOf(it.Key))
            .Select(it => CreateModelValue(it.Key, it.Value.ModelValue(), at))
            .ToArray();

        return new Snapshot(_identityGenerator.Next(recordUrn), recordUrn, selectedModelValue, at, formUrn);
    }
    
    private static IIMutableDataModelValue CreateModelValue(Urn urn, object value, TimeSpan at) => 
        DynamicModelFactory.Create(value.GetType(), urn, value, at);
}