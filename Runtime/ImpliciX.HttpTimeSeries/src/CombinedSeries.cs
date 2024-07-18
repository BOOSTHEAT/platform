using ImpliciX.Language.Model;

namespace ImpliciX.HttpTimeSeries;

public class CombinedSeries : IDefinedSeries
{
  private readonly Dictionary<string, (HashSet<Urn>, TimeSpan)> _storable;
  private readonly Dictionary<string, Urn> _members;
  private static readonly (HashSet<Urn>, TimeSpan Zero) DefaultStorable = (new HashSet<Urn>(), TimeSpan.Zero);
  
  public CombinedSeries(params IDefinedSeries[] series)
  {
    RootUrns = series.SelectMany(s => s.RootUrns).ToArray();
    OutputUrns = series.SelectMany(s => s.OutputUrns).ToArray();
    _members = (
      from s in series 
      from u in s.OutputUrns
      select (u.Value, s.RootUrnOf(u))
      ).ToDictionary(t => t.Item1, t => t.Item2);
    _storable = (
      from s in series 
      from root in s.RootUrns
      select (root.Value, s.StorablePropertiesForRoot(root))
    ).ToDictionary(t => t.Item1, t => t.Item2);
  }

  public Urn[] RootUrns { get; }
  public bool ContainsRootUrn(Urn rootUrn) => _storable.ContainsKey(rootUrn);

  public (HashSet<Urn>, TimeSpan) StorablePropertiesForRoot(Urn rootUrn) =>
    _storable.GetValueOrDefault(rootUrn.Value, DefaultStorable);

  public Urn[] OutputUrns { get; }
  public Urn? RootUrnOf(Urn outputUrn) => _members.GetValueOrDefault(outputUrn.Value);
}