using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.SharedKernel.Storage;
using StackExchange.Redis;

namespace ImpliciX.SharedKernel.Redis
{
    public class RedisReader : IReadFromStorage
    {
        public StorageConnectionString StorageConnection { get; }
        public readonly IConnectionMultiplexer Multiplexer;

        public RedisReader(StorageConnectionString storageConnection)
        {
            StorageConnection = storageConnection;
            Multiplexer = ConnectionMultiplexer.Connect(storageConnection.ConnectionString);
        }

        public HashValue ReadHash(int db, string key)
        {
            bool IsValid(HashEntry he)
            {
                return he.Name.HasValue && he.Value.HasValue;
            }

            var hashEntries = Multiplexer.GetDatabase(db).HashGetAll(key);
            var nameValuePairs = hashEntries
                .Where(he=>IsValid(he))
                .Select(he => (he.Name.ToString(), he.Value.ToString()))
                .ToArray();
            
            return new HashValue(key,nameValuePairs);
        }

        public IEnumerable<HashValue> ReadAll(int db)
        {
            var keys = Multiplexer.GetServer(StorageConnection.ConnectionString).Keys(db, "*", Int16.MaxValue, 0, 0);
            return keys.Select(k => ReadHash(db,k));
        }
    }
}