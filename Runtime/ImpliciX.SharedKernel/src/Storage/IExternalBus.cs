using System;

namespace ImpliciX.SharedKernel.Storage
{
    public interface IExternalBus
    {
   
        void SubscribeAllKeysModification(int db, Action<string> callback);
        void SubscribeChannel(Action<string, string> callback, string channelPattern);
        void UnsubscribeAll();
    }
}