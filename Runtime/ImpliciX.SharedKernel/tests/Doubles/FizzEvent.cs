using System;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Tests.Doubles
{
    public class FizzEvent : PublicDomainEvent
    {
        public FizzEvent() : base(Guid.NewGuid(), TimeSpan.Zero)
        {
        }
    }
}