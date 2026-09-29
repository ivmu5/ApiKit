using Microsoft.IdentityModel.Tokens;

namespace ApiKit.Authentication.Abstractions;

/// <summary>
/// Предоставляет ключи, которыми разрешено проверять подпись входящих JWT-токенов.
/// </summary>
public interface IJwtValidationKeysProvider
{
    /// <summary>
    /// Возвращает набор ключей, доступных для проверки подписи JWT-токенов.
    /// </summary>
    /// <returns>Набор ключей проверки подписи.</returns>
    IReadOnlyCollection<SecurityKey> GetValidationKeys();
}
