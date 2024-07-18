using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Data.HotDb;
using ImpliciX.Data.HotDb.Model;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using Db = ImpliciX.Data.HotDb.HotDb;

namespace ImpliciX.Data.HashDb;

public class HashDb : IHashDb, IDisposable
{
  public HashDb(ModelFactory modelFactory, string folderPath, string dbName, bool safeLoad = false)
  {
    _modelFactory = modelFactory;
    _db = Directory.Exists(folderPath) && Directory.EnumerateFiles(folderPath).Any()
      ? Db.Load(folderPath, dbName, safeLoad)
      : Db.Create(folderPath!, dbName);
  }

  private readonly ModelFactory _modelFactory;
  private readonly IHotDb _db;
  private readonly FieldDef _pkField = StdFields.Create("at", typeof(long));
  private const int BlocksPerSegment = 100;

  public Result<HashValue> Read(string key)
  {
    try
    {
      var result =
        from valueType in GetTypeOfKey(key)
        let fieldType = valueType == typeof(FunctionDefinition) ? typeof(float) : valueType
        let bytes = _db.GetLast(key).Skip(8)
        from hashValue in BytesToHash(key, bytes, fieldType)
        select hashValue;
      return result;
    }
    catch (Exception e)
    {
      return new Error(nameof(Read), e.Message);
    }
  }

  public Result<IEnumerable<HashValue>> ReadAll() => _db
    .DefinedStructsByName
    .Where(kv => _db.Count(kv.Value.First()) > 0)
    .Select(kv => Read(kv.Key))
    .Traverse();

  public Result<Unit> Write(HashValue hash)
  {
    var pk = (long)0;
    var result =
      from valueType in GetTypeOfKey(hash.Key)
      let fieldType = valueType == typeof(FunctionDefinition) ? typeof(float) : valueType
      from encoded in HashToBytes(hash, fieldType)
      let action = new Action( () =>
        _db.Upsert(encoded.Def, pk, _pkField.ToBytes(pk).Concat(encoded.Bytes).ToArray())) 
      select action;
    if(result.IsSuccess)
      result.Value();
    return result.Select(x => new Unit());
  }

  public Result<Unit> DeleteAll()
  {
    var result =
      from key in _db.DefinedStructsByName.Keys
      let deleted = _db.Delete(key, 0)
      let outcome = deleted==1
        ? Result<Unit>.Create(new Unit())
        : new Error(nameof(DeleteAll), $"Cannot delete {key}")
      select outcome;
    var doDelete = result.ToArray();
    return doDelete.Traverse().Select(x => x.First());
  }

  private Result<Type> GetTypeOfKey(string key)
  {
    var result =
      from urn in _modelFactory.CreateUrnWithBackwardCompatibility(key)
      let valueType = ValueObjectsFactory.ValueTypeFromUrnType(urn.GetType())
      select valueType;
    return result;
  }
  
  private Result<HashValue> BytesToHash(string key, IEnumerable<byte> bytes, Type fieldType)
  {
    var result =
      from def in GetExistingStructDef(key)
      from values in (
        from chunk in def.Fields.Skip(1).Zip(bytes.Chunk(4))
        let value = chunk.First.FromBytes(chunk.Second)
        let fieldAndValue = (
          from strValue in Converter.ToString(fieldType, value)
          select (chunk.First.Name,strValue)
        )
        select fieldAndValue
      ).Traverse()
      let hashValue = new HashValue(key, values.ToArray())
      select hashValue;
    if(result.IsSuccess && result.Value.Values.Length < 1)
      return new Error(nameof(BytesToHash), $"No data for {key}");
    return result;
  }

  private Result<(StructDef Def,byte[] Bytes)> HashToBytes(HashValue hash, Type fieldType)
  {
    var def = GetOrCreateStructDef(hash, fieldType);
    var result = hash
      .ValuesWithoutAtField
      .Select(f => Converter.FromString(fieldType, f.Value))
      .Traverse()
      .Select(values => def
        .Fields.Skip(1).Zip(values)
        .SelectMany(fieldAndValue => fieldAndValue.First.ToBytes(fieldAndValue.Second))
        .ToArray()
      )
      .Select(bytes => (def,bytes));
    return result;
  }

  private Result<StructDef> GetExistingStructDef(string name) =>
    _db.IsDefined(name)
      ? _db.DefinedStructsByName[name].First()
      : new Error(nameof(GetExistingStructDef), $"Cannot find StructDef {name}");

  private StructDef GetOrCreateStructDef(HashValue hash, Type fieldType)
  {
    if (_db.IsDefined(hash.Key))
      return _db.DefinedStructsByName[hash.Key].First();
    var fields =
      hash.ValuesWithoutAtField.Select(f => StdFields.Create(f.Name, fieldType));
    var def = new StructDef(Guid.NewGuid(), hash.Key, BlocksPerSegment, fields.Prepend(_pkField).ToArray());
    _db.Define(def);
    return def;
  }

  public void Dispose() => _db.Dispose();
}