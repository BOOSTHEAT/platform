using ImpliciX.Language.Model;

namespace ImpliciX.HttpTimeSeries;

public interface IDefinedSeries
{
  Urn[] RootUrns { get; }
  bool ContainsRootUrn(Urn rootUrn);
  (HashSet<Urn>, TimeSpan) StorablePropertiesForRoot(Urn rootUrn);
  Urn[] OutputUrns { get; }
  Urn? RootUrnOf(Urn outputUrn);
}