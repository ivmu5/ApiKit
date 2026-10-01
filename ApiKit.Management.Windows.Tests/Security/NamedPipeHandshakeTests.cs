using System.Buffers.Binary;
using System.IO.Pipes;
using System.Security.Claims;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;
using ApiKit.Management.Security;
using ApiKit.Management.Windows;
using ApiKit.Management.Windows.Internal;
using ApiKit.Management.Windows.Options;
using ApiKit.Management.Windows.Tests.Support;

namespace ApiKit.Management.Windows.Tests.Security;

public sealed class NamedPipeHandshakeTests
{
    private const int MaxBytes = 64 * 1024;
    private static readonly ManagementPeerIdentity Host = ManagementPeerIdentity.ForManagementHost("host");
    private static readonly ManagementPeerIdentity Admin = ManagementPeerIdentity.ForAdminClient("admin");

    [WindowsFact]
    public async Task Silent_peer_cannot_hold_a_handshake_open_indefinitely()
    {
        using var host = new WindowsPeerFixture(Host);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pipeName = "apikit-stalled-" + Guid.NewGuid().ToString("N");
        await using var serverPipe = WindowsNamedPipeSecurityFactory.CreateServer(
            pipeName, new WindowsNamedPipeAccessOptions());
        var server = Task.Run(async () =>
        {
            await serverPipe.WaitForConnectionAsync(timeout.Token);
            await WindowsNamedPipeChallengeResponseProtocol.VerifyIdentityAsync(
                serverPipe,
                Host,
                Peer(WindowsManagementTransportDefaults.AdministrationPurpose),
                host.Challenges,
                host.Verifier,
                WindowsManagementTransportDefaults.AdministrationPurpose,
                MaxBytes,
                timeout.Token,
                maximumHandshakeDuration: TimeSpan.FromMilliseconds(200));
        });
        await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(timeout.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => server);
    }

    [WindowsFact]
    public async Task Real_named_pipe_handshake_authenticates_both_sides_and_binds_to_same_connection()
    {
        using var host = new WindowsPeerFixture(Host);
        using var admin = new WindowsPeerFixture(Admin);
        await host.TrustPeerAsync(admin);
        await admin.TrustPeerAsync(host);
        var (hostResult, adminResult) = await RunHandshakeAsync(host, admin);
        Assert.True(hostResult.Succeeded, hostResult.FailureCode);
        Assert.True(adminResult.Succeeded, adminResult.FailureCode);
        Assert.Equal(Admin, hostResult.Identity);
        Assert.Equal(Host, adminResult.Identity);
    }

    [WindowsFact]
    public async Task Forged_admin_private_key_is_rejected_even_if_claimed_identity_matches()
    {
        using var host = new WindowsPeerFixture(Host);
        using var knownAdmin = new WindowsPeerFixture(Admin);
        using var attacker = new WindowsPeerFixture(Admin); // Separate key storage, but the same claimed identity.
        await host.TrustPeerAsync(knownAdmin);
        await attacker.TrustPeerAsync(host);
        var (hostResult, adminResult) = await RunHandshakeAsync(host, attacker);
        Assert.False(hostResult.Succeeded);
        Assert.Equal("proof_invalid", hostResult.FailureCode);
        Assert.False(adminResult.Succeeded);
    }

    [WindowsFact]
    public async Task Admin_must_verify_host_before_sending_its_own_proof()
    {
        using var unknownHost = new WindowsPeerFixture(Host);
        using var admin = new WindowsPeerFixture(Admin);
        await unknownHost.TrustPeerAsync(admin);
        var (hostResult, adminResult) = await RunHandshakeAsync(unknownHost, admin);
        Assert.False(hostResult.Succeeded);
        Assert.False(adminResult.Succeeded);
        Assert.Equal("credential_untrusted", adminResult.FailureCode);
    }

    [WindowsFact]
    public async Task Incorrect_wire_protocol_is_rejected_before_issuing_challenge()
    {
        using var host = new WindowsPeerFixture(Host);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var name = "apikit-test-" + Guid.NewGuid().ToString("N");
        await using var pipe = WindowsNamedPipeSecurityFactory.CreateServer(name, new WindowsNamedPipeAccessOptions());
        var server = Task.Run(async () =>
        {
            await pipe.WaitForConnectionAsync(timeout.Token);
            return await WindowsNamedPipeChallengeResponseProtocol.VerifyIdentityAsync(
                pipe, Host, Peer(WindowsManagementTransportDefaults.AdministrationPurpose),
                host.Challenges, host.Verifier, WindowsManagementTransportDefaults.AdministrationPurpose,
                MaxBytes, timeout.Token);
        });
        await using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(timeout.Token);
        await WindowsNamedPipeMessageSerializer.WriteAsync(client,
            new WindowsNamedPipeSecurityHello { ProtocolVersion = 5, Identity = Admin }, MaxBytes, timeout.Token);
        var result = await WindowsNamedPipeMessageSerializer.ReadAsync<WindowsNamedPipeSecurityResult>(client, MaxBytes, timeout.Token);
        Assert.False(result.Succeeded);
        Assert.Equal("protocol_version", result.ErrorCode);
        Assert.False((await server).Succeeded);
    }

