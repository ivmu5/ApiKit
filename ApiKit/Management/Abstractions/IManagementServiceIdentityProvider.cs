using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management service identity provider.
/// </summary>
public interface IManagementServiceIdentityProvider
{
    /// <summary>
    /// Returns identity.
    /// </summary>
    ManagementServiceIdentity GetIdentity();
}
