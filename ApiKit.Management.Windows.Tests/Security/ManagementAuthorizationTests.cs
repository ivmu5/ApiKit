using System.Security.Claims;
using ApiKit.Management;
using ApiKit.Management.Models;
using ApiKit.Management.Security;
using ApiKit.Management.Windows;
using ApiKit.Management.Windows.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ApiKit.Management.Windows.Tests.Security;

public sealed class ManagementAuthorizationTests
{
    [Theory]
    [InlineData("administration", ManagementPeerKind.AdminClient,
        ApiKitManagementAuthorizationDefaults.PolicyName)]
    [InlineData("administration", ManagementPeerKind.AdminClient,
        ApiKitManagementAuthorizationDefaults.LifecyclePolicyName)]
    [InlineData("administration", ManagementPeerKind.AdminClient,
        ApiKitManagementAuthorizationDefaults.PackageManagementPolicyName)]
    [InlineData("registration", ManagementPeerKind.ManagedService,
        ApiKitManagementAuthorizationDefaults.ServiceRegistrationPolicyName)]
    [InlineData("management", ManagementPeerKind.ManagementHost,
        ApiKitManagementAuthorizationDefaults.ManagementHostPolicyName)]
    public async Task Verified_identity_receives_only_its_real_aspnet_policy(
        string purpose, ManagementPeerKind kind, string expectedPolicy)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiKitWindowsManagementHost(options => options.UseProvisionedServiceSidAcl = false,
            options => options.EnableServiceSidIsolation = false);
        services.AddApiKitManagement().AddWindowsNamedPipeTransport(
            options => options.UseProvisionedWindowsIsolation = false);
        using var provider = services.BuildServiceProvider();
        var authenticator = new WindowsNamedPipePeerAuthenticator();
        var identity = new ManagementPeerIdentity("test", kind)
        {
            InstanceId = kind == ManagementPeerKind.ManagedService ? "running-instance" : null
        };
        var peer = new ManagementPeerContext
        {
            Transport = WindowsManagementTransportDefaults.TransportName,
            VerifiedIdentity = identity,
            VerifiedCredentialId = "verified-credential",
            Properties = new Dictionary<string, string>
            {
                ["purpose"] = purpose,
                ["transportAccessGranted"] = "True"
            }
        };
        var principal = await authenticator.AuthenticateAsync(peer);
        Assert.NotNull(principal);
        var policies = new[]
        {
            ApiKitManagementAuthorizationDefaults.PolicyName,
            ApiKitManagementAuthorizationDefaults.LifecyclePolicyName,
            ApiKitManagementAuthorizationDefaults.PackageManagementPolicyName,
            ApiKitManagementAuthorizationDefaults.ServiceRegistrationPolicyName,
            ApiKitManagementAuthorizationDefaults.ManagementHostPolicyName
        };
        var auth = provider.GetRequiredService<IAuthorizationService>();
        foreach (var policy in policies)
        {
            var expected = kind switch
            {
                ManagementPeerKind.AdminClient => policy is ApiKitManagementAuthorizationDefaults.PolicyName or
                    ApiKitManagementAuthorizationDefaults.LifecyclePolicyName or
                    ApiKitManagementAuthorizationDefaults.PackageManagementPolicyName,
                _ => policy == expectedPolicy
            };
            Assert.Equal(expected, (await auth.AuthorizeAsync(principal, policy)).Succeeded);
        }
    }

    [Fact]
    public async Task ACL_alone_or_unverified_claims_cannot_get_management_permissions()
    {
        var authenticator = new WindowsNamedPipePeerAuthenticator();
        var peer = new ManagementPeerContext
        {
            Transport = WindowsManagementTransportDefaults.TransportName,
            OperatingSystemIdentity = "some-windows-user",
            Properties = new Dictionary<string, string>
            {
                ["purpose"] = WindowsManagementTransportDefaults.AdministrationPurpose,
                ["transportAccessGranted"] = "True"
            }
        };
        Assert.Null(await authenticator.AuthenticateAsync(peer));
        Assert.Null(await authenticator.AuthenticateAsync(peer with
        {
            VerifiedIdentity = ManagementPeerIdentity.ForManagementHost("wrong-role"),
            VerifiedCredentialId = "credential"
        }));
        Assert.Null(await authenticator.AuthenticateAsync(peer with
        {
            VerifiedIdentity = ManagementPeerIdentity.ForAdminClient("test"),
            VerifiedCredentialId = "credential",
            Properties = new Dictionary<string, string> { ["purpose"] = "administration" }
        }));
    }
}
