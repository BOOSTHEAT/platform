using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.SharedKernel.Redis;

public class RedisStorage : IStorage
{
  public RedisStorage(StorageConnectionString storageConnection)
  {
    Reader = new RedisReader(storageConnection);
    Writer = new RedisWriter(storageConnection);
    Cleaner = new RedisCleaner(storageConnection);
    Listener = new RedisBus(storageConnection);
  }

  public IReadFromStorage Reader { get; }
  public IWriteToStorage Writer { get; }
  public ICleanStorage Cleaner { get; }
  public IExternalBus Listener { get; }
}