using System.Security.Claims;

namespace ApiKit.Authentication.Models;

/// <summary>
/// Defines a request for JWT token request.
/// </summary>
public sealed class JwtTokenRequest
{
    /// <summary>
    /// Initializes a new JwtTokenRequest instance.
    /// </summary>
    /// <param name="subject">The intended challenge recipient identity.</param>
    public JwtTokenRequest(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        Subject = subject;
    }

    /// <summary>
    /// Gets subject.
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// Gets or sets claims.
    /// </summary>
    public IReadOnlyCollection<Claim> Claims { get; init; } = Array.Empty<Claim>();

    /// <summary>
    /// Gets or sets JWT id.
    /// </summary>
    public string? JwtId { get; init; }

    /// <summary>
    /// Gets or sets audience.
    /// </summary>
    public string? Audience { get; init; }

    /// <summary>
    /// Gets or sets lifetime.
    /// </summary>
    public TimeSpan? Lifetime { get; init; }
}
