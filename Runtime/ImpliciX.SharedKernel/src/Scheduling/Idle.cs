using System;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Scheduling
{
    public class Idle : PublicDomainEvent
    {
        public int MailboxDrainingDuration { get; }
        public int IdleCycleDuration { get; }

        public Idle(TimeSpan at, int mailboxDrainingDuration, int idleCycleDuration) : base(Guid.NewGuid(), at)
        {
            MailboxDrainingDuration = mailboxDrainingDuration;
            IdleCycleDuration = idleCycleDuration;
        }
    }
}