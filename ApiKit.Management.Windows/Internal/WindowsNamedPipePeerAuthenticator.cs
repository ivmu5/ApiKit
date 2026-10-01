using System.Security.Claims;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;
using ApiKit.Management.Security;

namespace ApiKit.Management.Windows.Internal;

/// <summary>
/// Turns a cryptographically verified transport identity into an ASP.NET Core principal with purpose-scoped privileges.
/// </summary>
internal sealed class WindowsNamedPipePeerAuthenticator : IManagementPeerAuthenticator
{
    public ValueTask<ClaimsPrincipal?> AuthenticateAsync(
        ManagementPeerContext peer,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!peer.Properties.TryGetValue("transportAccessGranted", out var accessGranted)
            || !bool.TryParse(accessGranted, out var granted)
            || !granted
            || !peer.Properties.TryGetValue("purpose", out var purpose))
        {
            return ValueTask.FromResult<ClaimsPrincipal?>(null);
        }

        if (purpose == WindowsManagementTransportDefaults.RegistrationPurpose &&
            (peer.VerifiedIdentity is null ||
             peer.VerifiedIdentity.Kind != ManagementPeerKind.ManagedService ||
             string.IsNullOrWhiteSpace(peer.VerifiedIdentity.PeerId) ||
             string.IsNullOrWhiteSpace(peer.VerifiedIdentity.InstanceId) ||
             string.IsNullOrWhiteSpace(peer.VerifiedCredentialId)))
        {
            return ValueTask.FromResult<ClaimsPrincipal?>(null);
        }

        if (purpose == WindowsManagementTransportDefaults.AdministrationPurpose &&
            (peer.VerifiedIdentity is null ||
             peer.VerifiedIdentity.Kind != ManagementPeerKind.AdminClient ||
             string.IsNullOrWhiteSpace(peer.VerifiedIdentity.PeerId) ||
             !string.IsNullOrWhiteSpace(peer.VerifiedIdentity.InstanceId) ||
             string.IsNullOrWhiteSpace(peer.VerifiedCredentialId)))
        {
            return ValueTask.FromResult<ClaimsPrincipal?>(null);
        }

        if (purpose == WindowsManagementTransportDefaults.ManagementPurpose &&
            (peer.VerifiedIdentity is null ||
             peer.VerifiedIdentity.Kind != ManagementPeerKind.ManagementHost ||
             string.IsNullOrWhiteSpace(peer.VerifiedIdentity.PeerId) ||
             string.IsNullOrWhiteSpace(peer.VerifiedCredentialId)))
        {
            return ValueTask.FromResult<ClaimsPrincipal?>(null);
        }

        var identity = new ClaimsIdentity(
            authenticationType: WindowsManagementTransportDefaults.TransportName,
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);

        if (!string.IsNullOrWhiteSpace(peer.OperatingSystemIdentity))
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, peer.OperatingSystemIdentity));
        }

        if (peer.VerifiedIdentity is not null)
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, peer.VerifiedIdentity.PeerId));
        }

        switch (purpose)
        {
            case WindowsManagementTransportDefaults.AdministrationPurpose:
                AddManagementClaim(identity, ApiKitManagementAuthorizationDefaults.AdministratorClaimValue);
                AddManagementClaim(identity, ApiKitManagementAuthorizationDefaults.LifecycleClaimValue);
                AddManagementClaim(identity, ApiKitManagementAuthorizationDefaults.PackageManagementClaimValue);
                break;

            case WindowsManagementTransportDefaults.RegistrationPurpose:
                AddManagementClaim(identity, ApiKitManagementAuthorizationDefaults.ServiceRegistrationClaimValue);
                break;

            case WindowsManagementTransportDefaults.ManagementPurpose:
                AddManagementClaim(identity, ApiKitManagementAuthorizationDefaults.ManagementHostClaimValue);
                break;

            default:
                return ValueTask.FromResult<ClaimsPrincipal?>(null);
        }

        return ValueTask.FromResult<ClaimsPrincipal?>(new ClaimsPrincipal(identity));
    }

    private static void AddManagementClaim(ClaimsIdentity identity, string value) =>
        identity.AddClaim(new Claim(ApiKitManagementAuthorizationDefaults.ClaimType, value));
}
