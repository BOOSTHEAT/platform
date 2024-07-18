using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.Language.Records;

namespace ImpliciX.Data.Records.HotRecords;

public class InMemHotRecordsDb : IHotRecordsDb
{
    private readonly ConcurrentDictionary<Urn, List<Snapshot>> _snapshots = new();
    private readonly Dictionary<Urn, Option<int>> _retentions;
    
    public InMemHotRecordsDb(IRecord[] records)
    {
        _retentions = records.DistinctBy(it => it.Urn).ToDictionary(it => it.Urn, it => it.Retention);
    }
    
    public void Write(Snapshot snapshot)
    {
        var recordUrn = snapshot.RecordUrn;
        _snapshots
            .AddOrUpdate(recordUrn, 
                addValue: new List<Snapshot> {snapshot}, 
                updateValueFactory: (_, list) => {
                                list.Insert(0,snapshot);
                                return list;
                });
        
        _retentions[recordUrn].Tap(retention =>
        {
            var storedSnapshots = _snapshots[recordUrn];
            if (storedSnapshots.Count > retention)
                storedSnapshots.RemoveAt(storedSnapshots.Count - 1);
        });
    }

    public IReadOnlyList<Snapshot> ReadAll(Urn recordUrn)
    {
        return _snapshots.TryGetValue(recordUrn, out var snapshots)
            ? snapshots : new List<Snapshot>();
    }

    public void Dispose()
    {
    }
}