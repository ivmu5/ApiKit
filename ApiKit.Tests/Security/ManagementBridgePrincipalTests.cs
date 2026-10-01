using System.Security.Claims;
using ApiKit.Management.Internal;
using ApiKit.Management.Models;
using ApiKit.Management.Security;
using ApiKit.Authorization.Permissions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiKit.Tests.Security;

/// <summary>
/// Verifies that the in-process management bridge does not elevate arbitrary principals.
/// </summary>
public sealed class ManagementBridgePrincipalTests
{
    [Fact]
    public void Non_administrative_principal_does_not_gain_synthetic_crud_permission()
    {
        var caller = Principal(new Claim("ordinary", "true"));
        var resourcePrincipal = ManagementResourceDispatcher.CreateResourcePrincipal(
            caller, Resource(), ManagementResourceOperation.Delete);
        Assert.False(resourcePrincipal.HasClaim(PermissionAuthorizationDefaults.ClaimType, "widgets.delete"));
    }

    [Fact]
    public void Authenticated_administrator_receives_only_the_requested_crud_permission()
    {
        var caller = Principal(new Claim(
            ApiKitManagementAuthorizationDefaults.ClaimType,
            ApiKitManagementAuthorizationDefaults.AdministratorClaimValue));
        var resourcePrincipal = ManagementResourceDispatcher.CreateResourcePrincipal(
            caller, Resource(), ManagementResourceOperation.Delete);
        Assert.True(resourcePrincipal.HasClaim(PermissionAuthorizationDefaults.ClaimType, "widgets.delete"));
        Assert.False(resourcePrincipal.HasClaim(PermissionAuthorizationDefaults.ClaimType, "widgets.create"));
    }

    [Fact]
    public async Task Anonymous_principal_is_rejected_before_dispatching_an_action()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var dispatcher = new ManagementResourceDispatcher(
            null!, services, NullLogger<ManagementResourceDispatcher>.Instance);
        var result = await dispatcher.ExecuteAsync(
            "widgets", ManagementResourceOperation.Delete, new ClaimsPrincipal());
        Assert.False(result.Succeeded);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal("unauthorized", result.ErrorCode);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));

    private static CrudManagementResourceDefinition Resource() => new(
        new ManagementResourceDescriptor
        {
            Name = "widgets",
            DisplayName = "Widgets",
            ControllerName = "Widgets",
            EntityTypeName = "Widget",
            KeyTypeName = "string",
            ReadModel = new ManagementModelDescriptor { TypeName = "Widget", Properties = [] },
            CreateModel = new ManagementModelDescriptor { TypeName = "WidgetCreate", Properties = [] },
            UpdateModel = new ManagementModelDescriptor { TypeName = "WidgetUpdate", Properties = [] },
            Operations = [ManagementResourceOperation.Delete, ManagementResourceOperation.Create],
            Permissions = new Dictionary<ManagementResourceOperation, string>
            {
                [ManagementResourceOperation.Delete] = "widgets.delete",
                [ManagementResourceOperation.Create] = "widgets.create"
            }
        },
        new Dictionary<ManagementResourceOperation, Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>());
}
