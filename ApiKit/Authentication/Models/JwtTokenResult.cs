namespace ApiKit.Authentication.Models;

/// <summary>
/// Представляет результат создания JWT-токена.
/// </summary>
/// <param name="AccessToken">JWT-токен в строковом представлении.</param>
/// <param name="JwtId">Идентификатор выпущенного JWT-токена.</param>
/// <param name="IssuedAtUtc">Момент выпуска токена в UTC.</param>
/// <param name="ExpiresAtUtc">Момент окончания срока действия токена в UTC.</param>
public sealed record JwtTokenResult(
    string AccessToken,
    string JwtId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc);
