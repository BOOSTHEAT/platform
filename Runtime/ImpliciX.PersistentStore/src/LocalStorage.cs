using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Data.HashDb;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.PersistentStore;

public class LocalStorage : IStorage, IReadFromStorage, IWriteToStorage, ICleanStorage
{
  public LocalStorage(ModelFactory modelFactory, string localStoragePath) :
    this(name => GetHashDb(modelFactory, localStoragePath, name))
  {
  }

  private static IHashDb GetHashDb(ModelFactory modelFactory, string localStoragePath, string name) =>
    new HashDb(modelFactory, Path.Combine(localStoragePath, "settings", name), name);

  public LocalStorage(Func<string, IHashDb> getHashDbFromName)
  {
    Listener = new ExternalBus(this);
    _hashDbs = DbNames.ToDictionary(
      kv => kv.Key,
      kv => new DbHandler(getHashDbFromName(kv.Value))
    );
  }

  private static readonly Dictionary<int, string> DbNames = new()
  {
    [1] = "user",
    [2] = "version",
    [3] = "factory",
  };

  class DbHandler
  {
    public IHashDb HashDb;
    public Action<string> Callback = _ => { };
    public DbHandler(IHashDb hashDb) => HashDb = hashDb;
  }

  private readonly Dictionary<int, DbHandler> _hashDbs;

  public IReadFromStorage Reader => this;
  public IWriteToStorage Writer => this;
  public ICleanStorage Cleaner => this;
  public IExternalBus Listener { get; }

  public HashValue ReadHash(int db, string key) =>
    Do(db,
      handler => handler.HashDb.Read(key).GetValueOrDefault(EmptyHash(key)),
      () => EmptyHash(key)
    );

  public IEnumerable<HashValue> ReadAll(int db) =>
    Do(db,
      handler => handler.HashDb.ReadAll().GetValueOrDefault(Enumerable.Empty<HashValue>()),
      () => Enumerable.Empty<HashValue>()
    );

  public Result<Unit> WriteHash(int db, HashValue value) =>
    Do(db,
      handler => handler.HashDb.Write(value).Tap(_ => handler.Callback(value.Key)),
      () => UnknownDb(nameof(WriteHash), db)
    );

  public Result<Unit> FlushDb(int db) =>
    Do(db,
      handler => handler.HashDb.DeleteAll(),
      () => UnknownDb(nameof(FlushDb), db)
    );

  private static Result<Unit> UnknownDb(string key, int id) =>
    Result<Unit>.Create(new Error(key, $"Unknown db id {id}"));

  private T Do<T>(int db, Func<DbHandler, T> onDb, Func<T> onNoDb) =>
    _hashDbs.TryGetValue(db, out var dbHandler) ? onDb(dbHandler) : onNoDb();

  private static HashValue EmptyHash(string key) => new(key, Array.Empty<(string, string)>());

  class ExternalBus : IExternalBus
  {
    private readonly LocalStorage _ls;

    public ExternalBus(LocalStorage ls)
    {
      _ls = ls;
    }

    public void SubscribeAllKeysModification(int db, Action<string> callback) =>
      _ls.Do(db, handler =>
      {
        handler.Callback = callback;
        return 0;
      }, () => 0);

    public void SubscribeChannel(Action<string, string> callback, string channelPattern)
    {
    }

    public void UnsubscribeAll()
    {
    }
  }
}