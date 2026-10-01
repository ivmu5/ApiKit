using ApiKit.Management.Security;

namespace ApiKit.Management.Operations;

/// <summary>
/// Defines configuration settings for management operation options.
/// </summary>
public sealed class ManagementOperationOptions
{
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets group name.
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// Gets or sets authorization policy.
    /// </summary>
    public string AuthorizationPolicy { get; set; } = ApiKitManagementAuthorizationDefaults.PolicyName;
}
