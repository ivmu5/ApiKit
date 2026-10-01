using System.Security.Principal;
using System.Text.Json;
using ApiKit.Management.Provisioning;

namespace ApiKit.Management.Windows.Internal;

/// <summary>
/// Stores durable Windows service SID and management host identity metadata.
/// </summary>
internal sealed record WindowsManagedServiceIsolationState
{
    public const int CurrentVersion = 1;
    public const string DirectoryName = "windows";
    public const string FileName = "isolation.v1.json";

    public required int Version { get; init; }
    public required string ServiceName { get; init; }
    public required string ServiceSid { get; init; }
    public required string ManagementHostSid { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }

    public static string GetStateDirectory(string serviceRootDirectory) =>
        Path.Combine(
            Path.GetFullPath(serviceRootDirectory),
            ManagedServiceProvisioningDefaults.StateDirectoryName,
            DirectoryName);

    public static string GetStatePath(string serviceRootDirectory) =>
        Path.Combine(GetStateDirectory(serviceRootDirectory), FileName);

    public static WindowsManagedServiceIsolationState? TryLoad(string serviceRootDirectory)
    {
        var path = GetStatePath(serviceRootDirectory);
        if (!File.Exists(path))
        {
            return null;
        }

        EnsurePathIsSafe(serviceRootDirectory, path);

        var json = File.ReadAllText(path);
        var state = JsonSerializer.Deserialize<WindowsManagedServiceIsolationState>(json)
            ?? throw new InvalidOperationException($"Windows isolation state повреждён: {path}");

        Validate(state);
        return state;
    }

    public static WindowsManagedServiceIsolationState? TryLoadForInstalledApplication(
        string? applicationBaseDirectory = null)
    {
        DirectoryInfo? current = new DirectoryInfo(
            Path.GetFullPath(applicationBaseDirectory ?? AppContext.BaseDirectory));

        for (var depth = 0; current is not null && depth < 16; depth++, current = current.Parent)
        {
            var state = TryLoad(current.FullName);
            if (state is not null)
            {
                return state;
            }
        }

        return null;
    }

    public static void Save(
        string serviceRootDirectory,
        WindowsManagedServiceIsolationState state)
    {
        Validate(state);

        var directory = GetStateDirectory(serviceRootDirectory);
        var path = GetStatePath(serviceRootDirectory);
        Directory.CreateDirectory(directory);
        EnsurePathIsSafe(serviceRootDirectory, path);

        var temporaryPath = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public static void Delete(string serviceRootDirectory)
    {
        var directory = GetStateDirectory(serviceRootDirectory);
        var path = GetStatePath(serviceRootDirectory);

        if (File.Exists(path))
        {
            EnsurePathIsSafe(serviceRootDirectory, path);
            File.Delete(path);
        }

        if (Directory.Exists(directory)
            && !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }

        var managementDirectory = Directory.GetParent(directory)?.FullName;
        if (!string.IsNullOrWhiteSpace(managementDirectory)
            && Directory.Exists(managementDirectory)
            && !Directory.EnumerateFileSystemEntries(managementDirectory).Any())
        {
            Directory.Delete(managementDirectory);
        }
    }

    private static void Validate(WindowsManagedServiceIsolationState state)
    {
        if (state.Version != CurrentVersion)
        {
            throw new InvalidOperationException(
                $"Версия Windows isolation state {state.Version} не поддерживается.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(state.ServiceName);
        var serviceSid = new SecurityIdentifier(state.ServiceSid);
        if (!serviceSid.Value.StartsWith("S-1-5-80-", StringComparison.OrdinalIgnoreCase)
            || serviceSid.Value.Equals("S-1-5-80-0", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("ServiceSid должен быть уникальным Windows service SID.", nameof(state));
        }

        _ = new SecurityIdentifier(state.ManagementHostSid);
    }

    private static void EnsurePathIsSafe(string serviceRootDirectory, string targetPath)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(serviceRootDirectory));
        var target = Path.GetFullPath(targetPath);
        var rootPrefix = root + Path.DirectorySeparatorChar;

        if (!target.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Windows isolation state выходит за пределы каталога управляемого сервиса.");
        }

        var relative = Path.GetRelativePath(root, target);
        var current = root;

        foreach (var segment in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);

            if (!Directory.Exists(current) && !File.Exists(current))
            {
                continue;
            }

            var attributes = File.GetAttributes(current);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException(
                    $"Windows isolation state не может проходить через reparse point: {current}");
            }
        }
    }
}
