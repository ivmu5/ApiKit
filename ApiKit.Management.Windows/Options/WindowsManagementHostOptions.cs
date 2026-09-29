namespace ApiKit.Management.Windows.Options;

/// <summary>
/// Настройки локального Windows management host.
/// </summary>
public sealed class WindowsManagementHostOptions
{
    /// <summary>
    /// Стабильный security identifier management host, который Host криптографически доказывает сервисам и использует как issuer challenge.
    /// </summary>
    public string ManagementHostPeerId { get; set; } = "management-host";

    /// <summary>Pipe, через который микросервисы публикуют регистрацию и heartbeat.</summary>
    public string RegistrationPipeName { get; set; } = "ApiKit.Management.Registration";

    /// <summary>Pipe, через который админ-панель читает каталог и вызывает операции.</summary>
    public string AdministrationPipeName { get; set; } = "ApiKit.Management.Admin";

    /// <summary>Максимальный размер одного transport-сообщения.</summary>
    public int MaxMessageBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>Максимальное время подключения host к management pipe микросервиса.</summary>
    public TimeSpan ServiceConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>ACL pipe регистрации сервисов.</summary>
    public WindowsNamedPipeAccessOptions RegistrationPipeAccess { get; } = new();

    /// <summary>ACL административного pipe.</summary>
    public WindowsNamedPipeAccessOptions AdministrationPipeAccess { get; } = new();
}
