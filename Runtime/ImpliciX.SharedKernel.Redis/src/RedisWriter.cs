using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Storage;
using StackExchange.Redis;

namespace ImpliciX.SharedKernel.Redis
{
    public class RedisWriter : IWriteToStorage
    {
        public StorageConnectionString StorageConnection { get; }
        public readonly IConnectionMultiplexer Multiplexer;

        public RedisWriter(StorageConnectionString storageConnection)
        {
            StorageConnection = storageConnection;
            Multiplexer = ConnectionMultiplexer.Connect(storageConnection.ConnectionString);
        }

        public Result<Unit> WriteHash(int db, HashValue value)
        {
            return SideEffect.TryRun(() =>
                {
                    Multiplexer.GetDatabase(db).HashSet(value.Key,
                        value.Values.Select(t => new HashEntry(t.Name, t.Value)).ToArray());
                    return new Unit();
                },
                () => new RedisWriterError()
            );
        }
    }
    
    public class RedisWriterError : Error
    {
        public RedisWriterError() : base(nameof(RedisWriterError), "Error occured while storing values into Redis")
        {
        }
    }

}