namespace ApiKit.Management.Models;

/// <summary>
/// Describes the transport endpoint exposed by a managed service.
/// </summary>
public sealed record ManagementEndpointDescriptor
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets route.
    /// </summary>
    public required string Route { get; init; }

    /// <summary>
    /// Gets or sets http methods.
    /// </summary>
    public required IReadOnlyList<string> HttpMethods { get; init; }

    /// <summary>
    /// Gets or sets group name.
    /// </summary>
    public string? GroupName { get; init; }

    /// <summary>
    /// Gets or sets controller name.
    /// </summary>
    public string? ControllerName { get; init; }

    /// <summary>
    /// Gets or sets action name.
    /// </summary>
    public string? ActionName { get; init; }

    /// <summary>
    /// Gets or sets whether allows anonymous is enabled.
    /// </summary>
    public bool AllowsAnonymous { get; init; }

    /// <summary>
    /// Gets or sets authorization policies.
    /// </summary>
    public IReadOnlyList<string> AuthorizationPolicies { get; init; } = [];

    /// <summary>
    /// Gets or sets authorization roles.
    /// </summary>
    public IReadOnlyList<string> AuthorizationRoles { get; init; } = [];

    /// <summary>
    /// Gets or sets authentication schemes.
    /// </summary>
    public IReadOnlyList<string> AuthenticationSchemes { get; init; } = [];
}
