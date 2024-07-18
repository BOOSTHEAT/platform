using System.Collections.Generic;
using ImpliciX.Driver.Common;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using static ImpliciX.RTUModbus.Controllers.VendorBoard.State;

namespace ImpliciX.RTUModbus.Controllers.VendorBoard
{
    public class Controller : AbstractSlaveController<IBoardSlave,State>
    {
        protected override bool CanHandlePrivateEvent(PrivateDomainEvent privateDomainEvent) => false;
        protected override bool IsEnabled() => !(CurrentState is Disabled);
        
        public Controller(IBoardSlave boardSlave,
            DomainEventFactory domainEventFactory,
            DriverStateKeeper driverStateKeeper,
            State? fsmState = null):base(boardSlave,domainEventFactory,driverStateKeeper,fsmState)
        {
            _fsm = Fsm.Create(boardSlave, domainEventFactory);
            ControllersCommandsUrns = new HashSet<Urn>();
            TimersUrns = new HashSet<Urn>();
        }
        
        protected override HashSet<Urn> ControllersCommandsUrns { get; }
        protected override HashSet<Urn> TimersUrns { get; }
    }
}