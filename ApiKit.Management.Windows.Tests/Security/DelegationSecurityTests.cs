using ApiKit.Management.Security;
using ApiKit.Management.Windows.Internal;

namespace ApiKit.Management.Windows.Tests.Security;

/// <summary>
/// Ensures that missing administrative delegation cannot be replaced by Host credentials.
/// </summary>
public sealed class DelegationSecurityTests
{
    [Fact]
    public void Missing_delegation_fails_closed()
    {
        Assert.Null(WindowsNamedPipeServiceServer.CreateDelegatedPrincipal(null));
        Assert.Null(WindowsNamedPipeServiceServer.CreateDelegatedPrincipal([]));
        Assert.Null(WindowsNamedPipeServiceServer.CreateDelegatedPrincipal(
            [new SerializedClaim("ordinary", "value", "string", "LOCAL")]));
        Assert.Null(WindowsNamedPipeServiceServer.CreateDelegatedPrincipal(
            [new SerializedClaim(
                ApiKitManagementAuthorizationDefaults.ClaimType,
                ApiKitManagementAuthorizationDefaults.AdministratorClaimValue,
                System.Security.Claims.ClaimValueTypes.String,
                "LOCAL"), null!]));
    }

    [Fact]
    public void Explicit_administrative_delegation_remains_available()
    {
        var principal = WindowsNamedPipeServiceServer.CreateDelegatedPrincipal(
            [new SerializedClaim(
                ApiKitManagementAuthorizationDefaults.ClaimType,
                ApiKitManagementAuthorizationDefaults.AdministratorClaimValue,
                System.Security.Claims.ClaimValueTypes.String,
                "LOCAL")]);
        Assert.NotNull(principal);
        Assert.True(principal.Identity?.IsAuthenticated == true);
        Assert.True(principal.HasClaim(
            ApiKitManagementAuthorizationDefaults.ClaimType,
            ApiKitManagementAuthorizationDefaults.AdministratorClaimValue));
    }
}
