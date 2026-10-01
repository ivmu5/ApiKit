namespace ApiKit.Authentication.Options;

/// <summary>
/// Defines configuration settings for JWT bearer validation options.
/// </summary>
public sealed class JwtBearerValidationOptions
{
    /// <summary>
    /// Gets or sets section name.
    /// </summary>
    public const string SectionName = "Jwt:Validation";

    /// <summary>
    /// Gets or sets valid issuer.
    /// </summary>
    public string ValidIssuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets valid audiences.
    /// </summary>
    public string[] ValidAudiences { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets clock skew.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets role claim type.
    /// </summary>
    public string RoleClaimType { get; set; } = "role";
}
