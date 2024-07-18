using System;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Control
{
    public class Memento
    {

        public Memento()
        {
        }
        
        public Enum CurrentState { get; internal set; }
        public NotifyOnTimeoutRequested LastTimeoutRequested { get; internal set; }
    }
}