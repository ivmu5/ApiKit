namespace ApiKit.Management.Security;

/// <summary>
/// Compares the security-relevant kind, peer ID, and instance ID of management peers.
/// </summary>
public static class ManagementPeerIdentitySecurityComparer
{
    /// <summary>
    /// Compares the security-relevant fields of two management identities.
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
