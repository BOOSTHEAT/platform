using ImpliciX.SharedKernel.Storage;
using StackExchange.Redis;

namespace ImpliciX.SharedKernel.Redis.Tests
{
    public class Helper
    {
        public readonly StorageConnectionString ConnectionString;
        private readonly IConnectionMultiplexer _multiplexer;

        public Helper()
        {
            var myServer = $"127.0.0.1:{RedisTestContainer.REDIS_PORT_LOCAL}";
            ConnectionString = new StorageConnectionString { ConnectionString = myServer };
            _multiplexer = ConnectionMultiplexer.Connect(myServer, options => options.AbortOnConnectFail = false);
            var server = ConnectionMultiplexer.Connect(new ConfigurationOptions() { AbortOnConnectFail = false, AllowAdmin = true, EndPoints = { myServer } })
                .GetServer(myServer);
            server.Execute("config", "set", "notify-keyspace-events", "KEA");
            server.FlushAllDatabases();
        }

        public IDatabase DB(int index) => _multiplexer.GetDatabase(index);

        public ISubscriber Subscriber => _multiplexer.GetSubscriber();
        
    }
}