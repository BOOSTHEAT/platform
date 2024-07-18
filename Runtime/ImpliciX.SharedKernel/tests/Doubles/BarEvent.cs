using System;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Tests.Doubles
{
    public class BarEvent : PublicDomainEvent
    {
        public BarEvent() : base(Guid.NewGuid(), TimeSpan.Zero)
        {
        }
    }
}