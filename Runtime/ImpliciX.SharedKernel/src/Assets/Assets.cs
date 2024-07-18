using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data;


namespace ImpliciX.SharedKernel.Assets
{
    public class Assets : IEnumerable<Asset>
    {
        private readonly Dictionary<Type, Asset> _assets;

        public Assets(object[] assets)
        {
            _assets = new Dictionary<Type, Asset>();
            foreach (var asset in assets)
            {
                _assets.Add(asset.GetType(),new Asset(asset));
            }
        }

        public T Get<T>() where T : class
        {
            var assetType = typeof(T);
            Debug.PreCondition(()=>ContainsType(assetType),()=>$"{assetType.Name} does not exist");
            if(_assets.ContainsKey(assetType)) 
                return _assets[assetType].Get<T>();
            else
            {
                var candidateType = _assets.Keys.Single(assetType.IsAssignableFrom);
                return _assets[candidateType].Get<T>();
            }
        }

        private bool ContainsType(Type assetType)
        {
            return _assets.ContainsKey(assetType) || _assets.Keys.Any(assetType.IsAssignableFrom);
        }

        public void Add(object asset)
        {
            var assetType = asset.GetType();
            Debug.PreCondition(()=>!_assets.ContainsKey(assetType),()=>$"{assetType.Name} already exists");
            _assets.Add(assetType,new Asset(asset));
        }

        public void DisposeAll()
        {
            foreach (var asset in _assets.Values)
            {
                asset.Dispose();
            }
        }

        public IEnumerator<Asset> GetEnumerator() => _assets.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        
    }
}