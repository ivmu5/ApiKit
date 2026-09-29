using ApiKit.Authentication.Abstractions;
using ApiKit.Authentication.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApiKit.Authentication.Internal;

internal static class JwtTokenValidationParametersFactory
{
    public static TokenValidationParameters Create(
        JwtBearerValidationOptions options,
        IJwtValidationKeysProvider keyProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(keyProvider);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.ValidIssuer,

            ValidateAudience = true,
            ValidAudiences = options.ValidAudiences,

            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = options.ClockSkew,

            // Позволяет использовать стандартные [Authorize(Roles = "...")]
            // и ClaimsPrincipal.IsInRole(...) без преобразования имён входящих утверждений.
            RoleClaimType = options.RoleClaimType,

            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,

            // Механизм выбора поддерживает несколько одновременно действующих ключей.
            // Если токен содержит kid, используются только ключи с совпадающим KeyId.
            IssuerSigningKeyResolver = (_, _, keyId, _) =>
            {
                var keys = keyProvider.GetValidationKeys();

                if (string.IsNullOrWhiteSpace(keyId))
                {
                    return keys;
                }

                return keys.Where(key =>
                    string.Equals(key.KeyId, keyId, StringComparison.Ordinal));
            }
        };
    }
}
