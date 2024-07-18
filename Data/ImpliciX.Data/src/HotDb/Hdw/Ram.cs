using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.HotDb.Model;

namespace ImpliciX.Data.HotDb.Hdw;

internal class Ram : IRam
{
    internal Ram()
    {
        DataStructureDefs = new HashSet<StructDef>();
        DataStructuresByNameMap = new ConcurrentDictionary<string, HashSet<StructDef>>();
        DataStructuresByIdMap = new ConcurrentDictionary<Guid, StructDef>();
        SegsByStructIdMap = new ConcurrentDictionary<Guid, SortedSet<Seg>>();
        UsableSegsMap = new ConcurrentDictionary<Guid, ConcurrentDictionary<SegmentState, SortedSet<Seg>>>();
        LastPkByStructIdMap = new ConcurrentDictionary<Guid, long>();
    }

    public ConcurrentDictionary<Guid,ConcurrentDictionary<SegmentState,SortedSet<Seg>>> UsableSegsMap { get; set; }

    private ConcurrentDictionary<Guid, long> LastPkByStructIdMap { get; }

    private ConcurrentDictionary<Guid, SortedSet<Seg>> SegsByStructIdMap { get; set; }
    private HashSet<StructDef> DataStructureDefs { get; set; }
    private ConcurrentDictionary<Guid, StructDef> DataStructuresByIdMap { get; set; }
    
    public bool TryGetStructure(Guid id, out StructDef structDef) => 
        DataStructuresByIdMap.TryGetValue(id, out structDef);

    public long? LastPkOf(Guid structDefId)
    {
        return LastPkByStructIdMap.TryGetValue(structDefId, out var pk) ? pk : null;
    }

    public bool TryGetSegments(StructDef structDef, out SortedSet<Seg> segs) => 
        SegsByStructIdMap.TryGetValue(structDef.Id, out segs);

    public bool TryGetStructure(string key, out StructDef structDef)
    {
        if (DataStructuresByNameMap.TryGetValue(key, out var structDefs))
        {
            structDef = structDefs.First();
            return true;
        }

        structDef = null;
        return false;
    }

    public StructDef GetStructureById(Guid id)
    {
        return DataStructuresByIdMap[id];
    }

    private ConcurrentDictionary<string, HashSet<StructDef>> DataStructuresByNameMap { get; set; }


    public bool ContainsStructure(string structureName)
    {
        return DataStructuresByNameMap.ContainsKey(structureName);
    }

    public bool ContainsStructure(StructDef structDef)
    {
        return DataStructureDefs.Contains(structDef);
    }

    public StructDef[] AllStructureDefinitions()
    {
        return DataStructureDefs.ToArray();
    }

    public IReadOnlyDictionary<Guid, StructDef> AllStructuresById => DataStructuresByIdMap;

    public IReadOnlyDictionary<string, HashSet<StructDef>> AllStructuresByName => DataStructuresByNameMap;


    public Seg[] SegsOf(string structureName) =>
        SegsByStructIdMap            
            .Where(kvp => structureName.Equals(DataStructuresByIdMap.GetValueOrDefault(kvp.Key, null)?.Name, StringComparison.OrdinalIgnoreCase))
            .SelectMany(kvp => kvp.Value).ToArray();

    public Seg[] SegsOf(StructDef structDef, params SegmentState[] states)
    {
        var predicate = states.Length == 0
            ? (Func<Seg, bool>) (seg => true)
            : seg => states.Contains(seg.State);
        return SegsByStructIdMap.TryGetValue(structDef.Id, out var segs)
            ? segs.Where(predicate).ToArray() 
            : Array.Empty<Seg>();
    }
    
    public Seg[] UsableSegsOf(StructDef structDef, SegmentState state) =>
        UsableSegsMap.TryGetValue(structDef.Id, out var segsByState) 
            ? segsByState.TryGetValue(state, out var segs) ? segs.ToArray() : Array.Empty<Seg>()
            : Array.Empty<Seg>();

    public void AddOrUpdate(StructDef structDef)
    {
        DataStructureDefs.Add(structDef);
        DataStructuresByNameMap.AddOrUpdate(structDef.Name, new HashSet<StructDef>(){structDef},
            (_, hs) => { hs.Add(structDef);
                return hs;
            });
        DataStructuresByIdMap.AddOrUpdate(structDef.Id, structDef, (_, __) => structDef);
    }
    
    public void AddOrUpdate(Seg seg)
    {
        LastPkByStructIdMap.AddOrUpdate(seg.StructDefId, seg.LastPk, (_, pk)=> Math.Max(pk, seg.LastPk));
        SegsByStructIdMap.AddOrUpdate(seg.StructDefId, 
            new SortedSet<Seg>(new ByPkComparer()){seg}, 
            (_, sgs) =>
            {
                sgs.RemoveWhere(sg => sg.OwnOffset == seg.OwnOffset);
                sgs.Add(seg);
                return sgs;
            });
        
        UsableSegsMap.AddOrUpdate(seg.StructDefId,
            _ =>
            {
                var segs = new ConcurrentDictionary<SegmentState, SortedSet<Seg>>();
                if (seg.State != SegmentState.NonUsable)
                {
                    segs[seg.State] = new SortedSet<Seg>(new ByOwnOffsetComparer()) {seg};
                }
                return segs;
            }, 
            (_, sgs) =>
            {
                foreach (var state in sgs.Keys)
                {
                    sgs[state].RemoveWhere(sg => sg.OwnOffset == seg.OwnOffset);
                }

                if (seg.State != SegmentState.NonUsable)
                {
                    sgs.AddOrUpdate(seg.State, new SortedSet<Seg>(new ByOwnOffsetComparer()){seg}, (_, set) =>
                    {
                        set.Remove(seg);
                        set.Add(seg);
                        return set;
                    }); 
                }
                return sgs;
            });
    }
}