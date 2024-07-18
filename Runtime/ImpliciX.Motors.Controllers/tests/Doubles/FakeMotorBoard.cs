using System.Collections.Generic;
using System.Linq;
using ImpliciX.Driver.Common;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Motors.Controllers.Tests.Doubles
{
    public class FakeMotorBoard : IBoardSlave
    {
        public FakeMotorBoard(DeviceNode deviceNode)
        {
            DeviceNode = deviceNode;
            SettingsUrns = new Urn[] {};
        }
        
        public List<IExecuteCommandSimulation> CommandExecutionSimulations { get; set; }
        
        public uint ReadPaceInSystemTicks { get; set; }
        public ReadAndDecodeSimulatedResults ReadAndDecodeRegulationSimulation { get; set; }

        public DeviceNode DeviceNode { get; }
        public string Name { get; } = "FakeMotorBoard";
        public Urn[] SettingsUrns { get; set; }

        public Result2<IDataModelValue[], CommunicationDetails> ExecuteCommand(Urn commandUrn, object arg)
        {
            return CommandExecutionSimulations
                .Single(def => def.CommandUrn.Equals(commandUrn))
                .Simulate(arg);
        }

        public Result2<IDataModelValue[], CommunicationDetails> ReadProperties(MapKind mapKind)
        {
            return ReadAndDecodeRegulationSimulation.Simulate();
        }

        public bool IsConcernedByCommandRequested(Urn crUrn)
        {
            return CommandExecutionSimulations.Any(def => def.CommandUrn.Equals(crUrn));
        }
    }
}