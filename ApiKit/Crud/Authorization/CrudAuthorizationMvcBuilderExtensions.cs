using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ApiKit.Crud.Authorization;

/// <summary>
/// Содержит интеграцию generic CRUD с permission-based authorization ApiKit.
/// </summary>
public static class CrudAuthorizationMvcBuilderExtensions
{
    /// <summary>
    /// Автоматически связывает стандартные CRUD actions с permissions ресурса:
    /// GET — read, POST — create, PUT/PATCH — update, DELETE — delete.
    /// </summary>
    /// <remarks>
    /// Метод является опциональной интеграцией. Сам generic CRUD не требует
    /// permission-based authorization. До вызова этого метода необходимо зарегистрировать
    /// authorization через <c>AddApiKitAuthorization</c>.
    /// </remarks>
    /// <param name="mvcBuilder">Стандартный MVC builder ASP.NET Core.</param>
    /// <returns>Исходный MVC builder.</returns>
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
