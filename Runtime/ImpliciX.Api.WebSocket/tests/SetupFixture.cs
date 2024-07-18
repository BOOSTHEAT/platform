using NUnit.Framework;

namespace ImpliciX.Api.WebSocket.Tests;

[SetUpFixture]
public class Fixture
{
  [OneTimeSetUp]
  public void OneTimeSetUp()
  {
    TestsCommon.Fixture.SetupNUnitLogger();
  }
}