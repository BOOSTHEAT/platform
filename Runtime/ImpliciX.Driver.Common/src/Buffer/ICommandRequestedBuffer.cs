using System.Collections.Generic;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Driver.Common.Buffer
{
    public interface ICommandRequestedBuffer
    {
        void ReceivedCommandRequested(CommandRequested commandRequested);
        IEnumerable<CommandRequested> ReleaseCommandRequested();
    }
}