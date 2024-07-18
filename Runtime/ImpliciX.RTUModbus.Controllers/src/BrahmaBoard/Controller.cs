using System.Collections.Generic;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BHBoard;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RTUModbus.Controllers.BrahmaBoard
{
    public class Controller : AbstractSlaveController<IBrahmaBoardSlave, State>
    {
        public Controller(
            IBrahmaBoardSlave boardSlave,
            DomainEventFactory domainEventFactory,
            DriverStateKeeper driverStateKeeper,
            State? fsmState = null
        ) : base(boardSlave, domainEventFactory, driverStateKeeper, fsmState)
        {
            _fsm = Fsm.Create(boardSlave, domainEventFactory);
            ControllersCommandsUrns = new HashSet<Urn>(new Urn[]
            {
                boardSlave.GenericBurner._supply,
                boardSlave.GenericBurner._start_ignition,
                boardSlave.GenericBurner._stop_ignition,
                boardSlave.GenericBurner._manual_reset,
            });
            TimersUrns = new HashSet<Urn>(new Urn[]
            {
                boardSlave.GenericBurner.ignition_settings.ignition_period,
                boardSlave.GenericBurner.ignition_settings.ignition_reset_delay,
                boardSlave.GenericBurner.ignition_settings.ignition_supplying_delay
            });
        }

        protected override bool CanHandlePrivateEvent(PrivateDomainEvent privateDomainEvent) =>
            IsEnabled() &&
            privateDomainEvent is IBrahmaPrivateEvent pe &&
            pe.DeviceNode.Urn.Equals(_boardSlave.DeviceNode.Urn) ||
            privateDomainEvent is RegulationEntered entered &&
            entered.DeviceNode.Urn.Equals(_boardSlave.DeviceNode.Urn) ||
            privateDomainEvent is RegulationExited exited &&
            exited.DeviceNode.Urn.Equals(_boardSlave.DeviceNode.Urn);

        protected override bool IsEnabled() =>
            CurrentState.HasValue &&
            ((ushort)CurrentState & (ushort)State.EnabledAvailableStatesMask) != 0;

        protected override HashSet<Urn> ControllersCommandsUrns { get; }
        protected override HashSet<Urn> TimersUrns { get; }
    }
}