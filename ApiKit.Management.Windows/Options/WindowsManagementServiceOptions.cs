namespace ApiKit.Management.Windows.Options;

/// <summary>
/// Настройки Named Pipe транспорта управляемого микросервиса.
/// </summary>
public sealed class WindowsManagementServiceOptions
{
    /// <summary>
    /// Ожидаемый security identifier management host. Сервис принимает management-соединения
    /// и регистрацию только после криптографической проверки этой identity.
    /// </summary>
    public string ManagementHostPeerId { get; set; } = "management-host";

    /// <summary>Pipe management host, принимающий регистрации и heartbeat.</summary>
    public string RegistrationPipeName { get; set; } = "ApiKit.Management.Registration";

    /// <summary>Префикс pipe, на котором сервис принимает management-запросы.</summary>
    public string ManagementPipePrefix { get; set; } = "ApiKit.Management.Service";

    /// <summary>Максимальный размер одного transport-сообщения.</summary>
    public int MaxMessageBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>Максимальное время подключения к management host.</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>ACL management pipe текущего сервиса.</summary>
    public WindowsNamedPipeAccessOptions ManagementPipeAccess { get; } = new();
}
