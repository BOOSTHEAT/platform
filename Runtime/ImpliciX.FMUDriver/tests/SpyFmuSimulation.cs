namespace ImpliciX.FmuDriver.Tests
{
    public class SpyFmuSimulation : IFmuSimulation
    {
        public bool FmuStarted { get; private set; }
        public int countSimulation { get; private set; }
        public void StartSimulation()
        {
            FmuStarted = true;
        }

        public void StopSimulation()
        {
            FmuStarted = false;
        }

        public void CreateNewSimulation(FmuContext context)
        {
            countSimulation++;
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public void AdvanceTime(double settingsSimulationTimeStep)
        {
            throw new System.NotImplementedException();
        }
    }
}