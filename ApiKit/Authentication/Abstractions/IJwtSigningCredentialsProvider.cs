using Microsoft.IdentityModel.Tokens;

namespace ApiKit.Authentication.Abstractions;

/// <summary>
/// Предоставляет данные, необходимые для подписи создаваемых JWT-токенов.
/// </summary>
public interface IJwtSigningCredentialsProvider
{
    /// <summary>
    /// Возвращает параметры подписи JWT-токена.
    /// </summary>
    /// <returns>Параметры подписи JWT-токена.</returns>
    SigningCredentials GetSigningCredentials();
}
