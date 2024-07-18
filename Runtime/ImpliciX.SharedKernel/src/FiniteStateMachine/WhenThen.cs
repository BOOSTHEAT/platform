using System;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.FiniteStateMachine
{
    
    public class WhenThen
    {
        private readonly Func<DomainEvent, bool> _when;
        public WhenThen(Func<DomainEvent, bool> when)
        {
            _when = when;
        }
        
        public Func<DomainEvent, DomainEvent[]> Then(Func<DomainEvent, DomainEvent[]> f) => domain =>
            _when(domain) switch
            {
                true => f(domain),
                false => new DomainEvent[0]
            };
    }
}