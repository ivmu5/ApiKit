namespace ApiKit.Authentication.Options;

/// <summary>
/// Содержит настройки создания JWT-токенов.
/// </summary>
public sealed class JwtTokenGenerationOptions
{
    /// <summary>
    /// Имя секции конфигурации по умолчанию.
    /// </summary>
    public const string SectionName = "Jwt:Generation";

    /// <summary>
    /// Издатель создаваемых JWT-токенов.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Аудитория создаваемых JWT-токенов.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Время жизни JWT-токена по умолчанию.
    /// </summary>
    public TimeSpan DefaultLifetime { get; set; } = TimeSpan.FromMinutes(15);
}
