using ApiKit.Management.Lifecycle;

namespace ApiKit.Management.Windows.Internal;

/// <summary>
/// Безопасно создаёт локальный snapshot пакета и размещает проверенную версию
/// внутри каталога, контролируемого management host.
/// </summary>
internal static class WindowsServicePackageStorage
{
    public static void ValidateManifest(
        ManagedServicePackageManifest manifest,
        string packageDirectory)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageDirectory);

        if (!Directory.Exists(packageDirectory))
        {
            throw new DirectoryNotFoundException(packageDirectory);
        }

        ValidatePathSegment(manifest.ServiceName, nameof(manifest.ServiceName));
        ValidatePathSegment(manifest.Version, nameof(manifest.Version));

        if (string.IsNullOrWhiteSpace(manifest.DisplayName))
        {
            throw new ArgumentException("DisplayName не может быть пустым.", nameof(manifest));
        }

        if (string.IsNullOrWhiteSpace(manifest.EntryPoint))
        {
            throw new ArgumentException("EntryPoint обязателен.", nameof(manifest));
        }

        ValidatePackageTree(packageDirectory);
    }

    public static string StagePackage(
        string installRoot,
        ManagedServicePackageManifest manifest,
        string packageDirectory)
    {
        var fullInstallRoot = Path.GetFullPath(installRoot);
        Directory.CreateDirectory(fullInstallRoot);

        var stagingRoot = GetContainedPath(fullInstallRoot, ".staging");
        Directory.CreateDirectory(stagingRoot);

        var stagingDirectory = GetContainedPath(
            stagingRoot,
            $"{manifest.ServiceName}-{Guid.NewGuid():N}");

        Directory.CreateDirectory(stagingDirectory);

        try
        {
            CopyDirectorySafely(packageDirectory, stagingDirectory);
            ValidatePackageTree(stagingDirectory);
            return stagingDirectory;
        }
        catch
        {
            DeleteDirectoryIfExists(stagingDirectory);
            throw;
        }
    }

    public static string CommitStagedPackage(
        string installRoot,
        ManagedServicePackageManifest manifest,
        string stagedDirectory)
    {
        var fullInstallRoot = Path.GetFullPath(installRoot);
        Directory.CreateDirectory(fullInstallRoot);

        var serviceDirectory = GetContainedPath(fullInstallRoot, manifest.ServiceName);
        var destination = GetContainedPath(serviceDirectory, manifest.Version);

        Directory.CreateDirectory(serviceDirectory);

        if (Directory.Exists(destination))
        {
            Directory.Delete(destination, recursive: true);
        }

        Directory.Move(stagedDirectory, destination);
        return destination;
    }

    public static string ResolveEntryPoint(
        ManagedServicePackageManifest manifest,
        string installDirectory)
    {
        var root = Path.GetFullPath(installDirectory);
        var entryPoint = Path.GetFullPath(Path.Combine(root, manifest.EntryPoint));

        EnsureContainedPath(root, entryPoint, "EntryPoint выходит за пределы каталога пакета.");

        if (!File.Exists(entryPoint))
        {
            throw new FileNotFoundException("EntryPoint устанавливаемого сервиса не найден.", entryPoint);
        }

        return entryPoint;
    }

    public static void DeleteDirectoryIfExists(string? directory)
    {
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void ValidatePathSegment(string value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);

        var trimmed = value.Trim();

        if (trimmed is "." or ".."
            || string.Equals(trimmed, ".staging", StringComparison.OrdinalIgnoreCase)
            || IsReservedWindowsDeviceName(trimmed)
            || trimmed.EndsWith('.')
            || trimmed.EndsWith(' ')
            || Path.IsPathRooted(trimmed)
            || trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || trimmed.Contains(Path.DirectorySeparatorChar)
            || trimmed.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException(
                $"{name} не может использоваться как безопасный сегмент пути.",
                name);
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

    private static string GetContainedPath(string root, string child)
    {
        var fullRoot = Path.GetFullPath(root);
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, child));
        EnsureContainedPath(fullRoot, fullPath, "Итоговый путь выходит за разрешённый корневой каталог.");
        return fullPath;
    }

    private static void EnsureContainedPath(string root, string path, string message)
    {
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var fullPath = Path.GetFullPath(path);

        if (string.Equals(fullRoot, fullPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var rootPrefix = fullRoot + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void ValidatePackageTree(string packageDirectory)
    {
        var root = new DirectoryInfo(Path.GetFullPath(packageDirectory));
        if ((root.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException("Каталог пакета не может быть reparse point.");
        }

        foreach (var entry in root.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException(
                    $"Пакет содержит reparse point и не может быть установлен: {entry.FullName}");
            }
        }
    }

    private static void CopyDirectorySafely(string sourceDirectory, string destinationDirectory)
    {
        var source = new DirectoryInfo(Path.GetFullPath(sourceDirectory));
        var destination = new DirectoryInfo(Path.GetFullPath(destinationDirectory));
        CopyDirectorySafely(source, destination);
    }

    private static void CopyDirectorySafely(DirectoryInfo source, DirectoryInfo destination)
    {
        if ((source.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException($"Каталог пакета не может быть reparse point: {source.FullName}");
        }

        destination.Create();

        foreach (var entry in source.EnumerateFileSystemInfos())
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException(
                    $"Пакет содержит reparse point и не может быть установлен: {entry.FullName}");
            }

            switch (entry)
            {
                case DirectoryInfo directory:
                    CopyDirectorySafely(
                        directory,
                        new DirectoryInfo(Path.Combine(destination.FullName, directory.Name)));
                    break;

                case FileInfo file:
                    file.CopyTo(Path.Combine(destination.FullName, file.Name), overwrite: true);
                    break;
            }
        }
    }
}
