using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;

namespace ImpliciX.Driver.Common.State
{
    public class DriverState : IDriverState
    {
        public static DriverState Empty(Urn urn) => new DriverState(urn);
        
        public static DriverState Empty() => new DriverState(Urn.BuildUrn(""));
        
        private Dictionary<string, object> Data { get; }
        public Urn Id { get; private set; }

        private DriverState()
        {
            Data = new Dictionary<string, object>();
            Id = Urn.BuildUrn("");
        }

        public DriverState(Urn id) : this()
        {
            Id = id;
        }

        public Result<T> GetValue<T>(string key)
        {
            if (!Data.ContainsKey(key))
            {
                var error = new Error("keynotfound", $"Name {key} does not exists in {Id}");
                Log.Verbose(error.Message);
                return Result<T>.Create(error);
            }
            return SideEffect.SafeCast<T>(Data[key]);
        }

        public Result<T> GetValueOrDefault<T>(string key, T defaultValue)
        {
            return SideEffect.SafeCast<T>(Data.GetValueOrDefault(key, defaultValue))
                .Match(whenError: _ => defaultValue, whenSuccess: v => v);
        }

        public IDriverState New(Urn id) => new DriverState(id);

        public IDriverState WithValue(string key, object value)
        {
            if (!Contains(key))
                Data.Add(key, value);
            else
                Data[key] = value;
            return this;
        }

        public bool Contains(string key)
        {
            return Data.ContainsKey(key);
        }

        public bool IsEmpty => !Data.Any();
    }
}