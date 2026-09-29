namespace ApiKit.Authentication.Options;

/// <summary>
/// Содержит настройки проверки входящих JWT-токенов.
/// </summary>
public sealed class JwtBearerValidationOptions
{
    /// <summary>
    /// Имя секции конфигурации по умолчанию.
    /// </summary>
    public const string SectionName = "Jwt:Validation";

    /// <summary>
    /// Допустимый издатель JWT-токена.
    /// </summary>
    public string ValidIssuer { get; set; } = string.Empty;

    /// <summary>
    /// Допустимые аудитории JWT-токена.
    /// </summary>
    public string[] ValidAudiences { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Допустимое расхождение системного времени при проверке срока действия токена.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Тип claim, который ASP.NET Core использует для проверки ролей.
    /// </summary>
    public string RoleClaimType { get; set; } = "role";
}
