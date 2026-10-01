namespace ApiKit.Authentication.Options;

/// <summary>
/// Defines configuration settings for JWT token generation options.
/// </summary>
public sealed class JwtTokenGenerationOptions
{
    /// <summary>
    /// Gets or sets section name.
    /// </summary>
    public const string SectionName = "Jwt:Generation";

    /// <summary>
    /// Gets or sets issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets default lifetime.
    /// </summary>
    public TimeSpan DefaultLifetime { get; set; } = TimeSpan.FromMinutes(15);
}
