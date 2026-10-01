namespace ApiKit.Management.Options;

/// <summary>
/// Defines configuration settings for management security options.
/// </summary>
public sealed class ManagementSecurityOptions
{
    /// <summary>
    /// Gets or sets challenge lifetime.
    /// </summary>
    public TimeSpan ChallengeLifetime { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets nonce size bytes.
    /// </summary>
    public int NonceSizeBytes { get; set; } = 32;

    /// <summary>
    /// Gets or sets maximum pending challenges.
    /// </summary>
    public int MaximumPendingChallenges { get; set; } = 1024;
}
