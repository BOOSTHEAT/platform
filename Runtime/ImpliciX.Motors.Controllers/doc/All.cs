using System;
using System.Collections.Generic;
using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.Motors.Controllers.Board;
using ImpliciX.Motors.Controllers.Settings;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.DocTools;

namespace ImpliciX.Motors.Controllers.Doc
{
  public class All
  {
    public IEnumerable<FSMViewModel> FSMs
    {
      get
      {
        var clock = new VirtualClock(DateTime.Now);
        var domainEventFactory = EventFactory.Create(new ModelFactory(Assembly.GetExecutingAssembly()), clock.Now);
        var motorsModel = new MotorsModuleDefinition();
        var root = new RootModelNode("root");
        motorsModel.MotorsDeviceNode = new HardwareAndSoftwareDeviceNode("motors", root);
        var boardSlave = new MotorsSlave(motorsModel, null, clock, new MotorsDriverSettings());
        var fsmActions = new FsmActions(motorsModel, boardSlave, domainEventFactory);
        return new FSMViewModel[] { new FSMViewModel<State>(Fsm.Create(boardSlave, fsmActions)) };
      }
    }
  }
}