using ApiKit.Management.Security;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Управляет выпуском и одноразовым потреблением management challenge.
/// </summary>
/// <remarks>
/// Реализация отвечает за криптографически случайный nonce, короткий срок жизни и защиту от повторного
/// использования одного challenge. Сам transport не должен самостоятельно генерировать предсказуемые nonce.
/// </remarks>
public interface IManagementChallengeProvider
{
    /// <summary>
    /// Выпускает challenge от <paramref name="issuer"/> для <paramref name="subject"/> в заданном protocol context.
    /// </summary>
    ValueTask<ManagementChallenge> CreateChallengeAsync(
        ManagementPeerIdentity issuer,
        ManagementPeerIdentity subject,
        string transport,
        string purpose,
        int protocolVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Атомарно получает и удаляет ранее выпущенный challenge.
    /// </summary>
    /// <returns>
    /// Исходный challenge или <see langword="null"/>, если он неизвестен, уже использован или больше недействителен.
    /// </returns>
    ValueTask<ManagementChallenge?> ConsumeChallengeAsync(
        string challengeId,
        CancellationToken cancellationToken = default);
}
