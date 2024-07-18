using ImpliciX.Data.Factory;
using ImpliciX.TestsCommon;
using NUnit.Framework;

namespace ImpliciX.FmuDriver.Tests
{
  [SetUpFixture]
  public class Setup
  {
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
      EventsHelper.ModelFactory = new ModelFactory(typeof(fake).Assembly);
    }
  }
}