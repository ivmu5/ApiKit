using System.Security.Claims;
using ApiKit.Authorization;
using ApiKit.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ApiKit.Tests.Security;

/// <summary>
/// Tests the real ApiKit permission provider and ASP.NET Core authorization policies.
/// </summary>
public sealed class AuthorizationPolicyTests
{
    private sealed class Widget { }

    [Fact]
    public async Task Resource_permissions_require_an_authenticated_principal_with_the_exact_claim()
    {
        using var provider = CreateProvider();
        var policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var resolver = provider.GetRequiredService<IPermissionNameResolver>();
        Assert.Equal("widgets.read", resolver.GetPermission<Widget>(PermissionOperation.Read));
        var policyName = PermissionPolicyName.ForResource(typeof(Widget), PermissionOperation.Read);
        Assert.NotNull(await policies.GetPolicyAsync(policyName));

        Assert.True((await authorization.AuthorizeAsync(Principal("widgets.read"), policyName)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal("widgets.create"), policyName)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal(), policyName)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal("widgets.read", authenticated: false), policyName)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal("widgets.read", claimType: "permission"), policyName)).Succeeded);
    }

    [Fact]
    public async Task Literal_custom_permissions_and_default_aspnet_policies_are_separate()
    {
        using var provider = CreateProvider();
        var policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var resolver = provider.GetRequiredService<IPermissionNameResolver>();
        Assert.Equal("widgets.inspect", resolver.GetPermission<Widget>(PermissionOperation.Update));

        var customResource = PermissionPolicyName.ForResource(typeof(Widget), PermissionOperation.Update);
        var literal = PermissionPolicyName.ForPermission("widgets.inspect");
        foreach (var policy in new[] { customResource, literal })
        {
            Assert.True((await authorization.AuthorizeAsync(Principal("widgets.inspect"), policy)).Succeeded);
            Assert.False((await authorization.AuthorizeAsync(Principal("widgets.update"), policy)).Succeeded);
        }

        Assert.NotNull(await policies.GetPolicyAsync("ordinary"));
        Assert.True((await authorization.AuthorizeAsync(Principal("", extra: new Claim("ordinary", "yes")), "ordinary")).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal("widgets.inspect"), "ordinary")).Succeeded);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiKitAuthorization(options =>
        {
            options.PermissionClaimType = "capability";
            options.MapResource<Widget>("widgets");
            options.MapPermission<Widget>(PermissionOperation.Update, "widgets.inspect");
        });
        services.AddAuthorization(options => options.AddPolicy("ordinary", builder =>
            builder.RequireAuthenticatedUser().RequireClaim("ordinary", "yes")));
        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal Principal(string? permission = null, bool authenticated = true,
        string claimType = "capability", Claim? extra = null)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(permission)) claims.Add(new Claim(claimType, permission));
        if (extra is not null) claims.Add(extra);
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticated ? "test" : null));
    }
}
