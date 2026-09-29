namespace ApiKit.Authentication.Options;

/// <summary>
/// Содержит настройки HMAC-SHA256 ключа JWT.
/// </summary>
public sealed class JwtHmacOptions
{
    /// <summary>
    /// Имя секции конфигурации по умолчанию.
    /// </summary>
    public const string SectionName = "Jwt:Hmac";

    /// <summary>
    /// Секретный ключ, используемый для подписи и проверки JWT-токенов.
    /// </summary>
    public string Key { get; set; } = string.Empty;
}
