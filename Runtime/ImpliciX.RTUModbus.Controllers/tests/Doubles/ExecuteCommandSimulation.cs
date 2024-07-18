using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.RTUModbus.Controllers.Tests.Doubles
{
    public interface IExecuteCommandSimulation
    {
        Urn CommandUrn { get; }
        Result2<IDataModelValue[], CommunicationDetails> Simulate(object arg);
    }

    public class ExecuteCommandSimulation<T> : IExecuteCommandSimulation
    {
        public Result2<IDataModelValue[],CommunicationDetails> SimulationResult { get; set; }
        public CommandNode<T> CommandNode { get; }

        public Urn CommandUrn => CommandNode.command;
        
        public ExecuteCommandSimulation(CommandNode<T> commandNode)
        {
            CommandNode = commandNode;
        }
       
        public Result2<IDataModelValue[],CommunicationDetails> Simulate(object arg) => 
            SimulationResult ?? (new IDataModelValue[]
            {
                Property<T>.Create(CommandNode.measure, (T)arg, TimeSpan.Zero),
                Property<MeasureStatus>.Create(CommandNode.status, MeasureStatus.Success, TimeSpan.Zero),
            }, TestEnv.Healthy_CommunicationDetails);
    }
}