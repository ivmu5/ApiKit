using ApiKit.Authentication.Models;

namespace ApiKit.Authentication.Abstractions;

/// <summary>
/// Defines the contract for i JWT token generator.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a signed JWT for the supplied request.
    /// </summary>
    /// <param name="request">The operation request.</param>
    /// <returns>The result of the operation.</returns>
    JwtTokenResult Generate(JwtTokenRequest request);
}
