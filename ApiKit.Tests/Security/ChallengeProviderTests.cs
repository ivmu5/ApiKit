using ApiKit.Management;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Security;
using ApiKit.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiKit.Tests.Security;

public sealed class ChallengeProviderTests
{
    private static readonly ManagementPeerIdentity Host = ManagementPeerIdentity.ForManagementHost("host");
    private static readonly ManagementPeerIdentity Service = new ManagementPeerIdentity("service", ManagementPeerKind.ManagedService) { InstanceId = "instance-1" };
    private static readonly DateTimeOffset Start = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Challenge_is_single_use_even_under_parallel_consumers()
    {
        using var fixture = Create();
        var challenge = await fixture.Provider.CreateChallengeAsync(Host, Service, "named-pipe", "registration", 6);
        var attempts = await Task.WhenAll(Enumerable.Range(0, 64).Select(async _ =>
            await fixture.Provider.ConsumeChallengeAsync(challenge.ChallengeId)));
        Assert.Single(attempts.Where(static x => x is not null));
        Assert.Null(await fixture.Provider.ConsumeChallengeAsync(challenge.ChallengeId));
    }

    [Fact]
    public async Task Created_and_consumed_challenges_are_defensive_copies()
    {
        using var fixture = Create();
        var challenge = await fixture.Provider.CreateChallengeAsync(Host, Service, "named-pipe", "registration", 6);
        var original = challenge.Nonce.ToArray();
        challenge.Nonce.AsSpan().Fill(0);

        var consumed = await fixture.Provider.ConsumeChallengeAsync(challenge.ChallengeId);
        Assert.NotNull(consumed);
        Assert.Equal(original, consumed.Nonce);
        consumed.Nonce.AsSpan().Fill(0);
        Assert.Null(await fixture.Provider.ConsumeChallengeAsync(challenge.ChallengeId));
    }

    [Fact]
    public async Task Expired_challenge_is_rejected_and_pending_capacity_recovered()
    {
        using var fixture = Create(maxPending: 1, lifetime: TimeSpan.FromSeconds(10));
        var challenge = await fixture.Provider.CreateChallengeAsync(Host, Service, "named-pipe", "registration", 6);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fixture.Provider.CreateChallengeAsync(Host, Service, "named-pipe", "registration", 6));
        fixture.Clock.Advance(TimeSpan.FromSeconds(11));
        Assert.Null(await fixture.Provider.ConsumeChallengeAsync(challenge.ChallengeId));
        var replacement = await fixture.Provider.CreateChallengeAsync(Host, Service, "named-pipe", "registration", 6);
        Assert.NotEqual(challenge.ChallengeId, replacement.ChallengeId);
    }

    [Fact]
    public async Task Nonce_is_random_and_challenge_is_bound_to_expected_context()
    {
        using var fixture = Create();
        var one = await fixture.Provider.CreateChallengeAsync(Host, Service, "named-pipe", "registration", 6);
        var two = await fixture.Provider.CreateChallengeAsync(Host, Service, "named-pipe", "registration", 6);
        Assert.NotEqual(one.ChallengeId, two.ChallengeId);
        Assert.NotEqual(Convert.ToHexString(one.Nonce), Convert.ToHexString(two.Nonce));
        Assert.Equal(32, one.Nonce.Length);
        Assert.Equal(Host, one.Issuer);
        Assert.Equal(Service, one.Subject);
        Assert.Equal("registration", one.Purpose);
        Assert.Equal(6, one.ProtocolVersion);
        Assert.Equal(Start, one.IssuedAtUtc);
        Assert.Equal(Start.AddSeconds(30), one.ExpiresAtUtc);
    }

    [Fact]
    public async Task Invalid_identifiers_and_cancellation_are_rejected()
    {
        using var fixture = Create();
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await fixture.Provider.CreateChallengeAsync(Host, Service, " ", "registration", 6));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await fixture.Provider.CreateChallengeAsync(Host, Service, "named-pipe", "registration", 0));
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await fixture.Provider.CreateChallengeAsync(Host, Service, "named-pipe", "registration", 6, canceled.Token));
    }

    [Theory]
    [InlineData(0, 32, 100)]
    [InlineData(1, 15, 100)]
    [InlineData(1, 129, 100)]
    [InlineData(1, 32, 0)]
    [InlineData(1, 32, 65537)]
    public void Invalid_options_fail_options_validation(int lifetimeSeconds, int nonce, int maxPending)
    {
        var services = new ServiceCollection();
        services.AddApiKitManagementSecurity(options =>
        {
            options.ChallengeLifetime = TimeSpan.FromSeconds(lifetimeSeconds);
            options.NonceSizeBytes = nonce;
            options.MaximumPendingChallenges = maxPending;
        });
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<ApiKit.Management.Options.ManagementSecurityOptions>>().Value);
    }

    private static Fixture Create(int maxPending = 16, TimeSpan? lifetime = null)
    {
        var clock = new ManualTimeProvider(Start);
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);
        services.AddApiKitManagementSecurity(options =>
        {
            options.MaximumPendingChallenges = maxPending;
            if (lifetime.HasValue) options.ChallengeLifetime = lifetime.Value;
        });
        var provider = services.BuildServiceProvider();
        return new Fixture(provider.GetRequiredService<IManagementChallengeProvider>(), clock, provider);
    }

    private sealed record Fixture(IManagementChallengeProvider Provider, ManualTimeProvider Clock,
        ServiceProvider Services) : IDisposable
    {
        public void Dispose() => Services.Dispose();
    }
}
