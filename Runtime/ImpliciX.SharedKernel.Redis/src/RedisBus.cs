using System;
using ImpliciX.SharedKernel.Storage;
using StackExchange.Redis;

namespace ImpliciX.SharedKernel.Redis
{
    public class RedisBus : IExternalBus
    {
        public readonly IConnectionMultiplexer Multiplexer;

        public RedisBus(StorageConnectionString storageConnection)
        {
            Multiplexer = ConnectionMultiplexer.Connect(storageConnection.ConnectionString);
        }

        public void SubscribeAllKeysModification(int db, Action<string> callback)
        {
            var subscriber = Multiplexer.GetSubscriber();
            subscriber.Subscribe($"__keyevent@{db}__:hset", (redisChannel, value) =>
            {
                callback(value);
            });
        }

        public void SubscribeChannel(Action<string, string> callback, string channelPattern)
        {
            Multiplexer.GetSubscriber()
                .Subscribe(channelPattern, (channel, value) =>
                {
                    callback(channel, value);
                });
        }
        
        public void UnsubscribeAll()
        {
            var subscriber = Multiplexer.GetSubscriber();
            subscriber.UnsubscribeAll();
        }
    }
}