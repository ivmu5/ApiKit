namespace ApiKit.Management.Windows.Options;

/// <summary>
/// Configures Windows service installation, credential provisioning, ACL isolation, and lifecycle behavior.
/// </summary>
public sealed class WindowsServiceManagementOptions
{
    /// <summary>
    /// Gets or sets install root.
    /// </summary>
    public string InstallRoot { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ApiKit",
        "Services");

    /// <summary>
    /// Gets or sets service account.
    /// </summary>
    public string ServiceAccount { get; set; } = @"NT AUTHORITY\LocalService";

    /// <summary>
    /// Gets or sets whether service SID isolation is enabled.
    /// </summary>
    public bool EnableServiceSidIsolation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether managed directories is enabled.
    /// </summary>
    public bool ProtectManagedDirectories { get; set; } = true;

    /// <summary>
    /// Gets or sets whether built in administrators full control is enabled.
    /// </summary>
    public bool AllowBuiltInAdministratorsFullControl { get; set; } = true;

    /// <summary>
    /// Gets or sets whether local system full control is enabled.
    /// </summary>
    public bool AllowLocalSystemFullControl { get; set; } = true;

    /// <summary>
    /// Gets or sets operation timeout.
    /// </summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether package verifier is enabled.
    /// </summary>
    public bool RequirePackageVerifier { get; set; } = true;


    /// <summary>
    /// Gets or sets whether credential provisioner is enabled.
    /// </summary>
    public bool RequireCredentialProvisioner { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the service is started after installation.
    /// </summary>
    public bool StartAfterInstall { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the system should wait for authenticated registration is enabled.
    /// </summary>
    public bool WaitForAuthenticatedRegistration { get; set; } = true;

    /// <summary>
    /// Gets or sets registration timeout.
    /// </summary>
    public TimeSpan RegistrationTimeout { get; set; } = TimeSpan.FromSeconds(30);

}
