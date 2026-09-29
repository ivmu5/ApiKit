using ApiKit.Management.Lifecycle;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Проверяет целостность и доверенность пакета до привилегированной установки сервиса.
/// </summary>
/// <remarks>
/// Конкретная реализация может использовать подпись пакета, хеши файлов и CryptoKit.
/// Установщик не должен считать пакет доверенным только потому, что он находится на локальном диске.
/// </remarks>
public interface IManagedServicePackageVerifier
{
    /// <summary>
    /// Проверяет пакет перед установкой.
    /// </summary>
    /// <param name="manifest">Манифест пакета.</param>
    /// <param name="packageDirectory">Каталог распакованного пакета.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns><see langword="true"/>, если пакет разрешено устанавливать.</returns>
    ValueTask<bool> VerifyAsync(
        ManagedServicePackageManifest manifest,
        string packageDirectory,
        CancellationToken cancellationToken = default);
}
