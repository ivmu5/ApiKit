namespace ApiKit.Management.Windows;

/// <summary>
/// Defines standard values for windows management transport defaults.
/// </summary>
public static class WindowsManagementTransportDefaults
{
    /// <summary>
    /// Gets or sets transport name.
    /// </summary>
    public const string TransportName = "named-pipe";

    /// <summary>
    /// Gets or sets registration purpose.
    /// </summary>
    public const string RegistrationPurpose = "registration";

    /// <summary>
    /// Gets or sets administration purpose.
    /// </summary>
    public const string AdministrationPurpose = "administration";

    /// <summary>
    /// Gets or sets management purpose.
    /// </summary>
    public const string ManagementPurpose = "management";
}
