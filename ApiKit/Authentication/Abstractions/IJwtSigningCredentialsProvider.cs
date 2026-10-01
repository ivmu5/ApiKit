using Microsoft.IdentityModel.Tokens;

namespace ApiKit.Authentication.Abstractions;

/// <summary>
/// Defines the contract for i JWT signing credentials provider.
/// </summary>
public interface IJwtSigningCredentialsProvider
{
    /// <summary>
    /// Returns signing credentials.
    /// </summary>
    /// <returns>The requested value.</returns>
    SigningCredentials GetSigningCredentials();
}
