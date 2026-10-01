namespace ApiKit.Management.Security;

/// <summary>
/// Defines standard claim values and policy names for management authorization.
/// </summary>
public static class ApiKitManagementAuthorizationDefaults
{
    /// <summary>
    /// Gets or sets policy name.
    /// </summary>
    public const string PolicyName = "ApiKit.Management";

    /// <summary>
    /// Gets or sets service registration policy name.
    /// </summary>
    public const string ServiceRegistrationPolicyName = "ApiKit.Management.ServiceRegistration";

    /// <summary>
    /// Gets or sets management host policy name.
    /// </summary>
    public const string ManagementHostPolicyName = "ApiKit.Management.Host";

    /// <summary>
    /// Gets or sets lifecycle policy name.
    /// </summary>
    public const string LifecyclePolicyName = "ApiKit.Management.Lifecycle";

    /// <summary>
    /// Gets or sets package management policy name.
    /// </summary>
    public const string PackageManagementPolicyName = "ApiKit.Management.Packages";

    /// <summary>
    /// Gets or sets claim type.
    /// </summary>
    public const string ClaimType = "apikit.management";

    /// <summary>
    /// Gets or sets administrator claim value.
    /// </summary>
    public const string AdministratorClaimValue = "admin";

    /// <summary>
    /// Gets or sets service registration claim value.
    /// </summary>
    public const string ServiceRegistrationClaimValue = "service-registration";

    /// <summary>
    /// Gets or sets management host claim value.
    /// </summary>
    public const string ManagementHostClaimValue = "host";

    /// <summary>
    /// Gets or sets lifecycle claim value.
    /// </summary>
    public const string LifecycleClaimValue = "lifecycle";

    /// <summary>
    /// Gets or sets package management claim value.
    /// </summary>
    public const string PackageManagementClaimValue = "packages";
}
