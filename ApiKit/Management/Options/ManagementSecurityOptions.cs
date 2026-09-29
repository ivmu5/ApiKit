namespace ApiKit.Management.Options;

/// <summary>
/// Настройки одноразовых challenge, используемых для криптографической
/// аутентификации участников management plane.
/// </summary>
public sealed class ManagementSecurityOptions
{
    /// <summary>
    /// Срок жизни одного challenge.
    /// </summary>
    public TimeSpan ChallengeLifetime { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Размер криптографически случайного nonce в байтах.
    /// </summary>
    public int NonceSizeBytes { get; set; } = 32;

    /// <summary>
    /// Максимальное количество одновременно ожидающих challenge в текущем процессе.
    /// </summary>
    /// <remarks>
    /// Ограничение защищает in-memory provider от неограниченного роста состояния,
    /// если peer открывает соединения и не завершает handshake.
    /// </remarks>
    public int MaximumPendingChallenges { get; set; } = 1024;
}
