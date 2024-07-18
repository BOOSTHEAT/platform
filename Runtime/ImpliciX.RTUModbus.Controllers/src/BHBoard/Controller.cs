using System.Collections.Generic;
using ImpliciX.Driver.Common;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using static ImpliciX.RTUModbus.Controllers.BHBoard.State;

namespace ImpliciX.RTUModbus.Controllers.BHBoard
{
    public class Controller : AbstractSlaveController<IBoardSlave, State>
    {
        public Controller(IBoardSlave boardSlave, ModbusSlaveModel slaveModel, FirmwareUpdateContext context,
            DomainEventFactory domainEventFactory, DriverStateKeeper driverStateKeeper, State? currentState = null) : base(boardSlave,
            domainEventFactory, driverStateKeeper, currentState)
        {
            _fsm = Fsm.Create(boardSlave, slaveModel, domainEventFactory, context);
            ControllersCommandsUrns = new HashSet<Urn>(new Urn[]
            {
                slaveModel.Commit,
                slaveModel.Rollback,
            });
            TimersUrns = new HashSet<Urn>();
        }

        public override bool CanHandle(DomainEvent trigger)
        {
            return trigger switch
            {
                CommandRequested cr => (ControllersCommandsUrns.Contains(cr.Urn) || _boardSlave.IsConcernedByCommandRequested(cr.Urn)),
                PropertiesChanged pc => pc.ContainsAny(_boardSlave.SettingsUrns),
                SystemTicked _ => IsEnabled(),
                PrivateDomainEvent pe => IsEnabled() && CanHandlePrivateEvent(pe),
                TimeoutOccured to => IsEnabled() && TimersUrns.Contains(to.TimerUrn),
                _ => false,
            };
        }

        protected override bool CanHandlePrivateEvent(PrivateDomainEvent privateDomainEvent)
        {
            return IsEnabled() && 
                   privateDomainEvent is IBHBoardPrivateEvent pe &&
                   pe.DeviceNode.Urn.Equals(_boardSlave.DeviceNode.Urn);
        }

        protected override bool IsEnabled() => !(CurrentState is Disabled);

        protected override HashSet<Urn> ControllersCommandsUrns { get; }
        protected override HashSet<Urn> TimersUrns { get; }
    }
}