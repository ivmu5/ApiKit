using ApiKit.Management.Models;

namespace ApiKit.Management.Security;

/// <summary>
/// Identifies a participant in management authentication, including the instance ID when applicable.
/// </summary>
/// <param name="PeerId">The peer ID value.</param>
/// <param name="Kind">The kind value.</param>
public sealed record ManagementPeerIdentity(
    string PeerId,
    ManagementPeerKind Kind)
{
    /// <summary>
    /// Gets or sets instance id.
    /// </summary>
    public string? InstanceId { get; init; }

    /// <summary>
    /// Gets or sets metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();

    /// <summary>
    /// Creates the value for managed service.
    /// </summary>
    /// <param name="serviceIdentity">The managed service identity.</param>
    /// <returns>The result of the operation.</returns>
    public static ManagementPeerIdentity ForManagedService(
        ManagementServiceIdentity serviceIdentity)
    {
        ArgumentNullException.ThrowIfNull(serviceIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceIdentity.ServiceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceIdentity.InstanceId);

        return new ManagementPeerIdentity(
            serviceIdentity.ServiceName,
            ManagementPeerKind.ManagedService)
        {
            InstanceId = serviceIdentity.InstanceId
        };
    }
    /// <summary>
    /// Creates the value for management host.
    /// </summary>
    /// <param name="peerId">The peer ID value.</param>
    /// <returns>The result of the operation.</returns>
    public static ManagementPeerIdentity ForManagementHost(string peerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(peerId);

        return new ManagementPeerIdentity(
            peerId,
            ManagementPeerKind.ManagementHost);
    }

    /// <summary>
    /// Creates the value for admin client.
    /// </summary>
    /// <param name="peerId">The peer ID value.</param>
    /// <returns>The result of the operation.</returns>
    public static ManagementPeerIdentity ForAdminClient(string peerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(peerId);

        return new ManagementPeerIdentity(
            peerId,
            ManagementPeerKind.AdminClient);
    }

}
