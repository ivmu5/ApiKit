namespace ApiKit.Management.Windows.Options;

/// <summary>
/// Defines configuration settings for windows management client options.
/// </summary>
public sealed class WindowsManagementClientOptions
{
    /// <summary>
    /// Gets or sets admin client peer id.
    /// </summary>
    public string AdminClientPeerId { get; set; } = "admin-client";

    /// <summary>
    /// Gets or sets management host peer id.
    /// </summary>
    public string ManagementHostPeerId { get; set; } = "management-host";

    /// <summary>
    /// Gets or sets administration pipe name.
    /// </summary>
    public string AdministrationPipeName { get; set; } = "ApiKit.Management.Admin";

    /// <summary>
    /// Gets or sets connect timeout.
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets max message bytes.
    /// </summary>
    public int MaxMessageBytes { get; set; } = 4 * 1024 * 1024;
}
