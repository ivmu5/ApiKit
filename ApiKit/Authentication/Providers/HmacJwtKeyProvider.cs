using ApiKit.Authentication.Abstractions;
using ApiKit.Authentication.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ApiKit.Authentication.Providers;

/// <summary>
/// Provides HMAC-SHA-256 signing and verification keys for JWT.
/// </summary>
internal sealed class HmacJwtKeyProvider :
    IJwtSigningCredentialsProvider,
    IJwtValidationKeysProvider
{
    private readonly SymmetricSecurityKey _securityKey;
    private readonly SigningCredentials _signingCredentials;
    private readonly IReadOnlyCollection<SecurityKey> _validationKeys;

    /// <summary>
    /// Initializes a new HmacJwtKeyProvider instance.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    public HmacJwtKeyProvider(IOptions<JwtHmacOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(options.Value.Key));

        _signingCredentials = new SigningCredentials(
            _securityKey,
            SecurityAlgorithms.HmacSha256);

        _validationKeys = new SecurityKey[] { _securityKey };
    }

    /// <inheritdoc />
    public SigningCredentials GetSigningCredentials() =>
        _signingCredentials;

    /// <inheritdoc />
    public IReadOnlyCollection<SecurityKey> GetValidationKeys() =>
        _validationKeys;
}
