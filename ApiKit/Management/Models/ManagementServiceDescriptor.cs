namespace ApiKit.Management.Models;

/// <summary>
/// Describes a managed service and its exposed management endpoints.
/// </summary>
public sealed record ManagementServiceDescriptor
{
    /// <summary>
    /// Gets or sets identity.
    /// </summary>
    public required ManagementServiceIdentity Identity { get; init; }

    /// <summary>
    /// Gets or sets addresses.
    /// </summary>
    public IReadOnlyList<ManagementServiceAddress> Addresses { get; init; } = [];

    /// <summary>
    /// Gets or sets transports.
    /// </summary>
    public IReadOnlyList<ManagementTransportDescriptor> Transports { get; init; } = [];

    /// <summary>
    /// Gets or sets endpoints.
    /// </summary>
    public IReadOnlyList<ManagementEndpointDescriptor> Endpoints { get; init; } = [];

    /// <summary>
    /// Gets or sets resources.
    /// </summary>
    public IReadOnlyList<ManagementResourceDescriptor> Resources { get; init; } = [];

    /// <summary>
    /// Gets or sets operations.
    /// </summary>
    public IReadOnlyList<ManagementOperationDescriptor> Operations { get; init; } = [];

    /// <summary>
    /// Gets or sets capabilities.
    /// </summary>
    public IReadOnlyList<string> Capabilities { get; init; } = [];

    /// <summary>
    /// Gets or sets metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();
}
