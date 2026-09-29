using ApiKit.Authentication.Models;

namespace ApiKit.Authentication.Abstractions;

/// <summary>
/// Определяет механизм создания JWT-токенов.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Создаёт подписанный JWT-токен.
    /// </summary>
    /// <param name="request">Параметры создаваемого токена.</param>
    /// <returns>Результат создания JWT-токена.</returns>
    JwtTokenResult Generate(JwtTokenRequest request);
}
