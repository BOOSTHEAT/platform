using ImpliciX.Driver.Common;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.RuntimeFoundations.Factory;

namespace ImpliciX.RTUModbus.Controllers.VendorBoard
{
    public class FsmActions : FsmActionsBase
    {
        public FsmActions(IBoardSlave boardSlave, DomainEventFactory domainEventFactory) : base(boardSlave, domainEventFactory)
        {
        }
    }
}