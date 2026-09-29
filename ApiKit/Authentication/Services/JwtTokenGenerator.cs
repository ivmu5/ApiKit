using ApiKit.Authentication.Abstractions;
using ApiKit.Authentication.Models;
using ApiKit.Authentication.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ApiKit.Authentication.Services;

/// <summary>
/// Создаёт JWT-токены, не привязываясь к конкретному механизму хранения ключей.
/// </summary>
internal sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private static readonly HashSet<string> ReservedClaimTypes = new(
        StringComparer.Ordinal)
    {
        JwtRegisteredClaimNames.Iss,
        JwtRegisteredClaimNames.Sub,
        JwtRegisteredClaimNames.Aud,
        JwtRegisteredClaimNames.Exp,
        JwtRegisteredClaimNames.Nbf,
        JwtRegisteredClaimNames.Iat,
        JwtRegisteredClaimNames.Jti
    };

    private readonly JwtTokenGenerationOptions _options;
    private readonly IJwtSigningCredentialsProvider _signingCredentialsProvider;
    private readonly TimeProvider _timeProvider;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    /// <summary>
    /// Создаёт генератор JWT-токенов.
    /// </summary>
    /// <param name="options">Настройки создания JWT-токенов.</param>
    /// <param name="signingCredentialsProvider">Провайдер параметров подписи.</param>
    /// <param name="timeProvider">Источник текущего времени.</param>
    public JwtTokenGenerator(
        IOptions<JwtTokenGenerationOptions> options,
        IJwtSigningCredentialsProvider signingCredentialsProvider,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(signingCredentialsProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _options = options.Value;
        _signingCredentialsProvider = signingCredentialsProvider;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public JwtTokenResult Generate(JwtTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Claims);

        ValidateRequest(request);

        var lifetime = request.Lifetime ?? _options.DefaultLifetime;
        var audience = request.Audience ?? _options.Audience;
        var jwtId = request.JwtId ?? Guid.NewGuid().ToString("N");
        var issuedAt = _timeProvider.GetUtcNow();
        var expiresAt = issuedAt.Add(lifetime);

        var claims = new List<Claim>(request.Claims.Count + 2)
        {
            new(JwtRegisteredClaimNames.Sub, request.Subject),
            new(JwtRegisteredClaimNames.Jti, jwtId)
        };

        claims.AddRange(request.Claims);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _signingCredentialsProvider.GetSigningCredentials(),

            // Если ключ подписи содержит KeyId, он автоматически попадёт в kid
            // и сможет использоваться для выбора ключа при проверке токена.
            IncludeKeyIdInHeader = true,
            TokenType = "JWT"
        };

        var token = _tokenHandler.CreateToken(descriptor);
        var accessToken = _tokenHandler.WriteToken(token);

        return new JwtTokenResult(
            accessToken,
            jwtId,
            issuedAt,
            expiresAt);
    }

    /// <summary>
    /// Проверяет параметры конкретного запроса на выпуск токена.
    /// </summary>
    private static void ValidateRequest(JwtTokenRequest request)
    {
        if (request.Lifetime is { } lifetime &&
            lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Время жизни JWT-токена должно быть больше нуля.");
        }

        if (request.Audience is not null &&
            string.IsNullOrWhiteSpace(request.Audience))
        {
            throw new ArgumentException(
                "Аудитория JWT-токена не может быть пустой.",
                nameof(request));
        }

        if (request.JwtId is not null &&
            string.IsNullOrWhiteSpace(request.JwtId))
        {
            throw new ArgumentException(
                "Идентификатор JWT-токена не может быть пустым.",
                nameof(request));
        }

        var reservedClaim = request.Claims.FirstOrDefault(
            claim => ReservedClaimTypes.Contains(claim.Type));

        if (reservedClaim is not null)
        {
            throw new ArgumentException(
                $"Claim '{reservedClaim.Type}' управляется ApiKit и не должен передаваться вручную.",
                nameof(request));
        }
    }
}
