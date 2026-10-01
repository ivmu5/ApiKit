using ApiKit.Management.Abstractions;
using ApiKit.Management.Builders;
using ApiKit.Management.Models;
using ApiKit.Management.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Internal;

internal sealed class AspNetCoreEndpointDescriptorContributor(
    IEnumerable<EndpointDataSource> endpointDataSources,
    IOptions<ApiKitManagementOptions> options)
    : IManagementDescriptorContributor
{
    public ValueTask ContributeAsync(
        ManagementServiceDescriptorBuilder builder,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.DiscoverEndpoints)
        {
            return ValueTask.CompletedTask;
        }

        var count = 0;

        foreach (var endpoint in endpointDataSources.SelectMany(source => source.Endpoints))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (endpoint is not RouteEndpoint routeEndpoint)
            {
                continue;
            }

            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();

            if (methodMetadata is null || methodMetadata.HttpMethods.Count == 0)
            {
                continue;
            }

            var controller = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            var endpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
            var route = routeEndpoint.RoutePattern.RawText ?? routeEndpoint.RoutePattern.ToString();
            var name = endpointName
                ?? (controller is null
                    ? endpoint.DisplayName ?? route
                    : $"{controller.ControllerName}.{controller.ActionName}");

            var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            var policies = authorizeData
                .Select(data => data.Policy)
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value)
                .Cast<string>()
                .ToArray();

            var roles = authorizeData
                .SelectMany(data => (data.Roles ?? string.Empty)
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value)
                .ToArray();

            var authenticationSchemes = authorizeData
                .SelectMany(data => (data.AuthenticationSchemes ?? string.Empty)
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value)
                .ToArray();

            builder.AddEndpoint(new ManagementEndpointDescriptor
            {
                Name = name,
                Route = route,
                HttpMethods = methodMetadata.HttpMethods
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static method => method)
                    .ToArray(),
                GroupName = ResolveGroupName(endpoint, controller),
                ControllerName = controller?.ControllerName,
                ActionName = controller?.ActionName,
                AllowsAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null,
                AuthorizationPolicies = policies,
                AuthorizationRoles = roles,
                AuthenticationSchemes = authenticationSchemes
            });

            count++;
        }

        if (count > 0)
        {
            builder.AddCapability(ManagementCapabilities.Endpoints);
        }

        return ValueTask.CompletedTask;
    }

    private static string? ResolveGroupName(
        Endpoint endpoint,
        ControllerActionDescriptor? controller)
    {
        var endpointGroup = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName;

        if (!string.IsNullOrWhiteSpace(endpointGroup))
        {
            return endpointGroup;
        }

        if (controller is null)
        {
            return null;
        }

        var actionSettings = controller.MethodInfo
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), inherit: true)
            .OfType<ApiExplorerSettingsAttribute>()
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(actionSettings?.GroupName))
        {
            return actionSettings.GroupName;
        }

        var controllerSettings = controller.ControllerTypeInfo
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), inherit: true)
            .OfType<ApiExplorerSettingsAttribute>()
            .FirstOrDefault();

        return !string.IsNullOrWhiteSpace(controllerSettings?.GroupName)
            ? controllerSettings.GroupName
            : controller.ControllerName;
    }
}
