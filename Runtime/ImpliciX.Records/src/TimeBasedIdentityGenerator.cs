using System.Collections.Concurrent;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Clock;

namespace ImpliciX.Records;

public class TimeBasedIdentityGenerator: IIdentityGenerator
{
    private readonly IClock _clock;
    private const int TIMESTAMP_BITS = 42;
    private const int RANDOM_BITS = 22;
    private const long MAX_RANDOM_VALUE = (1L << RANDOM_BITS) - 1; // Maximum value for the random part
    private ConcurrentDictionary<Urn, long> _counters = new();
    public TimeBasedIdentityGenerator(IClock clock)
    {
        _clock = clock;
    }

    public long Next(Urn recordUrn)
    {
        var timestamp = new DateTimeOffset(_clock.DateTimeNow()).ToUnixTimeMilliseconds() & ((1L << TIMESTAMP_BITS) - 1);
        var counter = _counters.AddOrUpdate(recordUrn, 1, (_, value) => (value + 1) & MAX_RANDOM_VALUE);
        var shiftedTimestamp = timestamp << RANDOM_BITS;
        return shiftedTimestamp | counter;
    }
}

public interface IIdentityGenerator
{
    long Next(Urn recordUrn);
}