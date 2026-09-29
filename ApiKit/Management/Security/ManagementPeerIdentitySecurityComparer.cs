namespace ApiKit.Management.Security;

/// <summary>
/// Сравнивает management identity только по security-значимым полям.
/// </summary>
/// <remarks>
/// <see cref="ManagementPeerIdentity.Metadata"/> намеренно не участвует в сравнении,
/// поскольку metadata является описательной информацией и не должна изменять
/// криптографическую identity участника.
/// </remarks>
public static class ManagementPeerIdentitySecurityComparer
{
    /// <summary>
    /// Проверяет равенство двух management identity по типу участника,
    /// стабильному идентификатору и идентификатору экземпляра.
    /// </summary>
    public static bool Equals(
        ManagementPeerIdentity? left,
        ManagementPeerIdentity? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Kind == right.Kind &&
               StringComparer.Ordinal.Equals(left.PeerId, right.PeerId) &&
               StringComparer.Ordinal.Equals(left.InstanceId, right.InstanceId);
    }
}
