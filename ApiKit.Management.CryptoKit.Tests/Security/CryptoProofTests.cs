using System.Security.Cryptography;
using ApiKit.Management.CryptoKit.Tests.Support;
using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Tests.Security;

public sealed class CryptoProofTests
{
    [Fact]
    public async Task Real_CryptoKit_RSA_PSS_proof_is_verified_using_only_trusted_public_key()
    {
        using var fixture = new CryptoFixture();
        await fixture.TrustAdminAsync();
        var challenge = fixture.Challenge();
        var signed = await fixture.Credentials.CreateChallengeResponseAsync(CryptoFixture.Admin, challenge);
        var result = await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), challenge, signed);
        Assert.True(result.Succeeded, result.FailureCode);
        Assert.Equal(CryptoFixture.Admin, result.Identity);
        Assert.Equal("admin-credential-1", result.CredentialId);
        var publicOnly = await fixture.Trust.TryGetAsync(CryptoFixture.Admin, signed.CredentialId);
        Assert.NotNull(publicOnly);
        using var publicRsa = RSA.Create();
        publicRsa.ImportSubjectPublicKeyInfo(publicOnly.PublicKeySubjectPublicKeyInfo, out _);
        Assert.ThrowsAny<CryptographicException>(() => publicRsa.ExportParameters(includePrivateParameters: true));
    }

    [Fact]
    public async Task Signature_and_all_signed_context_fields_cannot_be_substituted()
    {
        using var fixture = new CryptoFixture();
        await fixture.TrustAdminAsync();
        var challenge = fixture.Challenge();
        var signed = await fixture.Credentials.CreateChallengeResponseAsync(CryptoFixture.Admin, challenge);
        var corrupted = signed with { Proof = signed.Proof.ToArray() };
        corrupted.Proof[0] ^= 0x01;
        Assert.Equal("proof_invalid", (await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), challenge, corrupted)).FailureCode);
        Assert.Equal("challenge_id", (await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), challenge,
            signed with { ChallengeId = "other" })).FailureCode);
        Assert.Equal("identity_mismatch", (await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), challenge,
            signed with { Responder = ManagementPeerIdentity.ForAdminClient("imposter") })).FailureCode);
        Assert.Equal("credential_untrusted", (await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), challenge,
            signed with { CredentialId = "unknown" })).FailureCode);
        Assert.Equal("transport_mismatch", (await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(transport: "unix"), challenge, signed)).FailureCode);
        Assert.Equal("purpose_mismatch", (await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(purpose: "registration"), challenge, signed)).FailureCode);
        Assert.Equal("proof_invalid", (await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), challenge with
            { Issuer = ManagementPeerIdentity.ForManagementHost("different-host") }, signed)).FailureCode);
        Assert.Equal("proof_invalid", (await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), challenge with
            { Nonce = new byte[32] }, signed)).FailureCode);
    }

    [Fact]
    public async Task Unknown_or_forged_credentials_are_not_implicitly_trusted()
    {
        using var fixture = new CryptoFixture();
        var challenge = fixture.Challenge();
        var signed = await fixture.Credentials.CreateChallengeResponseAsync(CryptoFixture.Admin, challenge);
        Assert.Equal("credential_untrusted", (await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), challenge, signed)).FailureCode);
        await fixture.TrustAdminAsync();
        using var attacker = new CryptoFixture();
        var attackerProof = await attacker.Credentials.CreateChallengeResponseAsync(CryptoFixture.Admin, challenge);
        Assert.Equal("proof_invalid", (await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), challenge, attackerProof)).FailureCode);
        var attackerPublic = await attacker.Public.GetActiveAsync(CryptoFixture.Admin);
        await Assert.ThrowsAsync<CryptographicException>(async () =>
            await fixture.Trust.TrustAsync(CryptoFixture.Admin, attackerPublic.CredentialId, attackerPublic.PublicKeySubjectPublicKeyInfo));
    }

    [Fact]
    public async Task Deadline_clock_skew_and_future_response_are_validated()
    {
        using var fixture = new CryptoFixture();
        await fixture.TrustAdminAsync();
        var challenge = fixture.Challenge();
        var signed = await fixture.Credentials.CreateChallengeResponseAsync(CryptoFixture.Admin, challenge);
        Assert.Equal("response_time", (await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), challenge,
            signed with { CreatedAtUtc = fixture.Clock.GetUtcNow().AddSeconds(7) })).FailureCode);
        fixture.Clock.Advance(TimeSpan.FromSeconds(66));
        Assert.Equal("challenge_expired", (await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), challenge, signed)).FailureCode);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fixture.Credentials.CreateChallengeResponseAsync(CryptoFixture.Admin, challenge));
    }

    [Fact]
    public async Task A_credential_cannot_sign_challenge_for_another_identity()
    {
        using var fixture = new CryptoFixture();
        var challenge = fixture.Challenge(subject: ManagementPeerIdentity.ForAdminClient("other"));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fixture.Credentials.CreateChallengeResponseAsync(CryptoFixture.Admin, challenge));
    }

    [Fact]
    public async Task Missing_transport_purpose_is_rejected()
    {
        using var fixture = new CryptoFixture();
        await fixture.TrustAdminAsync();
        var challenge = fixture.Challenge();
        var proof = await fixture.Credentials.CreateChallengeResponseAsync(CryptoFixture.Admin, challenge);
        var peer = new ApiKit.Management.Models.ManagementPeerContext { Transport = "named-pipe" };
        Assert.Equal("purpose_mismatch", (await fixture.Verifier.VerifyAsync(peer, challenge, proof)).FailureCode);
    }
}
