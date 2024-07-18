using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.PersistentStore
{
  public class PersistentStoreInitializer
  {
    public static void InsertDefaultValuesIfEmpty(IDictionary<Type, int> settingKinds,
      IWriteToStorage writer,
      IReadFromStorage reader,
      IDictionary<Urn, (string Name, string Value)[]> defaultValues, TimeSpan initTs)
    {
      if (defaultValues == null || !defaultValues.Any())
        return;
      var db = FindDb(settingKinds, defaultValues);
      if (reader.ReadAll(db).Any())
        return;
      foreach (var kv in defaultValues)
        InsertHash(writer, initTs, kv, db);
    }

    public static void AddMissingDefaultValues(IDictionary<Type, int> settingKinds,
      IWriteToStorage writer,
      IReadFromStorage reader,
      IDictionary<Urn, (string Name, string Value)[]> defaultValues, TimeSpan initTs)
    {
      if (defaultValues == null || !defaultValues.Any())
        return;
      var db = FindDb(settingKinds, defaultValues);
      var existing = reader.ReadAll(db).Select(x => x.Key).ToHashSet();
      foreach (var kv in defaultValues)
      {
        if (existing.Contains(kv.Key))
          continue;
        InsertHash(writer, initTs, kv, db);
      }
    }
    
    private static void InsertHash(IWriteToStorage writer, TimeSpan initTs, KeyValuePair<Urn, (string Name, string Value)[]> defaultValue, int db)
    {
      var hash = new HashValue(defaultValue.Key, defaultValue.Value);
      hash.SetAtField(initTs);
      writer.WriteHash(db, hash);
    }

    private static int FindDb(IDictionary<Type, int> settingKinds, IDictionary<Urn, (string Name, string Value)[]> defaultValues)
    {
      var key = defaultValues.First().Key.GetType().GetGenericTypeDefinition();
      if(!settingKinds.ContainsKey(key))
        throw new ApplicationException($"No setting kind found for {key}.");
      return settingKinds[key];
    }
  }
}