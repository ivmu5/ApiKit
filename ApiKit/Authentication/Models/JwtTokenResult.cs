namespace ApiKit.Authentication.Models;

/// <summary>
/// Defines a result of JWT token result.
/// </summary>
/// <param name="AccessToken">The access token value.</param>
/// <param name="JwtId">The JWT ID value.</param>
/// <param name="IssuedAtUtc">The issued at UTC value.</param>
/// <param name="ExpiresAtUtc">The expires at UTC value.</param>
public sealed record JwtTokenResult(
    string AccessToken,
    string JwtId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc);
