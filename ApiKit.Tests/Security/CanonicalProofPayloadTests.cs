using ApiKit.Management.Security;

namespace ApiKit.Tests.Security;

public sealed class CanonicalProofPayloadTests
{
    private static readonly DateTimeOffset Issued = new(2026, 9, 30, 11, 30, 0, TimeSpan.Zero);
    private static readonly ManagementPeerIdentity Host = ManagementPeerIdentity.ForManagementHost("host");
    private static readonly ManagementPeerIdentity Service = new("service", ManagementPeerKind.ManagedService)
    {
        InstanceId = "instance-1"
    };

    [Fact]
    public void Same_semantic_input_is_byte_for_byte_stable_and_metadata_is_not_signed()
    {
        var challenge = Example();
        var bytes = ManagementChallengeProofPayload.Create(challenge, Service, "credential-a", Issued.AddSeconds(1));
        var metadata = challenge with
        {
            Issuer = Host with { Metadata = new Dictionary<string, string> { ["display"] = "anything" } },
            Subject = Service with { Metadata = new Dictionary<string, string> { ["display"] = "anything" } }
        };
        Assert.Equal(bytes, ManagementChallengeProofPayload.Create(metadata, Service, "credential-a", Issued.AddSeconds(1)));
        Assert.Equal(bytes, ManagementChallengeProofPayload.Create(Example(), Service, "credential-a", Issued.AddSeconds(1)));
    }

    [Fact]
    public void Every_security_relevant_field_affects_payload()
    {
        var baseline = Example();
        var cases = new[]
        {
            baseline with { ChallengeId = "other" },
            baseline with { Issuer = Host with { PeerId = "other" } },
            baseline with { Subject = Service with { PeerId = "other" } },
            baseline with { Subject = Service with { InstanceId = "instance-2" } },
            baseline with { Nonce = new byte[32] },
            baseline with { Transport = "unix-socket" },
            baseline with { Purpose = "management" },
            baseline with { ProtocolVersion = 5 },
            baseline with { IssuedAtUtc = Issued.AddMilliseconds(1) },
            baseline with { ExpiresAtUtc = Issued.AddSeconds(31) }
        };
        var expected = ManagementChallengeProofPayload.Create(baseline, Service, "credential-a", Issued.AddSeconds(1));
        foreach (var changed in cases)
        {
            Assert.NotEqual(Convert.ToHexString(expected),
                Convert.ToHexString(ManagementChallengeProofPayload.Create(changed, Service, "credential-a", Issued.AddSeconds(1))));
        }
        Assert.NotEqual(Convert.ToHexString(expected),
            Convert.ToHexString(ManagementChallengeProofPayload.Create(baseline, Host, "credential-a", Issued.AddSeconds(1))));
        Assert.NotEqual(Convert.ToHexString(expected),
            Convert.ToHexString(ManagementChallengeProofPayload.Create(baseline, Service, "credential-b", Issued.AddSeconds(1))));
        Assert.NotEqual(Convert.ToHexString(expected),
            Convert.ToHexString(ManagementChallengeProofPayload.Create(baseline, Service, "credential-a", Issued.AddSeconds(2))));
    }

    [Fact]
    public void Length_prefixes_prevent_string_boundary_ambiguity()
    {
        var first = Example() with { ChallengeId = "ab", Transport = "c" };
        var second = Example() with { ChallengeId = "a", Transport = "bc" };
        Assert.NotEqual(Convert.ToHexString(ManagementChallengeProofPayload.Create(first, Service, "cred", Issued)),
            Convert.ToHexString(ManagementChallengeProofPayload.Create(second, Service, "cred", Issued)));
    }

    [Fact]
    public void Null_empty_nonce_and_invalid_dates_fail_closed()
    {
        var example = Example();
        Assert.Throws<ArgumentException>(() => ManagementChallengeProofPayload.Create(
            example with { Nonce = [] }, Service, "cred", Issued));
        Assert.Throws<ArgumentException>(() => ManagementChallengeProofPayload.Create(
            example with { ExpiresAtUtc = Issued }, Service, "cred", Issued));
        Assert.Throws<ArgumentException>(() => ManagementChallengeProofPayload.Create(
            example, Service, " ", Issued));
    }

    [Fact]
    public void Identity_comparison_requires_role_peer_id_and_instance_but_ignores_metadata()
    {
        Assert.True(ManagementPeerIdentitySecurityComparer.Equals(Service,
            Service with { Metadata = new Dictionary<string, string> { ["name"] = "untrusted" } }));
        Assert.False(ManagementPeerIdentitySecurityComparer.Equals(Service,
            Service with { Kind = ManagementPeerKind.AdminClient }));
        Assert.False(ManagementPeerIdentitySecurityComparer.Equals(Service,
            Service with { InstanceId = "instance-2" }));
        Assert.False(ManagementPeerIdentitySecurityComparer.Equals(Service,
            Service with { PeerId = "other" }));
    }

    internal static ManagementChallenge Example() => new()
    {
        ChallengeId = "challenge-1",
        Issuer = Host,
        Subject = Service,
        Nonce = Enumerable.Range(0, 32).Select(static x => (byte)x).ToArray(),
        Transport = "named-pipe",
        Purpose = "registration",
        ProtocolVersion = 6,
        IssuedAtUtc = Issued,
        ExpiresAtUtc = Issued.AddSeconds(30)
    };
}
