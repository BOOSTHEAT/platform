using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Logger;
using NUnit.Framework;

namespace ImpliciX.FmuDriver.Tests;

[SetUpFixture]
public class Fixture
{
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Log.Logger = new SerilogLogger(Serilog.Core.Logger.None);
    }
    
}