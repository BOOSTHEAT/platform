using System;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Tests.Doubles
{
    public class FooEvent : PublicDomainEvent
    {
        public FooEvent() : base(Guid.NewGuid(), TimeSpan.Zero)
        {
        }

        public int Foo { get; set; }
    }
}