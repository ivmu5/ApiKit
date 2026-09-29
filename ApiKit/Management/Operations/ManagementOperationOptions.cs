using ApiKit.Management.Security;

namespace ApiKit.Management.Operations;

/// <summary>
/// Настройки произвольной management-операции.
/// </summary>
public sealed class ManagementOperationOptions
{
    /// <summary>Отображаемое имя операции.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Описание операции.</summary>
    public string? Description { get; set; }

    /// <summary>Группа операции в админ-панели.</summary>
    public string? GroupName { get; set; }

    /// <summary>ASP.NET Core authorization policy, необходимая для вызова.</summary>
    public string AuthorizationPolicy { get; set; } = ApiKitManagementAuthorizationDefaults.PolicyName;
}
