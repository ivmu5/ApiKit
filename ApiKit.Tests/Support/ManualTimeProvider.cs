namespace ApiKit.Tests.Support;

/// <summary>
/// Provides a controllable clock for expiration and rotation tests.
/// </summary>
internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private long _utcTicks = start.ToUniversalTime().Ticks;

    public override DateTimeOffset GetUtcNow() =>
        new(Interlocked.Read(ref _utcTicks), TimeSpan.Zero);

    public void Advance(TimeSpan amount) => Interlocked.Add(ref _utcTicks, amount.Ticks);
}
