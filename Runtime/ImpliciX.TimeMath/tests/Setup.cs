using ImpliciX.Data.Factory;
using ImpliciX.TestsCommon;
using NUnit.Framework;

namespace ImpliciX.TimeMath.Tests;

[SetUpFixture]
public class Setup
{
  [OneTimeSetUp]
  public void RunBeforeAnyTests()
  {
    EventsHelper.ModelFactory = new ModelFactory(typeof(Fake).Assembly);
  }
}
