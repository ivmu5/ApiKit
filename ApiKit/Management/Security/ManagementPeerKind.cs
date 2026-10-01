namespace ApiKit.Management.Security;

/// <summary>
/// Defines the available states for management peer kind.
/// </summary>
public enum ManagementPeerKind
{
    /// <summary>
    /// Identifies the unknown peer kind.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Identifies the management host peer kind.
    /// </summary>
    ManagementHost = 1,

    /// <summary>
    /// Identifies the admin client peer kind.
    /// </summary>
    AdminClient = 2,

    /// <summary>
    /// Identifies the managed service peer kind.
    /// </summary>
    ManagedService = 3
}
