namespace ApiKit.Management.Windows.Options;

/// <summary>
/// Defines configuration settings for windows named pipe access options.
/// </summary>
public sealed class WindowsNamedPipeAccessOptions
{
    private readonly HashSet<string> _allowedAccounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _allowedSids = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets whether current user only is enabled.
    /// </summary>
    public bool CurrentUserOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets allowed accounts.
    /// </summary>
    public IReadOnlyCollection<string> AllowedAccounts => _allowedAccounts;

    /// <summary>
    /// Gets or sets allowed SIDs.
    /// </summary>
    public IReadOnlyCollection<string> AllowedSids => _allowedSids;

    /// <summary>
    /// Grants the specified Windows account access to the Named Pipe.
    /// </summary>
    public WindowsNamedPipeAccessOptions AllowAccount(string accountName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        _allowedAccounts.Add(accountName.Trim());
        return this;
    }


    /// <summary>
    /// Grants the specified Windows security identifier access to the Named Pipe.
    /// </summary>
    public WindowsNamedPipeAccessOptions AllowSid(string sid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sid);
        _ = new System.Security.Principal.SecurityIdentifier(sid);
        _allowedSids.Add(sid.Trim());
        return this;
    }

    /// <summary>
    /// Permits members of the built-in Windows Administrators group to connect.
    /// </summary>
    public WindowsNamedPipeAccessOptions AllowBuiltInAdministrators() =>
        AllowAccount(@"BUILTIN\Administrators");

    /// <summary>
    /// Permits the Windows LocalSystem identity to connect.
    /// </summary>
    public WindowsNamedPipeAccessOptions AllowLocalSystem() =>
        AllowAccount(@"NT AUTHORITY\SYSTEM");
}
