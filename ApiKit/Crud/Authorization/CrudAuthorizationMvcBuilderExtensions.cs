using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ApiKit.Crud.Authorization;

/// <summary>
/// Defines extension methods for CRUD authorization MVC builder extensions.
/// </summary>
public static class CrudAuthorizationMvcBuilderExtensions
{
    /// <summary>
    /// Registers API kit CRUD permissions.
    /// </summary>
    /// <param name="mvcBuilder">The ASP.NET Core MVC builder.</param>
    /// <returns>The builder or service collection for further configuration.</returns>
    public static IMvcBuilder AddApiKitCrudPermissions(this IMvcBuilder mvcBuilder)
    {
        ArgumentNullException.ThrowIfNull(mvcBuilder);

        mvcBuilder.Services.Configure<MvcOptions>(options =>
        {
            if (options.Conventions.All(
                    convention => convention is not CrudPermissionApplicationModelConvention))
            {
                options.Conventions.Add(new CrudPermissionApplicationModelConvention());
            }
        });

        return mvcBuilder;
    }
}
