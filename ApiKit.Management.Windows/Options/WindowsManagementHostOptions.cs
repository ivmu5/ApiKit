namespace ApiKit.Management.Windows.Options;

/// <summary>
/// Configures management host identity, Named Pipe endpoints, message limits, and pipe ACL settings.
/// </summary>
public sealed class WindowsManagementHostOptions
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
    /// Gets or sets administration pipe name.
    /// </summary>
    public string AdministrationPipeName { get; set; } = "ApiKit.Management.Admin";

    /// <summary>
    /// Gets or sets max message bytes.
    /// </summary>
    public int MaxMessageBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>
    /// Gets or sets service connect timeout.
    /// </summary>
    public TimeSpan ServiceConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets registration pipe access.
    /// </summary>
    public WindowsNamedPipeAccessOptions RegistrationPipeAccess { get; } = new();

    /// <summary>
    /// Gets administration pipe access.
    /// </summary>
    public WindowsNamedPipeAccessOptions AdministrationPipeAccess { get; } = new();

    /// <summary>
    /// Gets or sets whether use provisioned service SID ACL is enabled.
    /// </summary>
    public bool UseProvisionedServiceSidAcl { get; set; } = true;

    /// <summary>
    /// Gets or sets whether built in administrators on administration pipe is enabled.
    /// </summary>
    public bool AllowBuiltInAdministratorsOnAdministrationPipe { get; set; } = true;
}
