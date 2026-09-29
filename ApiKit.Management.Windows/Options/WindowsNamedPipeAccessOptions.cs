namespace ApiKit.Management.Windows.Options;

/// <summary>
/// Настраивает Windows ACL для создаваемого Named Pipe.
/// </summary>
public sealed class WindowsNamedPipeAccessOptions
{
    private readonly HashSet<string> _allowedAccounts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Ограничивать pipe только текущей Windows identity.
    /// </summary>
    /// <remarks>
    /// Это удобный default для разработки, но он не изолирует pipe от другого процесса,
    /// уже работающего под той же Windows identity. Для production и Windows Services
    /// под разными учётными записями рекомендуется отключить режим и задать явный ACL.
    /// </remarks>
    public bool CurrentUserOnly { get; set; } = true;

    /// <summary>Явно разрешённые Windows accounts при <see cref="CurrentUserOnly"/> = <see langword="false"/>.</summary>
    public IReadOnlyCollection<string> AllowedAccounts => _allowedAccounts;

    /// <summary>Разрешает указанную Windows identity или группу.</summary>
    public WindowsNamedPipeAccessOptions AllowAccount(string accountName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        _allowedAccounts.Add(accountName.Trim());
        return this;
    }

    /// <summary>Разрешает встроенную локальную группу Administrators.</summary>
    public WindowsNamedPipeAccessOptions AllowBuiltInAdministrators() =>
        AllowAccount(@"BUILTIN\Administrators");

    /// <summary>Разрешает LocalSystem.</summary>
    public WindowsNamedPipeAccessOptions AllowLocalSystem() =>
        AllowAccount(@"NT AUTHORITY\SYSTEM");
}
