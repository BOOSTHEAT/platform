using System;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Driver.Dumb
{
    public class QueryStateRequested : PrivateDomainEvent
    {
        public QueryStateRequested(TimeSpan at) : base(Guid.NewGuid(), at)
        {
        }
    }
}