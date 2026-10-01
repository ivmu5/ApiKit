using System.Reflection;
using ApiKit.Authorization.Permissions;
using ApiKit.Crud.Attributes;
using ApiKit.Crud.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace ApiKit.Crud.Authorization;

internal sealed class CrudPermissionApplicationModelConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        ArgumentNullException.ThrowIfNull(application);

        foreach (var controller in application.Controllers)
        {
            var entityType = TryGetCrudEntityType(controller.ControllerType.AsType());

            if (entityType is null)
            {
                continue;
            }

            foreach (var action in controller.Actions)
            {
                ApplyPermission(action, entityType);
            }
        }
    }

    private static void ApplyPermission(ActionModel action, Type entityType)
    {
        var declaredAuthorization = action.ActionMethod
            .GetCustomAttributes(inherit: false)
            .Any(attribute => attribute is IAuthorizeData or IAllowAnonymous);

        if (declaredAuthorization)
        {
            return;
        }

        var crudOperation = action.ActionMethod
            .GetCustomAttributes<CrudOperationAttribute>(inherit: true)
            .FirstOrDefault();

        if (crudOperation is null)
        {
            return;
        }

        var policyName = PermissionPolicyName.ForResource(
            entityType,
            ToPermissionOperation(crudOperation.Operation));

        action.Filters.Add(new AuthorizeFilter(policyName));
    }

    private static PermissionOperation ToPermissionOperation(CrudOperationKind operation) =>
        operation switch
        {
            CrudOperationKind.Read => PermissionOperation.Read,
            CrudOperationKind.Create => PermissionOperation.Create,
            CrudOperationKind.Update => PermissionOperation.Update,
            CrudOperationKind.Delete => PermissionOperation.Delete,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

    private static Type? TryGetCrudEntityType(Type controllerType)
    {
        for (var current = controllerType; current is not null; current = current.BaseType)
        {
            if (!current.IsGenericType
                || current.GetGenericTypeDefinition() != typeof(CrudController<,,,,,>))
            {
                continue;
            }

            return current.GetGenericArguments()[1];
        }

        return null;
    }
}
