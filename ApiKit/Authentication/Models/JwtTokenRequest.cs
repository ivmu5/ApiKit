using System.Security.Claims;

namespace ApiKit.Authentication.Models;

/// <summary>
/// Описывает данные, необходимые для создания JWT-токена.
/// </summary>
public sealed class JwtTokenRequest
{
    /// <summary>
    /// Создаёт запрос на выпуск JWT-токена для указанного субъекта.
    /// </summary>
    /// <param name="subject">
    /// Идентификатор субъекта токена. Значение будет записано в claim <c>sub</c>.
    /// </param>
    public JwtTokenRequest(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        Subject = subject;
    }

    /// <summary>
    /// Идентификатор субъекта JWT-токена.
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// Дополнительные claims, которые будут добавлены в JWT-токен.
    /// Зарезервированные JWT claims добавляются генератором автоматически
    /// и не должны передаваться через эту коллекцию.
    /// </summary>
    public IReadOnlyCollection<Claim> Claims { get; init; } = Array.Empty<Claim>();

    /// <summary>
    /// Необязательный идентификатор JWT-токена.
    /// Если значение не указано, генератор создаст новый <c>jti</c> автоматически.
    /// </summary>
    public string? JwtId { get; init; }

    /// <summary>
    /// Необязательная аудитория токена.
    /// Если значение не указано, используется аудитория из настроек генерации JWT.
    /// </summary>
    public string? Audience { get; init; }

    /// <summary>
    /// Необязательное время жизни токена.
    /// Если значение не указано, используется значение из настроек генерации JWT.
    /// </summary>
    public TimeSpan? Lifetime { get; init; }
}
