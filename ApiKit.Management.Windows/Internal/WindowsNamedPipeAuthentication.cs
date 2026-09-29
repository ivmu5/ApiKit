using System.IO.Pipes;
using System.Security.Claims;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;

namespace ApiKit.Management.Windows.Internal;

internal static class WindowsNamedPipeAuthentication
{
    public static async ValueTask<ClaimsPrincipal?> AuthenticateAsync(
        NamedPipeServerStream pipe,
        string pipeName,
        string purpose,
        IManagementPeerAuthenticator authenticator,
        CancellationToken cancellationToken)
    {
        var peer = CreatePeerContext(pipe, pipeName, purpose);
        return await AuthenticateAsync(peer, authenticator, cancellationToken);
    }

    public static ValueTask<ClaimsPrincipal?> AuthenticateAsync(
        ManagementPeerContext peer,
        IManagementPeerAuthenticator authenticator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(peer);
        ArgumentNullException.ThrowIfNull(authenticator);

        return authenticator.AuthenticateAsync(peer, cancellationToken);
    }

    public static ManagementPeerContext CreatePeerContext(
        NamedPipeServerStream pipe,
        string pipeName,
        string purpose)
    {
        ArgumentNullException.ThrowIfNull(pipe);
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        string? operatingSystemIdentity = null;

        try
        {
            // GetImpersonationUserName доступен только когда клиент разрешил impersonation
            // и уже записал данные в pipe. Поэтому identity является дополнительной metadata,
            // а безопасный Windows boundary прежде всего задаётся ACL самого pipe.
            operatingSystemIdentity = pipe.GetImpersonationUserName();
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (InvalidOperationException)
        {
        }

        return new ManagementPeerContext
        {
            Transport = WindowsManagementTransportDefaults.TransportName,
            OperatingSystemIdentity = operatingSystemIdentity,
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pipe"] = pipeName,
                ["purpose"] = purpose,
                ["transportAccessGranted"] = bool.TrueString
            }
        };
    }
    public static ManagementPeerContext CreateClientPeerContext(
        string pipeName,
        string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        return new ManagementPeerContext
        {
            Transport = WindowsManagementTransportDefaults.TransportName,
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pipe"] = pipeName,
                ["purpose"] = purpose,
                ["transportAccessGranted"] = bool.TrueString
            }
        };
    }

}
