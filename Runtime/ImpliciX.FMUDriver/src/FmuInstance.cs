using System;
using System.Collections.Generic;
using System.Linq;
using Femyou;
using ImpliciX.SharedKernel.Clock;
using Serilog;

namespace ImpliciX.FmuDriver
{
    public class FmuInstance : IFmuInstance
    {
        private readonly IVariable[] _writtenParameterVariables;
        private readonly IClock _clock;

        private IInstance FmuApiInstance { get; set; }
        public bool FmuStarted { get; private set; }
        private const double SimulationStartTime = 0.0f;
        private Func<IInstance> CreateSimulationInstance { get; }

        public FmuInstance(Func<IInstance> createSimulationInstance, IVariable[] writtenParameterVariables, IClock clock)
        {
            CreateSimulationInstance = createSimulationInstance;
            _writtenParameterVariables = writtenParameterVariables;
            _clock = clock;
        }

        public void StartSimulation()
        {
            Log.Information("Simulation started.");
            FmuApiInstance.StartTime(SimulationStartTime);
            FmuStarted = true;
        }

        public void StopSimulation()
        {
            Log.Information("Simulation stopped.");
            FmuStarted = false;
        }

        public void CreateNewSimulation(FmuContext fmuContext)
        {
            FmuApiInstance = CreateSimulationInstance();
            InitializeModelFiles(fmuContext);
        }

        private void InitializeModelFiles(FmuContext fmuContext)
        {
            foreach (var (key, parameterFilePath, writer) in fmuContext.FmuWrittenParametersMap)
            {
                var variable = _writtenParameterVariables.Single(c => c.Name == key);
                writer(parameterFilePath, this, variable);
            }
        }

        public void Dispose() => FmuApiInstance?.Dispose();

        public void AdvanceTime(double settingsSimulationTimeStep)
        {
            _clock.Advance(TimeSpan.FromSeconds(settingsSimulationTimeStep));
            FmuApiInstance.AdvanceTime(settingsSimulationTimeStep);
        }

        public IEnumerable<double> Read(IEnumerable<IVariable> fmuVariables) => FmuApiInstance.ReadReal(fmuVariables);

        public void WriteBoolean((IVariable, bool) valueTuple) => FmuApiInstance.WriteBoolean(valueTuple);

        public void WriteReal((IVariable, double) valueTuple) => FmuApiInstance.WriteReal(valueTuple);

        public void WriteString(params (IVariable, string)[] valueTuples) => FmuApiInstance.WriteString(valueTuples);
    }
}