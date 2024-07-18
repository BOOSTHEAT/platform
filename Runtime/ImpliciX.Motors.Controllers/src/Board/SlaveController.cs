using System;
using System.Collections.Generic;
using ImpliciX.Driver.Common;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using static ImpliciX.Motors.Controllers.Board.State;

namespace ImpliciX.Motors.Controllers.Board
{
    public class SlaveController : AbstractSlaveController<IBoardSlave,State>
    {

        public override bool CanHandle(DomainEvent trigger) =>
            trigger switch
            {
                SystemTicked _ => IsEnabled() && CurrentState == Started,
                CommandRequested cr =>  IsEnabled() && (_boardSlave.IsConcernedByCommandRequested(cr.Urn) || ControllersCommandsUrns.Contains(cr.Urn)),
                TimeoutOccured timeout => IsEnabled() && CurrentState == Starting && TimersUrns.Contains(timeout.TimerUrn),
                SlaveRestarted slaveRestarted => IsEnabled() && _isHeatPumpRestarted(slaveRestarted),
                PropertiesChanged pc => pc.ContainsAny(_boardSlave.SettingsUrns), 
                _ => false,
            };

        protected override bool CanHandlePrivateEvent(PrivateDomainEvent privateDomainEvent) => false;
       
        protected override bool IsEnabled() => CurrentState != Disabled;

        protected override HashSet<Urn> ControllersCommandsUrns { get; }
        protected override HashSet<Urn> TimersUrns { get; }

        private Func<DomainEvent, bool> _isHeatPumpRestarted;
        public SlaveController(MotorsModuleDefinition motorsModuleDefinition, IBoardSlave slave, DriverStateKeeper driverStateKeeper,
            DomainEventFactory domainEventFactory, State? currentState = null)
        :base(slave,domainEventFactory,driverStateKeeper,currentState)
        {
            var fsmActions = new FsmActions(motorsModuleDefinition, slave, domainEventFactory);
            _fsm = Fsm.Create(slave, fsmActions);
            _isHeatPumpRestarted = fsmActions.IsHeatPumpRestarted;
            ControllersCommandsUrns = new HashSet<Urn>(new Urn[] { motorsModuleDefinition.SwitchCommandUrn });
            TimersUrns = new HashSet<Urn>(new Urn[] {motorsModuleDefinition.SettingsSupplyDelayTimerUrn});
            Activate();
            if (currentState != null)
            {
                CurrentState = currentState;
            }
        }
        
    }
}