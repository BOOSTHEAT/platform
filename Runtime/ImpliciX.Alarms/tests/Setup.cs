using ImpliciX.Data.Factory;
using ImpliciX.TestsCommon;
using NUnit.Framework;

namespace ImpliciX.Alarms.Tests
{
    [SetUpFixture]
    public class Setup
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            TheModelFactory = new ModelFactory(typeof(fake).Assembly);
            EventsHelper.ModelFactory = TheModelFactory;
        }

        public static ModelFactory TheModelFactory { get; private set; }
    }
}