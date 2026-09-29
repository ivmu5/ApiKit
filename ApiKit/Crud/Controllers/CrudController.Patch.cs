using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.EntityFrameworkCore;

namespace ApiKit.Crud.Controllers;

public abstract partial class CrudController<
    TDbContext,
    TEntity,
    TKey,
    TReadModel,
    TCreateModel,
    TUpdateModel>
    where TDbContext : DbContext
    where TEntity : class
    where TCreateModel : class
    where TUpdateModel : class
{
    /// <summary>
    /// Проверяет базовые ограничения JSON Patch до его применения.
    /// </summary>
    /// <param name="patchDocument">JSON Patch документ.</param>
    protected virtual void ValidatePatchDocument(JsonPatchDocument<TUpdateModel> patchDocument)
    {
        var operations = ((IJsonPatchDocument)patchDocument).GetOperations();

        if (operations.Count > MaxPatchOperations)
        {
            ModelState.AddModelError(
                nameof(patchDocument),
                $"JSON Patch не может содержать более {MaxPatchOperations} операций.");
            return;
        }

        var allowedPaths = GetPatchablePaths();

        for (var index = 0; index < operations.Count; index++)
        {
            ValidatePatchPath(
                operations[index].path,
                $"operations[{index}].path",
                allowedPaths);

            if (!string.IsNullOrWhiteSpace(operations[index].from))
            {
                ValidatePatchPath(
                    operations[index].from,
                    $"operations[{index}].from",
                    allowedPaths);
            }
        }
    }

    /// <summary>
    /// Возвращает верхнеуровневые свойства update-модели, доступные для JSON Patch.
    /// </summary>
    /// <returns>Допустимые имена свойств.</returns>
    /// <remarks>
    /// Проверка по умолчанию ограничивает только корневой сегмент JSON Pointer.
    /// Для более строгих правил можно переопределить <see cref="ValidatePatchDocument"/>.
    /// </remarks>
    protected virtual IReadOnlySet<string> GetPatchablePaths()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in typeof(TUpdateModel)
                     .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(property =>
                         property.SetMethod?.IsPublic == true
                         && property.GetCustomAttribute<JsonIgnoreAttribute>() is null))
        {
            result.Add(property.Name);

            if (property.GetCustomAttribute<JsonPropertyNameAttribute>() is { } jsonName)
            {
                result.Add(jsonName.Name);
            }
        }

        return result;
    }

    private void ValidatePatchPath(
        string? path,
        string modelStateKey,
        IReadOnlySet<string> allowedPaths)
    {
        var root = GetJsonPointerRoot(path);

        if (string.IsNullOrWhiteSpace(root))
        {
            ModelState.AddModelError(
                modelStateKey,
                "JSON Patch path должен указывать на свойство модели.");
            return;
        }

        if (!allowedPaths.Contains(root))
        {
            ModelState.AddModelError(
                modelStateKey,
                $"Свойство '{root}' недоступно для PATCH.");
        }
    }

    private static string? GetJsonPointerRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path[0] != '/')
        {
            return null;
        }

        var slashIndex = path.IndexOf('/', 1);
        var segment = slashIndex < 0 ? path[1..] : path[1..slashIndex];

        return segment
            .Replace("~1", "/", StringComparison.Ordinal)
            .Replace("~0", "~", StringComparison.Ordinal);
    }
}
