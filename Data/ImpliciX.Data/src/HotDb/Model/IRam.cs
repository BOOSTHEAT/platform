using System;
using System.Collections.Generic;

namespace ImpliciX.Data.HotDb.Model;

internal interface IRam : IReadOnlyRam
{
    void AddOrUpdate(StructDef structDef);
    void AddOrUpdate(Seg seg);
}

internal interface IReadOnlyRam
{
    bool TryGetSegments(StructDef structDef, out SortedSet<Seg> segs);
    bool TryGetStructure(string key, out StructDef structDef);

    StructDef GetStructureById(Guid id);
    
    bool ContainsStructure(string structureName);
    bool ContainsStructure(StructDef structDef);
    StructDef[] AllStructureDefinitions();

    IReadOnlyDictionary<Guid, StructDef> AllStructuresById { get; }
    IReadOnlyDictionary<string, HashSet<StructDef>> AllStructuresByName { get; }

    public Seg[] SegsOf(string structureName);
    long? LastPkOf(Guid structDefId);
    Seg[] UsableSegsOf(StructDef structDef, SegmentState state);
    Seg[] SegsOf(StructDef structDef, params SegmentState[] states);
}