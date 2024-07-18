namespace ImpliciX.FmuDriver
{
    public interface IFmuSimulation
    {
        bool FmuStarted { get; }
        void StartSimulation();
        void StopSimulation();
        void CreateNewSimulation(FmuContext context);
        void Dispose();
        void AdvanceTime(double settingsSimulationTimeStep);
    }
}