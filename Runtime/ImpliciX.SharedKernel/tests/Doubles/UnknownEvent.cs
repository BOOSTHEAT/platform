using System;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Tests.Doubles
{
    public class UnknownEvent : PublicDomainEvent
    {
        public UnknownEvent() : base(Guid.NewGuid(), TimeSpan.Zero)
        {
        }
        
    }
}