using ImpliciX.Data.TimeSeries;
using ImpliciX.Language.Model;

namespace ImpliciX.HttpTimeSeries;

internal class SelfDefinedSeries : IDefinedSeries
{
  private readonly Dictionary<string, (HashSet<Urn>, TimeSpan)> _storable;
  private readonly Dictionary<string, TimeSeriesUrn> _members;
  private static readonly (HashSet<Urn>, TimeSpan Zero) DefaultStorable = (new HashSet<Urn>(), TimeSpan.Zero);

  public SelfDefinedSeries(TimeSeriesWithRetention[] def)
  {
    var urns = def.Select(ts => ts.Urn()).ToArray();
    _storable = urns
      .ToDictionary(u => u.Value, u => (new HashSet<Urn>(u.Members), u.Retention));
    _members = urns
      .SelectMany(u => u.Members.Select(m => (m, u)))
      .ToDictionary(x => x.m.Value, x => x.u);
    RootUrns = urns.Cast<Urn>().ToArray();
    OutputUrns = urns.SelectMany(u => u.Members).ToArray();
  }

  public Urn[] RootUrns { get; }
  public bool ContainsRootUrn(Urn rootUrn) => _storable.ContainsKey(rootUrn);

  public (HashSet<Urn>, TimeSpan) StorablePropertiesForRoot(Urn rootUrn) =>
    _storable.GetValueOrDefault(rootUrn.Value, DefaultStorable);

  public Urn[] OutputUrns { get; }
  public Urn? RootUrnOf(Urn outputUrn) => _members.GetValueOrDefault(outputUrn.Value);
}