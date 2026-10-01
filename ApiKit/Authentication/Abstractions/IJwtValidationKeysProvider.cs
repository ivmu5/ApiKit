using Microsoft.IdentityModel.Tokens;

namespace ApiKit.Authentication.Abstractions;

/// <summary>
/// Defines the contract for i JWT validation keys provider.
/// </summary>
public interface IJwtValidationKeysProvider
{
    /// <summary>
    /// Returns validation keys.
    /// </summary>
    /// <returns>The requested value.</returns>
    IReadOnlyCollection<SecurityKey> GetValidationKeys();
}
