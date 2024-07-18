using System;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Tests.Doubles
{
    public class BazEvent : PrivateDomainEvent
    {
        public BazEvent() : base(Guid.NewGuid(), TimeSpan.Zero)
        {
        }
    }
}