    [WindowsFact]
    public async Task Previously_signed_response_fails_against_a_new_wire_challenge()
    {
        using var host = new WindowsPeerFixture(Host);
        using var admin = new WindowsPeerFixture(Admin);
        await host.TrustPeerAsync(admin);

        async Task<(ManagementChallengeResponse Response, ManagementAuthenticationResult Result)> ExchangeAsync(
            ManagementChallengeResponse? replay = null)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var name = "apikit-replay-" + Guid.NewGuid().ToString("N");
            await using var serverPipe = WindowsNamedPipeSecurityFactory.CreateServer(name, new WindowsNamedPipeAccessOptions());
            var server = Task.Run(async () =>
            {
                await serverPipe.WaitForConnectionAsync(timeout.Token);
                return await WindowsNamedPipeChallengeResponseProtocol.VerifyIdentityAsync(
                    serverPipe, Host, Peer(WindowsManagementTransportDefaults.AdministrationPurpose),
                    host.Challenges, host.Verifier, WindowsManagementTransportDefaults.AdministrationPurpose,
                    MaxBytes, timeout.Token,
                    identity => ManagementPeerIdentitySecurityComparer.Equals(identity, Admin));
            });
            await using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
            await client.ConnectAsync(timeout.Token);
            await WindowsNamedPipeMessageSerializer.WriteAsync(client,
                new WindowsNamedPipeSecurityHello { Identity = Admin }, MaxBytes, timeout.Token);
            var frame = await WindowsNamedPipeMessageSerializer.ReadAsync<WindowsNamedPipeSecurityChallenge>(
                client, MaxBytes, timeout.Token);
            var signed = replay ?? await admin.Credentials.CreateChallengeResponseAsync(Admin, frame.Challenge);
            await WindowsNamedPipeMessageSerializer.WriteAsync(client,
                new WindowsNamedPipeSecurityResponse { Response = signed }, MaxBytes, timeout.Token);
            var result = await WindowsNamedPipeMessageSerializer.ReadAsync<WindowsNamedPipeSecurityResult>(
                client, MaxBytes, timeout.Token);
            var verified = await server;
            Assert.Equal(verified.Succeeded, result.Succeeded);
            Assert.Equal(verified.FailureCode, result.ErrorCode);
            return (signed, verified);
        }

        var initial = await ExchangeAsync();
        Assert.True(initial.Result.Succeeded, initial.Result.FailureCode);
        var replayed = await ExchangeAsync(initial.Response);
        Assert.False(replayed.Result.Succeeded);
        Assert.Equal("challenge_id", replayed.Result.FailureCode);
    }

    [Fact]
    public async Task Wire_serializer_rejects_invalid_and_oversized_frames_before_allocation()
    {
        var data = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(data, int.MaxValue);
        using var source = new MemoryStream(data);
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await WindowsNamedPipeMessageSerializer.ReadAsync<WindowsNamedPipeRequest>(source, 1024, default));
        using var output = new MemoryStream();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await WindowsNamedPipeMessageSerializer.WriteAsync(output,
                new WindowsNamedPipeRequest { Type = new string('x', 4096) }, 128, default));
    }

    private static async Task<(ManagementAuthenticationResult Host, ManagementAuthenticationResult Client)> RunHandshakeAsync(
        WindowsPeerFixture host, WindowsPeerFixture client)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var pipeName = "apikit-test-" + Guid.NewGuid().ToString("N");
        await using var serverPipe = WindowsNamedPipeSecurityFactory.CreateServer(pipeName, new WindowsNamedPipeAccessOptions());
        var serverTask = Task.Run(async () =>
        {
            await serverPipe.WaitForConnectionAsync(timeout.Token);
            var first = await WindowsNamedPipeChallengeResponseProtocol.ProveIdentityAsync(
                serverPipe, Host, host.Credentials,
                WindowsManagementTransportDefaults.AdministrationPurpose, MaxBytes, timeout.Token,
                challengeIssuerValidator: identity => ManagementPeerIdentitySecurityComparer.Equals(identity, Admin));
            if (!first.Succeeded) return first;
            return await WindowsNamedPipeChallengeResponseProtocol.VerifyIdentityAsync(
                serverPipe, Host, Peer(WindowsManagementTransportDefaults.AdministrationPurpose),
                host.Challenges, host.Verifier, WindowsManagementTransportDefaults.AdministrationPurpose,
                MaxBytes, timeout.Token,
                identity => ManagementPeerIdentitySecurityComparer.Equals(identity, Admin));
        });
        await using var clientPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await clientPipe.ConnectAsync(timeout.Token);
        var clientResult = await WindowsNamedPipeChallengeResponseProtocol.VerifyIdentityAsync(
            clientPipe, client.Identity, Peer(WindowsManagementTransportDefaults.AdministrationPurpose),
            client.Challenges, client.Verifier, WindowsManagementTransportDefaults.AdministrationPurpose,
            MaxBytes, timeout.Token,
            identity => ManagementPeerIdentitySecurityComparer.Equals(identity, Host));
        if (clientResult.Succeeded)
        {
            var final = await WindowsNamedPipeChallengeResponseProtocol.ProveIdentityAsync(
                clientPipe, client.Identity, client.Credentials,
                WindowsManagementTransportDefaults.AdministrationPurpose,
                MaxBytes, timeout.Token, expectedChallengeIssuer: Host);
            if (!final.Succeeded) clientResult = final;
        }
        return (await serverTask, clientResult);
    }

    private static ManagementPeerContext Peer(string purpose) => new()
    {
        Transport = WindowsManagementTransportDefaults.TransportName,
        Properties = new Dictionary<string, string> { ["purpose"] = purpose }
    };
}
