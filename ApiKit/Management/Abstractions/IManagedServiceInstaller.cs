using ApiKit.Management.Lifecycle;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Абстракция привилегированного management host для установки, обновления и удаления сервисов.
/// </summary>
public interface IManagedServiceInstaller
{
    /// <summary>Устанавливает сервис из подготовленного каталога пакета.</summary>
    ValueTask InstallAsync(
        ManagedServicePackageManifest manifest,
        string packageDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>Обновляет ранее установленный сервис новым пакетом.</summary>
    ValueTask UpdateAsync(
        ManagedServicePackageManifest manifest,
        string packageDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>Удаляет установленный сервис.</summary>
    ValueTask UninstallAsync(
        string serviceName,
        CancellationToken cancellationToken = default);
}
