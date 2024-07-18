using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Logger;

namespace ImpliciX.FrozenTimeSeries.Tests;

[SetUpFixture]
public class Fixture
{
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Log.Logger = new SerilogLogger(Serilog.Core.Logger.None);
    }
    
}