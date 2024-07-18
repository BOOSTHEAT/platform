using ImpliciX.SharedKernel.Clock;
using Moq;

namespace ImpliciX.Records.Tests;

public class TimeBasedIdentityGeneratorTests
{

    [Test]
    public void unique_id_properties_tests()
    {
        var clock = new Mock<IClock>();
        
        clock.Setup(o => o.DateTimeNow()).Returns(DateTime.UtcNow);
        
        var generator = new TimeBasedIdentityGenerator(clock.Object);
        var ids = new HashSet<long>();
        var previous = 0L;
        for (var i = 0; i < 10_000; i++)
        {
            var id = generator.Next("foo");
            
            if (ids.Contains(id)) Assert.Fail($"Step {i} collision detected");
            if (id < previous) Assert.Fail($"Step {i} id should be strictly greater than previous");
            previous = id;
            ids.Add(id);
        }
    }
}