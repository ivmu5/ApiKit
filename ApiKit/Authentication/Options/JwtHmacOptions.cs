namespace ApiKit.Authentication.Options;

/// <summary>
/// Defines configuration settings for JWT HMAC options.
/// </summary>
public sealed class JwtHmacOptions
{
    /// <summary>
    /// Gets or sets section name.
    /// </summary>
    public const string SectionName = "Jwt:Hmac";

    /// <summary>
    /// Gets or sets key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
}
