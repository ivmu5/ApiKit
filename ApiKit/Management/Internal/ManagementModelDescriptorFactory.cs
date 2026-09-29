using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApiKit.Management.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Internal;

internal sealed class ManagementModelDescriptorFactory(IServiceProvider serviceProvider)
{
    public ManagementModelDescriptor Create(Type modelType)
    {
        ArgumentNullException.ThrowIfNull(modelType);

        var mvcJsonOptions = serviceProvider
            .GetService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>();
        var namingPolicy = mvcJsonOptions is null
            ? JsonNamingPolicy.CamelCase
            : mvcJsonOptions.Value.JsonSerializerOptions.PropertyNamingPolicy;

        var nullability = new NullabilityInfoContext();
        var properties = modelType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.GetMethod?.IsPublic == true)
            .Where(static property => !IsAlwaysIgnored(property))
            .Select(property => CreateProperty(property, namingPolicy, nullability))
            .OrderBy(static property => property.JsonName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ManagementModelDescriptor
        {
            TypeName = GetTypeName(modelType),
            Properties = properties
        };
    }

    private static ManagementPropertyDescriptor CreateProperty(
        PropertyInfo property,
        JsonNamingPolicy? namingPolicy,
        NullabilityInfoContext nullability)
    {
        var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
            ?? namingPolicy?.ConvertName(property.Name)
            ?? property.Name;
        var propertyType = property.PropertyType;
        var enumType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        return new ManagementPropertyDescriptor
        {
            Name = property.Name,
            JsonName = jsonName,
            TypeName = GetTypeName(propertyType),
            IsNullable = IsNullable(property, nullability),
            IsRequired = property.IsDefined(typeof(RequiredAttribute), inherit: true)
                || property.IsDefined(typeof(RequiredMemberAttribute), inherit: true),
            IsReadOnly = property.SetMethod?.IsPublic != true,
            IsCollection = propertyType != typeof(string)
                && typeof(IEnumerable).IsAssignableFrom(propertyType),
            AllowedValues = enumType.IsEnum
                ? Enum.GetNames(enumType)
                : []
        };
    }

    private static bool IsAlwaysIgnored(PropertyInfo property)
    {
        var ignore = property.GetCustomAttribute<JsonIgnoreAttribute>();
        return ignore is not null && ignore.Condition == JsonIgnoreCondition.Always;
    }

    private static bool IsNullable(
        PropertyInfo property,
        NullabilityInfoContext nullability)
    {
        var type = property.PropertyType;
        if (Nullable.GetUnderlyingType(type) is not null)
        {
            return true;
        }

        if (type.IsValueType)
        {
            return false;
        }

        return nullability.Create(property).ReadState != NullabilityState.NotNull;
    }

    private static string GetTypeName(Type type) =>
        type.FullName ?? type.Name;
}
