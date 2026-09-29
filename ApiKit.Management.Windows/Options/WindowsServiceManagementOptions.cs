namespace ApiKit.Management.Windows.Options;

/// <summary>
/// Настройки управления Windows Services через management host.
/// </summary>
public sealed class WindowsServiceManagementOptions
{
    /// <summary>
    /// Корневой каталог, в который устанавливаются управляемые сервисы.
    /// </summary>
    public string InstallRoot { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ApiKit",
        "Services");

    /// <summary>
    /// Учётная запись Windows, от имени которой запускаются устанавливаемые сервисы.
    /// </summary>
    /// <remarks>
    /// По умолчанию используется LocalService. Для более строгой изоляции рекомендуется
    /// задавать отдельные service identities при развёртывании.
    /// </remarks>
    public string ServiceAccount { get; set; } = @"NT AUTHORITY\LocalService";

    /// <summary>
    /// Максимальное время ожидания перехода Windows Service в требуемое состояние.
    /// </summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Требовать хотя бы один <see cref="ApiKit.Management.Abstractions.IManagedServicePackageVerifier"/>
    /// перед установкой или обновлением пакета.
    /// </summary>
    public bool RequirePackageVerifier { get; set; } = true;

    /// <summary>
    /// Запускать установленный сервис автоматически после установки.
    /// </summary>
    public bool StartAfterInstall { get; set; } = true;
}
