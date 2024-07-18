using System;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Storage;
using StackExchange.Redis;

namespace ImpliciX.SharedKernel.Redis
{
    public class RedisCleaner : ICleanStorage
    {
        public StorageConnectionString StorageConnection { get; }
        public readonly IConnectionMultiplexer Multiplexer;

        public RedisCleaner(StorageConnectionString storageConnection)
        {
            StorageConnection = storageConnection;
            Multiplexer = ConnectionMultiplexer.Connect(new ConfigurationOptions() { AllowAdmin = true, EndPoints = { storageConnection.ConnectionString }});
        }
        public Result<Unit> FlushDb(int db)
        {
            return SideEffect.TryRun(() =>
                {
                    var server = Multiplexer.GetServer(StorageConnection.ConnectionString);
                    server.FlushDatabase(db);
                    return default(Unit);
                },e=> new RedisCleanError(db,e));
        }
    }
    
    public class RedisCleanError : Error
    {
        public RedisCleanError(int db, Exception e) : base(nameof(RedisWriterError), $"Error occured while flushing redis db {db}. {e.CascadeMessage()} ")
        {
        }
    }
}