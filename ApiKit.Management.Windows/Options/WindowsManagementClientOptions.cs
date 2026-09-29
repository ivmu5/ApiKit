namespace ApiKit.Management.Windows.Options;

/// <summary>
/// Настройки Named Pipe клиента админ-панели.
/// </summary>
public sealed class WindowsManagementClientOptions
{
    /// <summary>
    /// Стабильный security identifier административного клиента.
    /// Должен иметь собственный credential в настроенном management security provider.
    /// </summary>
    public string AdminClientPeerId { get; set; } = "admin-client";

    /// <summary>
    /// Ожидаемый security identifier локального management host.
    /// Клиент проверяет его до отправки собственного proof и административного запроса.
    /// </summary>
    public string ManagementHostPeerId { get; set; } = "management-host";

    /// <summary>Pipe локального management host.</summary>
    public string AdministrationPipeName { get; set; } = "ApiKit.Management.Admin";

    /// <summary>Максимальное время подключения.</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Максимальный размер одного transport-сообщения.</summary>
    public int MaxMessageBytes { get; set; } = 4 * 1024 * 1024;
}
