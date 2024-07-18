using System.Collections.Concurrent;
using ImpliciX.Language.Model;

namespace ImpliciX.Records.Tests.Helpers;

public class SequentialIdGenerator: IIdentityGenerator
{
    private readonly ConcurrentDictionary<Urn, int> _ids = new();
    public long Next(Urn recordUrn) => _ids.AddOrUpdate(recordUrn, 1, (_, value) => value + 1);
}