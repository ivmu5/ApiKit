using ApiKit.Management.Lifecycle;
using ApiKit.Management.Provisioning;

namespace ApiKit.Management.Windows.Internal;

/// <summary>
/// Validates service package metadata and rejects unsafe package trees.
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

        // Validate the entire manifest before staging or changing SCM. Deserializers
        // can populate nullable values even when C# declares a property non-nullable.
        if (manifest.DisplayName.IndexOf('\0') >= 0 ||
            manifest.Arguments is null ||
            manifest.Arguments.Any(static argument => argument is null || argument.IndexOf('\0') >= 0) ||
            manifest.Dependencies is null ||
            manifest.Metadata is null ||
            manifest.Metadata.Any(static item => string.IsNullOrWhiteSpace(item.Key) || item.Value is null || item.Value.IndexOf('\0') >= 0))
        {
            throw new ArgumentException("The service manifest contains invalid fields.", nameof(manifest));
        }

        foreach (var dependency in manifest.Dependencies)
        {
            // sc.exe uses '/' to separate dependencies, so reject delimiters
            // inside a single dependency name to preserve manifest semantics.
            if (string.IsNullOrWhiteSpace(dependency) ||
                dependency.Length > 256 ||
                dependency.IndexOfAny(new[] { '/', '\\', '\0', '\r', '\n' }) >= 0)
            {
                throw new ArgumentException("A service dependency name is invalid.", nameof(manifest));
            }
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
        EnsureExistingDirectoryNotReparsePoint(fullInstallRoot);

        var stagingRoot = GetContainedPath(fullInstallRoot, ".staging");
        Directory.CreateDirectory(stagingRoot);
        EnsureExistingDirectoryNotReparsePoint(stagingRoot);

        var stagingDirectory = GetContainedPath(
            stagingRoot,
            $"{manifest.ServiceName}-{Guid.NewGuid():N}");

        // A package source may be an installed version, but it must not contain
        // the staging destination: copying the install root into itself would recurse.
        if (IsSameOrContainedPath(Path.GetFullPath(packageDirectory), stagingDirectory))
        {
            throw new InvalidOperationException(
                "The package source cannot contain the destination staging directory.");
        }

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
        EnsureExistingDirectoryNotReparsePoint(fullInstallRoot);

        var serviceDirectory = GetContainedPath(fullInstallRoot, manifest.ServiceName);
        var destination = GetContainedPath(serviceDirectory, manifest.Version);
        var stagingRoot = GetContainedPath(fullInstallRoot, ".staging");
        if (!IsSameOrContainedPath(stagingRoot, stagedDirectory) ||
            string.Equals(Path.GetFullPath(stagedDirectory), stagingRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The staged package must be a child of the install root's staging directory.");
        }

        // Recheck the snapshot before moving it; the source package may have
        // been changed between the first validation and the final commit.
        ValidatePackageTree(stagedDirectory);
        Directory.CreateDirectory(serviceDirectory);
        EnsureExistingDirectoryNotReparsePoint(serviceDirectory);

        if (Directory.Exists(destination) || File.Exists(destination))
        {
            throw new IOException($"Версия уже существует: {destination}. Перед обновлением сохраните rollback snapshot.");
        }

        Directory.Move(stagedDirectory, destination);
        return destination;
    }

    /// <summary>
    /// Moves the existing package version to an isolated rollback snapshot.
    /// </summary>
    internal static string? BackupVersionForUpdate(string installRoot, ManagedServicePackageManifest manifest)
    {
        var serviceDirectory = GetServiceDirectory(installRoot, manifest.ServiceName);
        ValidatePathSegment(manifest.Version, nameof(manifest.Version));
        var existing = GetContainedPath(serviceDirectory, manifest.Version);
        if (!Directory.Exists(existing))
        {
            return null;
        }

        var source = new DirectoryInfo(existing);
        ValidateDirectoryTreeForDeletion(source);
        var staging = GetContainedPath(Path.GetFullPath(installRoot), ".staging");
        Directory.CreateDirectory(staging);
        EnsureExistingDirectoryNotReparsePoint(staging);
        var backup = GetContainedPath(staging, $"rollback-{Guid.NewGuid():N}");
        Directory.Move(existing, backup);
        return backup;
    }

    /// <summary>
    /// Restores the original package version from its validated rollback snapshot.
    /// </summary>
    internal static void RestoreVersionAfterFailedUpdate(
        string installRoot,
        ManagedServicePackageManifest manifest,
        string? backup)
    {
        var serviceDirectory = GetServiceDirectory(installRoot, manifest.ServiceName);
        ValidatePathSegment(manifest.Version, nameof(manifest.Version));
        var destination = GetContainedPath(serviceDirectory, manifest.Version);

        if (backup is null)
        {
            DeleteDirectoryIfExists(destination);
            return;
        }

        if (!Directory.Exists(backup))
        {
            throw new DirectoryNotFoundException($"Rollback snapshot недоступен: {backup}");
        }

        var staging = GetContainedPath(Path.GetFullPath(installRoot), ".staging");
        var fullBackup = Path.GetFullPath(backup);
        EnsureContainedPath(staging, fullBackup, "Rollback snapshot выходит за staging directory.");
        var snapshot = new DirectoryInfo(fullBackup);
        ValidateDirectoryTreeForDeletion(snapshot);
        DeleteDirectoryIfExists(destination);
        Directory.Move(fullBackup, destination);
    }

    public static string GetServiceDirectory(
        string installRoot,
        string serviceName)
    {
        ValidatePathSegment(serviceName, nameof(serviceName));

        var fullInstallRoot = Path.GetFullPath(installRoot);
        Directory.CreateDirectory(fullInstallRoot);
        EnsureExistingDirectoryNotReparsePoint(fullInstallRoot);

        var serviceDirectory = GetContainedPath(fullInstallRoot, serviceName);
        EnsureExistingDirectoryNotReparsePoint(serviceDirectory);
        return serviceDirectory;
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
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        var root = new DirectoryInfo(Path.GetFullPath(directory));
        ValidateDirectoryTreeForDeletion(root);
        Directory.Delete(root.FullName, recursive: true);
    }

    public static void TryDeleteDirectoryIfEmpty(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        var root = new DirectoryInfo(Path.GetFullPath(directory));
        if ((root.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException(
                $"Каталог не может быть удалён как управляемый service directory, потому что является reparse point: {root.FullName}");
        }

        if (!root.EnumerateFileSystemInfos().Any())
        {
            root.Delete();
        }
    }

    private static void EnsureExistingDirectoryNotReparsePoint(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        var attributes = File.GetAttributes(directory);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException(
                $"Управляемый каталог не может быть reparse point: {directory}");
        }
    }

    private static void ValidateDirectoryTreeForDeletion(DirectoryInfo directory)
    {
        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException(
                $"Управляемый каталог не может быть reparse point: {directory.FullName}");
        }

        foreach (var entry in directory.EnumerateFileSystemInfos())
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException(
                    $"Управляемый каталог содержит reparse point и не может быть рекурсивно удалён: {entry.FullName}");
            }

            if (entry is DirectoryInfo childDirectory)
            {
                ValidateDirectoryTreeForDeletion(childDirectory);
            }
        }
    }

    private static void ValidatePathSegment(string value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);

        var trimmed = value.Trim();

        if (!StringComparer.Ordinal.Equals(value, trimmed)
            || trimmed.Length > 128
            || trimmed is "." or ".."
            || string.Equals(trimmed, ".staging", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, ManagedServiceProvisioningDefaults.StateDirectoryName, StringComparison.OrdinalIgnoreCase)
            || IsReservedWindowsDeviceName(trimmed)
            || trimmed.EndsWith('.')
            || trimmed.EndsWith(' ')
            || trimmed.IndexOfAny(new[] { '\0', '\r', '\n' }) >= 0
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
            || baseName.Equals("NUL", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("CONIN$", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("CONOUT$", StringComparison.OrdinalIgnoreCase))
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

    private static bool IsSameOrContainedPath(string root, string path)
    {
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var fullPath = Path.GetFullPath(path);
        var rootPrefix = Path.EndsInDirectorySeparator(fullRoot)
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;
        return string.Equals(fullRoot, fullPath, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureContainedPath(string root, string path, string message)
    {
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var fullPath = Path.GetFullPath(path);

        if (string.Equals(fullRoot, fullPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var rootPrefix = Path.EndsInDirectorySeparator(fullRoot)
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void ValidatePackageTree(string packageDirectory)
    {
        var root = new DirectoryInfo(Path.GetFullPath(packageDirectory));
        ValidatePackageDirectory(root);
    }

    private static void ValidatePackageDirectory(DirectoryInfo directory)
    {
        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException(
                $"Каталог пакета не может быть reparse point: {directory.FullName}");
        }

        foreach (var entry in directory.EnumerateFileSystemInfos())
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException(
                    $"Пакет содержит reparse point и не может быть установлен: {entry.FullName}");
            }

            if (entry is DirectoryInfo childDirectory)
            {
                if (string.Equals(
                        childDirectory.Name,
                        ManagedServiceProvisioningDefaults.StateDirectoryName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Пакет использует зарезервированный management-каталог '{ManagedServiceProvisioningDefaults.StateDirectoryName}': {entry.FullName}");
                }

                ValidatePackageDirectory(childDirectory);
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
