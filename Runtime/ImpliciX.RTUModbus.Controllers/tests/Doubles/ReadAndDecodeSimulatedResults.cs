using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.RTUModbus.Controllers.Tests.Doubles
{
    public class ReadAndDecodeSimulatedResults
    {
        private readonly Queue<Result2<IDataModelValue[],CommunicationDetails>> _simulatedResults;
        private readonly int _size;

        public ReadAndDecodeSimulatedResults(List<Result2<IDataModelValue[],CommunicationDetails>> simulatedResults)
        {
            _simulatedResults = new Queue<Result2<IDataModelValue[],CommunicationDetails>>(simulatedResults);
            _size = simulatedResults.Count;
        }

        public Result2<IDataModelValue[],CommunicationDetails> Simulate()
        {
            if(_simulatedResults.Count==0)
                throw new ApplicationException("Simulation ended.");

            return _simulatedResults.Dequeue();
        }
    }
}