using System;
using System.Collections.Generic;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.TestsCommon
{
    public class FakeExternalBus : IExternalBus
    {
        public FakeExternalBus()
        {
        }
        public void SubscribeAllKeysModification(int db, Action<string> callback)
        {
            throw new NotImplementedException();
        }

        public void SubscribeChannel(Action<string, string> callback, string s)
        {
            throw new NotImplementedException();
        }

        public void UnsubscribeAll()
        {
            HasUnSubscribed = true;
        }

        public List<string> RecordedSubscriptions { get; }
        public bool HasUnSubscribed { get; private set; }
    }
}