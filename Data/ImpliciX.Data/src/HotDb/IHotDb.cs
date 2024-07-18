#nullable enable
using System;
using System.Collections.Generic;
using ImpliciX.Data.HotDb.Model;

namespace ImpliciX.Data.HotDb;

public interface IHotDb: IDisposable
{
    void Define(StructDef def);
    void Upsert(StructDef structDef, long pk, byte[] bytes);
    int Delete(string structureName, long pk);
    int Delete(string structureName, long pkMin, long pkMax);
    byte[] Get(string structureName, long pk);
    Dictionary<StructDef, byte[][]> GetAll(string structureName, long? count=null, long? upTo=null);
    int Count(StructDef structDef);
    bool IsDefined(string structureName);
    byte[] GetFirst(string structureName, long? pkMin = null);
    IReadOnlyDictionary<string,HashSet<StructDef>> DefinedStructsByName { get; }
    StructDef[] DefinedStructs { get; }
    byte[] GetLast(string structureName, long? pkMax = null);
}