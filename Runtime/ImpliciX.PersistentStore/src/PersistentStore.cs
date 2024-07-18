using ImpliciX.Data.Factory;
using ImpliciX.SharedKernel.Redis;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.PersistentStore;

public class PersistentStore
{
  public static IStorage Create(PersistentStoreSettings settings, string localStoragePath, ModelFactory modelFactory)
  {
    if (string.IsNullOrEmpty(settings?.Storage?.ConnectionString))
      return new LocalStorage(modelFactory, localStoragePath);
    return new RedisStorage(settings.Storage);
  }
}