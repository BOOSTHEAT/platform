using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Data.HotDb;
using ImpliciX.Data.HotDb.Model;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.Language.Records;
using static ImpliciX.Data.Records.HotRecords.Ext;
using static ImpliciX.Data.Records.HotRecords.HotRecordsDb;
using Db = ImpliciX.Data.HotDb.HotDb;
namespace ImpliciX.Data.Records.HotRecords;

public class HotRecordsDb : IHotRecordsDb
{
    public record FieldDescriptor(string Name, Type ModelType, StdFields.StorageType StorageType,  int SizeInBytes);
    public record RecordDescriptor(Urn Urn, int Retention, FieldDescriptor[] Fields);
    
    private readonly IHotDb _hotDb;
    public HotRecordsDb(IRecord[] records, string folderPath, string dbName, bool safeLoad = false)
    {
        if(records == null || records.Length == 0) throw new InvalidOperationException("records must be defined");
        if (folderPath == null) throw new ArgumentNullException(nameof(folderPath));
        DbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
        _hotDb = Directory.Exists(folderPath) switch
        {
            true => Db.Load(folderPath, dbName, safeLoad),
            false => Db.Create(folderPath, dbName)
        }; 
        DefineMissingStructures(records, _hotDb);
        DefinedStructs = _hotDb.DefinedStructs;
        Retentions = records.ToDictionary(r => r.Urn, r => r.Retention);
    }

    public Dictionary<Urn,Option<int>> Retentions { get; set; }

    private static void DefineMissingStructures(IRecord[] records, IHotDb hotDb)
    {
        var recordsDescriptors = records
            .Where(r => r.Retention.IsSome)
            .ToDictionary(r => r.Urn, Descriptor);

        var actualStructs = hotDb.DefinedStructs.ToHashSet();
        
        var neededStructs = recordsDescriptors
            .Select(it => new StructDef(
                Guid.NewGuid(), 
                it.Key,
                (ushort) it.Value.Retention,
                it.Value.Fields.Select(fd=>new FieldDef(fd.Name, fd.ModelType.FullName!, (byte)fd.StorageType, fd.SizeInBytes))
                    .Prepend(StdFields.Create<long>("at"))
                    .Prepend(StdFields.Create<long>("_id"))
                    .ToArray()))
            .ToHashSet();
        
        var missingStructs = neededStructs.Except(actualStructs,StructDef.StructuralCmp).ToArray();
        
        foreach (var missingStruct in missingStructs) 
            hotDb.Define(missingStruct);
        

    }

    public StructDef[] DefinedStructs { get; set; }

    public string DbName { get; set; }

    public void Write(Snapshot snapshot)
    {
        if(IsDisposed) throw new ObjectDisposedException(nameof(HotRecordsDb));
        var structure = FindDbStructure(snapshot);
        if(structure == null) throw new InvalidOperationException($"No structure found for {snapshot.RecordUrn.Value}");
        var bytes = snapshot.ToBytes(structure);
        _hotDb.Upsert(structure, snapshot.Id, bytes);
        var count = _hotDb.Count(structure);
        Retentions[snapshot.RecordUrn].Tap(retention =>
        {
            if (count > retention)
            {
                var first = _hotDb.GetFirst(structure.Name).AsSpan();
                _hotDb.Delete(structure.Name, BitConverter.ToInt64(first[..8]));
            }
        });
        
        
    }

    private StructDef FindDbStructure(Snapshot snapshot)
    {
        var definedStructures = _hotDb.DefinedStructsByName[snapshot.RecordUrn.Value];
        var structure =
            definedStructures
                .Reverse()
                .FirstOrDefault(
                s =>
                {
                    var dbStructFields = s.Fields.Select(f => f.Name).Skip(1).ToHashSet();
                    var snapshotFields = snapshot.Values.Select(it => it.Urn.Value).ToHashSet();
                    return snapshotFields.IsSubsetOf(dbStructFields);
                });
        return structure;
    }

    public IReadOnlyList<Snapshot> ReadAll(Urn recordUrn)
    {
         if(IsDisposed) throw new ObjectDisposedException(nameof(HotRecordsDb));
         var results = _hotDb.GetAll(recordUrn);
         var snapshots = new List<Snapshot>();
         foreach (var (structDef, snapshotsBytes) in results)
         {
             foreach (var bytes in snapshotsBytes)
             {
                 snapshots.Add(SnapshotFromBytes(structDef, bytes));
             }
         }
         return snapshots;
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        _hotDb?.Dispose();
        IsDisposed = true;
    }

    private bool IsDisposed { get; set; }
}

static class Ext
{
    public static RecordDescriptor Descriptor(IRecord record)
        {
            var skipProperties = new HashSet<string>(new[] {"Parent", "Token", "Urn"});
            var recordType = record.Type;
            var props = recordType.GetProperties();
            var fields = props.SelectMany(p => GetDescriptor(p)).ToArray();
            
            return new RecordDescriptor(record.Urn,record.Retention.GetValueOrDefault(0), fields);
       

            FieldDescriptor[] GetDescriptor(PropertyInfo property, IEnumerable<PropertyInfo> parents=null)
            {
                parents ??= new List<PropertyInfo>();
                if (property.PropertyType.IsGenericType &&
                    property.PropertyType.GetGenericTypeDefinition() == typeof(PropertyUrn<>))
                {
                    var propType = property.PropertyType.GenericTypeArguments[0];
                    var fieldName = string.Join(":", parents.Append(property).Select(p => p.Name));
                    var field = StdFields.Create(fieldName, propType);
                    return new[] {new FieldDescriptor(field.Name, propType, (StdFields.StorageType) field.StorageType, field.FixedSizeInBytes)};
                }
                if(typeof(ModelNode).IsAssignableFrom(property.PropertyType) && !skipProperties.Contains(property.Name))
                {
                    return property.PropertyType.GetProperties()
                        .SelectMany(p => GetDescriptor(p, parents.Append(property)))
                        .ToArray();
                }
                return Array.Empty<FieldDescriptor>();
            }
        }
    
    public static byte[] ToBytes(this Snapshot snapshot, StructDef structDef)
    {
        var payloadBytes = new byte[structDef.BlockPayloadSize];
        
        using var ms = new MemoryStream(payloadBytes);
        using var bw = new BinaryWriter(ms);
        bw.Write(snapshot.Id);
        bw.Write(snapshot.At.Ticks);
        var snapshotValues = snapshot.Values.ToDictionary(it => it.Urn.Value, it => it.ModelValue());
        foreach (var field in structDef.Fields[2..])
        {
            field.WriteTo(bw, snapshotValues.GetValueOrDefault(field.Name,null));
        }
        return ms.ToArray();
    }

    public static Snapshot SnapshotFromBytes(StructDef structDef, byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var br = new BinaryReader(ms);
        var id = br.ReadInt64();
        var at = new TimeSpan(br.ReadInt64());
        return new Snapshot(id, structDef.Name, 
            (from field in structDef.Fields[2..]
                let fieldRawValue = field.ReadFrom(br)
                where !fieldRawValue.Equals(float.NaN) && !fieldRawValue.Equals(string.Empty)
                let dataModelValue = DynamicModelFactory.Create(field.ModelTypeName, field.Name, fieldRawValue, at)
                select dataModelValue).ToArray(), at);
    }
    
}
