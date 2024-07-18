using System;
using ImpliciX.Data;

namespace ImpliciX.SharedKernel.Assets
{
    public class Asset : IAsset, IDisposable
    {
        private readonly object _asset;
        private bool _isDisposed;

        public Asset(object asset)
        {
            _isDisposed = false;
            _asset = asset;
        }

        public bool IsDisposed => _isDisposed;

        public T Get<T>() where T : class
        {
            Debug.PreCondition(()=>!_isDisposed, ()=>$"Already disposed asset {typeof(T).Name}");
            return _asset as T;
        }

        public void Dispose()
        {
            if (_asset is IDisposable disposable)
            {
                disposable.Dispose();
                _isDisposed = true;
            }
        }
    }

    public interface IAsset
    {
        bool IsDisposed { get; }
        T Get<T>() where T : class;
    }
}