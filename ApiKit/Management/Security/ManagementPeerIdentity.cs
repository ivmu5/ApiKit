using ApiKit.Management.Models;

namespace ApiKit.Management.Security;

/// <summary>
/// Описывает стабильную security identity участника management plane.
/// </summary>
/// <param name="PeerId">
/// Стабильный технический идентификатор участника. Для управляемого сервиса обычно совпадает с его ServiceName.
/// </param>
/// <param name="Kind">Роль участника в management plane.</param>
public sealed record ManagementPeerIdentity(
    string PeerId,
    ManagementPeerKind Kind)
{
    /// <summary>
    /// Идентификатор конкретного запущенного экземпляра, если identity относится к процессу с отдельным instance id.
    /// </summary>
    public string? InstanceId { get; init; }

    /// <summary>
    /// Дополнительная metadata identity, не используемая как самостоятельное доказательство доверия.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();

    /// <summary>
    /// Создаёт security identity для конкретного экземпляра управляемого сервиса.
    /// </summary>
    /// <param name="serviceIdentity">Идентификация экземпляра сервиса.</param>
    /// <returns>
    /// Identity с <see cref="PeerId"/>, равным <see cref="ManagementServiceIdentity.ServiceName"/>,
    /// и <see cref="InstanceId"/>, равным идентификатору текущего процесса сервиса.
    /// </returns>
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
    /// Создаёт security identity центрального management host.
    /// </summary>
    /// <param name="peerId">Стабильный технический идентификатор management host.</param>
    /// <returns>Identity с типом <see cref="ManagementPeerKind.ManagementHost"/>.</returns>
    public static ManagementPeerIdentity ForManagementHost(string peerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(peerId);

        return new ManagementPeerIdentity(
            peerId,
            ManagementPeerKind.ManagementHost);
    }

    /// <summary>
    /// Создаёт security identity административного management-клиента.
    /// </summary>
    /// <param name="peerId">Стабильный технический идентификатор административного клиента.</param>
    /// <returns>Identity с типом <see cref="ManagementPeerKind.AdminClient"/>.</returns>
    public static ManagementPeerIdentity ForAdminClient(string peerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(peerId);

        return new ManagementPeerIdentity(
            peerId,
            ManagementPeerKind.AdminClient);
    }

}
