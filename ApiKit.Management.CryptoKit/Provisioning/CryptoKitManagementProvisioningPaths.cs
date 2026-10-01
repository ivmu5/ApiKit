using ApiKit.Management.Provisioning;

namespace ApiKit.Management.CryptoKit.Provisioning;

/// <summary>
/// Validates and resolves isolated service credential storage paths.
/// </summary>
public static class CryptoKitManagementProvisioningPaths
{
    /// <summary>
    /// Returns storage directory.
    /// </summary>
    public static string GetStorageDirectory(
        string serviceRootDirectory,
        string cryptoKitDirectoryName = "cryptokit")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceRootDirectory);
        ValidateDirectoryName(cryptoKitDirectoryName);

        var serviceRoot = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(serviceRootDirectory));

        return Path.GetFullPath(
            Path.Combine(
                serviceRoot,
                ManagedServiceProvisioningDefaults.StateDirectoryName,
                cryptoKitDirectoryName));
    }

    /// <summary>
    /// Returns storage directory for installed application.
    /// </summary>
    public static string GetStorageDirectoryForInstalledApplication(
        string? applicationBaseDirectory = null,
        string cryptoKitDirectoryName = "cryptokit")
    {
        ValidateDirectoryName(cryptoKitDirectoryName);

        DirectoryInfo? current = new DirectoryInfo(
            Path.GetFullPath(applicationBaseDirectory ?? AppContext.BaseDirectory));

        for (var depth = 0; current is not null && depth < 16; depth++, current = current.Parent)
        {
            var candidate = GetStorageDirectory(current.FullName, cryptoKitDirectoryName);
            if (Directory.Exists(candidate))
            {
                EnsureSafeProvisioningDirectory(current.FullName, candidate);
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "Не найден provisioned CryptoKit management storage. " +
            "Убедитесь, что сервис установлен через ApiKit installer и security provisioning завершён.");
    }

    internal static void EnsureSafeProvisioningDirectory(
        string serviceRootDirectory,
        string storageDirectory)
    {
        var serviceRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(serviceRootDirectory));
        var storage = Path.GetFullPath(storageDirectory);
        var rootPrefix = serviceRoot + Path.DirectorySeparatorChar;

        if (!storage.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "CryptoKit provisioning storage выходит за пределы каталога управляемого сервиса.");
        }

        EnsureNotReparsePoint(serviceRoot);

        var relative = Path.GetRelativePath(serviceRoot, storage);
        var current = serviceRoot;

        foreach (var segment in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (Directory.Exists(current))
            {
                EnsureNotReparsePoint(current);
            }
        }
    }

    internal static void DeleteStorageDirectorySafely(
        string serviceRootDirectory,
        string storageDirectory)
    {
        if (!Directory.Exists(storageDirectory))
        {
            return;
        }

        EnsureSafeProvisioningDirectory(serviceRootDirectory, storageDirectory);

        var root = new DirectoryInfo(storageDirectory);
        ValidateDirectoryTreeWithoutReparsePoints(root);

        Directory.Delete(storageDirectory, recursive: true);

        var managementDirectory = Directory.GetParent(storageDirectory)?.FullName;
        if (!string.IsNullOrWhiteSpace(managementDirectory)
            && Directory.Exists(managementDirectory)
            && !Directory.EnumerateFileSystemEntries(managementDirectory).Any())
        {
            Directory.Delete(managementDirectory);
        }
    }


    private static void ValidateDirectoryTreeWithoutReparsePoints(DirectoryInfo directory)
    {
        foreach (var entry in directory.EnumerateFileSystemInfos())
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException(
                    $"Management security storage содержит reparse point: {entry.FullName}");
            }

            if (entry is DirectoryInfo childDirectory)
            {
                ValidateDirectoryTreeWithoutReparsePoints(childDirectory);
            }
        }
    }

    private static void EnsureNotReparsePoint(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        var attributes = File.GetAttributes(directory);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException(
                $"Management security storage не может проходить через reparse point: {directory}");
        }
    }

    private static bool IsReservedWindowsDeviceName(string value)
    {
        var baseName = value.Split('.', 2)[0];

        if (baseName.Equals("CON", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("PRN", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("AUX", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("NUL", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return baseName.Length == 4
            && (baseName.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                || baseName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
            && baseName[3] is >= '1' and <= '9';
    }

    internal static void ValidateDirectoryName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();

        if (!StringComparer.Ordinal.Equals(value, trimmed)
            || value is "." or ".."
            || IsReservedWindowsDeviceName(value)
            || value.EndsWith('.')
            || value.EndsWith(' ')
            || Path.IsPathRooted(value)
            || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || value.Contains(Path.DirectorySeparatorChar)
            || value.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException(
                "Имя каталога CryptoKit должно быть одним безопасным сегментом пути.",
                nameof(value));
        }
    }
}
