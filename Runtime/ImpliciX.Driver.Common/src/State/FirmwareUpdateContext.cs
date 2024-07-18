using System;
using System.Diagnostics.Contracts;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Driver.Common.State
{
    public class FirmwareUpdateContext
    {
        private readonly DeviceNode _deviceNode;
        private DriverState _state;

        public FirmwareUpdateContext(DeviceNode deviceNode)
        {
            _deviceNode = deviceNode;
            _state = new DriverState(deviceNode.Urn);
        }
        
        public Result<T> TryGet<T>(string key=null)
        {
            key ??= Key(typeof(T));
            return _state.GetValue<T>(key);
        }


        public T GetOrDefault<T>(T defaultValue, string key=null)
        {
            return TryGet<T>(key).GetValueOrDefault(defaultValue);
        }

        public bool Contains<T>() => 
            _state.Contains(Key(typeof(T)));

        public Unit Set<T>(T data,string key=null)
        {
            key ??= Key(typeof(T));
            Contract.Assert(!_state.Contains(key), $"Name {key} already stored");
            _state.WithValue(key, data);
            return default;
        }
        public Unit Reset()
        {
            _state = new DriverState(_deviceNode.Urn);
            return default;
        }

        private static string Key(Type type)
        {
            return type.Name;
        }
    }
}