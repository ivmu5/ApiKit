namespace ApiKit.Management.Windows.Options;

/// <summary>
/// Defines configuration settings for windows management service options.
/// </summary>
public sealed class WindowsManagementServiceOptions
{
    /// <summary>
    /// Gets or sets management host peer id.
    /// </summary>
    public string ManagementHostPeerId { get; set; } = "management-host";

    /// <summary>
    /// Gets or sets registration pipe name.
    /// </summary>
    public string RegistrationPipeName { get; set; } = "ApiKit.Management.Registration";

    /// <summary>
    /// Gets or sets management pipe prefix.
    /// </summary>
    public string ManagementPipePrefix { get; set; } = "ApiKit.Management.Service";

    /// <summary>
    /// Gets or sets max message bytes.
    /// </summary>
    public int MaxMessageBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>
    /// Gets or sets connect timeout.
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets management pipe access.
    /// </summary>
    public WindowsNamedPipeAccessOptions ManagementPipeAccess { get; } = new();

    /// <summary>
    /// Gets or sets whether use provisioned windows isolation is enabled.
    /// </summary>
    public bool UseProvisionedWindowsIsolation { get; set; } = true;
}
