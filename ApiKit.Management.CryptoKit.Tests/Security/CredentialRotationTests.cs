using System.Security.Cryptography;
using ApiKit.Management.CryptoKit.Models;
using ApiKit.Management.CryptoKit.Tests.Support;
using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Tests.Security;

public sealed class CredentialRotationTests
{
    [Fact]
    public async Task Durable_rotation_preserves_old_key_during_overlap_and_rejects_it_after_expiration()
    {
        var storage = new InMemoryKeyStorage();
        using (var fixture = new CryptoFixture(storage))
        {
            await fixture.TrustAdminAsync();
            var oldChallenge = fixture.Challenge();
            var oldProof = await fixture.Credentials.CreateChallengeResponseAsync(CryptoFixture.Admin, oldChallenge);
            var replacement = await fixture.Rotation.PrepareAsync(
                CryptoFixture.Admin, "admin-key-2", "admin-credential-2");
            Assert.Equal(CryptoKitManagementLocalCredentialStatus.Available, replacement.Status);
            Assert.Equal("credential_untrusted", (await fixture.Verifier.VerifyAsync(
                CryptoFixture.Peer(), oldChallenge, oldProof with { CredentialId = replacement.CredentialId })).FailureCode);

            await fixture.Trust.TrustAsync(CryptoFixture.Admin, replacement.CredentialId,
                replacement.PublicKeySubjectPublicKeyInfo);
            await fixture.Trust.MarkRetiringAsync(CryptoFixture.Admin, oldProof.CredentialId,
                fixture.Clock.GetUtcNow().AddSeconds(5));
            await fixture.Rotation.ActivateAsync(CryptoFixture.Admin, replacement.CredentialId);
            var newChallenge = fixture.Challenge();
            var newProof = await fixture.Credentials.CreateChallengeResponseAsync(CryptoFixture.Admin, newChallenge);
            Assert.Equal(replacement.CredentialId, newProof.CredentialId);
            Assert.True((await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), newChallenge, newProof)).Succeeded);
            Assert.True((await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), oldChallenge, oldProof)).Succeeded);

            fixture.Clock.Advance(TimeSpan.FromSeconds(11)); // Five-second overlap plus five seconds of permitted clock skew.
            Assert.Equal("credential_retired", (await fixture.Verifier.VerifyAsync(
                CryptoFixture.Peer(), oldChallenge, oldProof)).FailureCode);
            Assert.True((await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), newChallenge, newProof)).Succeeded);
            await fixture.Rotation.RetireAsync(CryptoFixture.Admin, oldProof.CredentialId);
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await fixture.Rotation.ActivateAsync(CryptoFixture.Admin, oldProof.CredentialId));
        }

        using (var restarted = new CryptoFixture(storage))
        {
            Assert.Equal("admin-credential-2",
                await restarted.Credentials.GetCredentialIdAsync(CryptoFixture.Admin));
            var active = await restarted.Rotation.GetCredentialsAsync(CryptoFixture.Admin);
            Assert.Single(active, item => item.Status == CryptoKitManagementLocalCredentialStatus.Active);
            Assert.Single(active, item => item.Status == CryptoKitManagementLocalCredentialStatus.Retired);
        }
    }

    [Fact]
    public async Task Revocation_immediately_revokes_access_and_retrust_can_roll_back_retirement()
    {
        using var fixture = new CryptoFixture();
        await fixture.TrustAdminAsync();
        var publicCredential = await fixture.Public.GetActiveAsync(CryptoFixture.Admin);
        var challenge = fixture.Challenge();
        var proof = await fixture.Credentials.CreateChallengeResponseAsync(CryptoFixture.Admin, challenge);
        await fixture.Trust.MarkRetiringAsync(CryptoFixture.Admin, publicCredential.CredentialId,
            fixture.Clock.GetUtcNow().AddSeconds(6));
        await fixture.Trust.TrustAsync(CryptoFixture.Admin, publicCredential.CredentialId,
            publicCredential.PublicKeySubjectPublicKeyInfo);
        var trusted = await fixture.Trust.TryGetAsync(CryptoFixture.Admin, publicCredential.CredentialId);
        Assert.NotNull(trusted);
        Assert.Equal(CryptoKitManagementTrustedCredentialStatus.Trusted, trusted.Status);
        Assert.Null(trusted.AcceptUntilUtc);
        fixture.Clock.Advance(TimeSpan.FromSeconds(12));
        Assert.True((await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), challenge, proof)).Succeeded);
        await fixture.Trust.RevokeAsync(CryptoFixture.Admin, publicCredential.CredentialId);
        await fixture.Trust.RevokeAsync(CryptoFixture.Admin, publicCredential.CredentialId);
        Assert.Null(await fixture.Trust.TryGetAsync(CryptoFixture.Admin, publicCredential.CredentialId));
        Assert.Equal("credential_untrusted", (await fixture.Verifier.VerifyAsync(CryptoFixture.Peer(), challenge, proof)).FailureCode);
    }

    [Fact]
    public async Task Key_identifiers_cannot_be_reused_across_credentials_or_identities()
    {
        using var fixture = new CryptoFixture();
        await fixture.Public.GetActiveAsync(CryptoFixture.Admin);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fixture.Rotation.PrepareAsync(
                ManagementPeerIdentity.ForAdminClient("other"), "admin-key-1", "credential-foreign"));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fixture.Rotation.PrepareAsync(
                CryptoFixture.Admin, "admin-key-1", "credential-2"));
    }

    [Fact]
    public async Task Trust_store_rejects_small_rsa_keys_and_unexpected_private_key_format()
    {
        using var fixture = new CryptoFixture();
        using var weak = RSA.Create(1024);
        await Assert.ThrowsAsync<CryptographicException>(async () =>
            await fixture.Trust.TrustAsync(CryptoFixture.Admin, "weak", weak.ExportSubjectPublicKeyInfo()));
        using var valid = RSA.Create(2048);
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await fixture.Trust.TrustAsync(CryptoFixture.Admin, "private-instead-of-public", valid.ExportPkcs8PrivateKey()));
    }

    [Fact]
    public async Task Retiring_window_must_be_future_and_retiring_key_must_exist()
    {
        using var fixture = new CryptoFixture();
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await fixture.Trust.MarkRetiringAsync(CryptoFixture.Admin, "missing", fixture.Clock.GetUtcNow().AddMinutes(1)));
        await fixture.TrustAdminAsync();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await fixture.Trust.MarkRetiringAsync(CryptoFixture.Admin, "admin-credential-1", fixture.Clock.GetUtcNow()));
    }
}
