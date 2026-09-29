using System.Reflection;
using System.Text.Json;
using ApiKit.Authorization.Permissions;
using ApiKit.Crud.Attributes;
using ApiKit.Crud.Controllers;
using ApiKit.Management.Models;
using ApiKit.Management.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Internal;

/// <summary>
/// Единый внутренний каталог обнаруженных CRUD-ресурсов.
/// </summary>
/// <remarks>
/// Каталог используется и descriptor discovery, и management dispatcher, чтобы они
/// не реализовывали независимо друг от друга правила определения имён и actions.
/// </remarks>
internal sealed class CrudManagementResourceRegistry(
    IServiceProvider serviceProvider,
    IOptions<ApiKitManagementOptions> options,
    ManagementModelDescriptorFactory modelDescriptorFactory)
{
    private readonly object _sync = new();
    private int _cachedVersion = -1;
    private IReadOnlyList<CrudManagementResourceDefinition> _cached = [];

    public IReadOnlyList<CrudManagementResourceDefinition> GetResources()
    {
        if (!options.Value.DiscoverCrudResources)
        {
            return [];
        }

        var actionProvider = serviceProvider.GetService<IActionDescriptorCollectionProvider>();
        if (actionProvider is null)
        {
            return [];
        }

        var collection = actionProvider.ActionDescriptors;
        if (_cachedVersion == collection.Version)
        {
            return _cached;
        }

        lock (_sync)
        {
            collection = actionProvider.ActionDescriptors;
            if (_cachedVersion == collection.Version)
            {
                return _cached;
            }

            _cached = BuildResources(collection.Items.OfType<ControllerActionDescriptor>());
            _cachedVersion = collection.Version;
            return _cached;
        }
    }

    public CrudManagementResourceDefinition? Find(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        return GetResources().FirstOrDefault(resource =>
            string.Equals(
                resource.Descriptor.Name,
                resourceName,
                StringComparison.OrdinalIgnoreCase));
    }

    private IReadOnlyList<CrudManagementResourceDefinition> BuildResources(
        IEnumerable<ControllerActionDescriptor> actions)
    {
        var permissionResolver = serviceProvider.GetService<IPermissionNameResolver>();
        var resources = new List<CrudManagementResourceDefinition>();

        foreach (var controllerGroup in actions.GroupBy(action => action.ControllerTypeInfo.AsType()))
        {
            var genericArguments = TryGetCrudGenericArguments(controllerGroup.Key);
            if (genericArguments is null)
            {
                continue;
            }

            var operationActions = controllerGroup
                .Select(action => (Action: action, Operation: TryGetOperation(action)))
                .Where(static item => item.Operation is not null)
                .GroupBy(static item => item.Operation!.Value)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.First().Action);

            if (operationActions.Count == 0)
            {
                continue;
            }

            var firstAction = controllerGroup.First();
            var entityType = genericArguments[1];
            var resourceName = JsonNamingPolicy.KebabCaseLower.ConvertName(firstAction.ControllerName);
            var displayName = firstAction.ControllerName;
            var groupName = ResolveGroupName(firstAction);

            if (options.Value.TryGetResourceOptions(entityType, out var resourceOptions)
                && resourceOptions is not null)
            {
                if (!resourceOptions.Enabled)
                {
                    continue;
                }

                resourceName = string.IsNullOrWhiteSpace(resourceOptions.Name)
                    ? resourceName
                    : resourceOptions.Name.Trim();
                displayName = string.IsNullOrWhiteSpace(resourceOptions.DisplayName)
                    ? displayName
                    : resourceOptions.DisplayName.Trim();
                groupName = string.IsNullOrWhiteSpace(resourceOptions.GroupName)
                    ? groupName
                    : resourceOptions.GroupName.Trim();

                if (resourceOptions.AllowedOperations is not null)
                {
                    operationActions = operationActions
                        .Where(pair => resourceOptions.AllowedOperations.Contains(pair.Key))
                        .ToDictionary(static pair => pair.Key, static pair => pair.Value);
                }
            }

            if (operationActions.Count == 0)
            {
                continue;
            }

            var operations = operationActions.Keys.OrderBy(static operation => operation).ToArray();
            var permissions = permissionResolver is null
                ? new Dictionary<ManagementResourceOperation, string>()
                : CreatePermissionMap(permissionResolver, entityType, operations);

            var readModel = modelDescriptorFactory.Create(genericArguments[3]);
            var descriptor = new ManagementResourceDescriptor
            {
                Name = resourceName,
                DisplayName = displayName,
                GroupName = groupName,
                ControllerName = firstAction.ControllerName,
                EntityTypeName = GetTypeName(entityType),
                KeyTypeName = GetTypeName(genericArguments[2]),
                KeyJsonName = ResolveKeyJsonName(
                    entityType,
                    genericArguments[2],
                    readModel,
                    resourceOptions),
                ReadModel = readModel,
                CreateModel = modelDescriptorFactory.Create(genericArguments[4]),
                UpdateModel = modelDescriptorFactory.Create(genericArguments[5]),
                Operations = operations,
                Permissions = permissions
            };

            resources.Add(new CrudManagementResourceDefinition(
                descriptor,
                operationActions));
        }

        var duplicate = resources
            .GroupBy(
                static resource => resource.Descriptor.Name,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Management resource name '{duplicate.Key}' используется несколькими CRUD-контроллерами. " +
                "Задайте уникальные имена через ApiKitManagementOptions.MapResource<TEntity>().");
        }

        return resources
            .OrderBy(static resource => resource.Descriptor.GroupName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static resource => resource.Descriptor.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ManagementResourceOperation? TryGetOperation(ControllerActionDescriptor action)
    {
        // Management публикует только стандартные CRUD actions, помеченные внутренним
        // маркером CrudController. Пользовательские GET/POST методы в том же контроллере
        // не должны автоматически превращаться в management CRUD-операции.
        var crudOperation = action.MethodInfo
            .GetCustomAttributes<CrudOperationAttribute>(inherit: true)
            .FirstOrDefault();

        if (crudOperation is null)
        {
            return null;
        }

        var methods = action.ActionConstraints?
            .OfType<HttpMethodActionConstraint>()
            .SelectMany(static constraint => constraint.HttpMethods)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        if (methods.Length != 1)
        {
            return null;
        }

        var method = methods[0];

        return crudOperation.Operation switch
        {
            CrudOperationKind.Read when method.Equals("GET", StringComparison.OrdinalIgnoreCase)
                => HasRouteParameter(action, "id")
                    ? ManagementResourceOperation.Get
                    : ManagementResourceOperation.List,

            CrudOperationKind.Create when method.Equals("POST", StringComparison.OrdinalIgnoreCase)
                => ManagementResourceOperation.Create,

            CrudOperationKind.Update when method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
                => ManagementResourceOperation.Update,

            CrudOperationKind.Update when method.Equals("PATCH", StringComparison.OrdinalIgnoreCase)
                => ManagementResourceOperation.Patch,

            CrudOperationKind.Delete when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase)
                => ManagementResourceOperation.Delete,

            _ => null
        };
    }

    private static bool HasRouteParameter(
        ControllerActionDescriptor action,
        string parameterName)
    {
        var template = action.AttributeRouteInfo?.Template;
        return template?.Contains(
            "{" + parameterName,
            StringComparison.OrdinalIgnoreCase) == true;
    }

    private static Type[]? TryGetCrudGenericArguments(Type controllerType)
    {
        for (var current = controllerType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType
                && current.GetGenericTypeDefinition() == typeof(CrudController<,,,,,>))
            {
                return current.GetGenericArguments();
            }
        }

        return null;
    }

    private static Dictionary<ManagementResourceOperation, string> CreatePermissionMap(
        IPermissionNameResolver resolver,
        Type entityType,
        IEnumerable<ManagementResourceOperation> operations)
    {
        var result = new Dictionary<ManagementResourceOperation, string>();

        foreach (var operation in operations)
        {
            var permissionOperation = operation switch
            {
                ManagementResourceOperation.List or ManagementResourceOperation.Get
                    => PermissionOperation.Read,
                ManagementResourceOperation.Create => PermissionOperation.Create,
                ManagementResourceOperation.Update or ManagementResourceOperation.Patch
                    => PermissionOperation.Update,
                ManagementResourceOperation.Delete => PermissionOperation.Delete,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };

            result[operation] = resolver.GetPermission(
                entityType,
                permissionOperation.ToString());
        }

        return result;
    }

    private static string? ResolveKeyJsonName(
        Type entityType,
        Type keyType,
        ManagementModelDescriptor readModel,
        ManagementResourceOptions? resourceOptions)
    {
        if (!string.IsNullOrWhiteSpace(resourceOptions?.KeyJsonName))
        {
            return resourceOptions.KeyJsonName.Trim();
        }

        var conventionalNames = new[]
        {
            "Id",
            entityType.Name + "Id"
        };

        foreach (var conventionalName in conventionalNames)
        {
            var property = readModel.Properties.FirstOrDefault(property =>
                string.Equals(property.Name, conventionalName, StringComparison.OrdinalIgnoreCase));

            if (property is not null)
            {
                return property.JsonName;
            }
        }

        var keyTypeName = GetTypeName(keyType);
        var candidates = readModel.Properties
            .Where(property => string.Equals(property.TypeName, keyTypeName, StringComparison.Ordinal))
            .Where(property => property.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return candidates.Length == 1
            ? candidates[0].JsonName
            : null;
    }

    private static string? ResolveGroupName(ControllerActionDescriptor descriptor)
    {
        var actionSettings = descriptor.MethodInfo
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), inherit: true)
            .OfType<ApiExplorerSettingsAttribute>()
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(actionSettings?.GroupName))
        {
            return actionSettings.GroupName;
        }

        var controllerSettings = descriptor.ControllerTypeInfo
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), inherit: true)
            .OfType<ApiExplorerSettingsAttribute>()
            .FirstOrDefault();

        return !string.IsNullOrWhiteSpace(controllerSettings?.GroupName)
            ? controllerSettings.GroupName
            : descriptor.ControllerName;
    }

    private static string GetTypeName(Type type) =>
        type.FullName ?? type.Name;
}

internal sealed record CrudManagementResourceDefinition(
    ManagementResourceDescriptor Descriptor,
    IReadOnlyDictionary<ManagementResourceOperation, ControllerActionDescriptor> Actions);
