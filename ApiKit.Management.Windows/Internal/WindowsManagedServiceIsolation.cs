using System.Security.AccessControl;
using System.Security.Principal;
using ApiKit.Management.Provisioning;
using ApiKit.Management.Windows.Options;

namespace ApiKit.Management.Windows.Internal;

/// <summary>
/// Configures unique Windows service SIDs and restrictive ACLs for managed package and credential directories.
/// </summary>
internal static class WindowsManagedServiceIsolation
{
    private const FileSystemRights ServicePackageRights = FileSystemRights.ReadAndExecute;
    private const FileSystemRights ServiceStateRights = FileSystemRights.Modify | FileSystemRights.Synchronize;

    public static void ProtectInstallRoot(WindowsServiceManagementOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.ProtectManagedDirectories)
        {
            return;
        }

        ApplyInstallRootAcl(options.InstallRoot, WindowsIsolationIdentity.GetCurrentIsolationSid(), options);
    }

    public static WindowsManagedServiceIsolationState Provision(
        string serviceName,
        string serviceRootDirectory,
        string installDirectory,
        WindowsServiceManagementOptions options,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceRootDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(installDirectory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var serviceSid = ResolveAccountSid($@"NT SERVICE\{serviceName}");
        var hostSid = WindowsIsolationIdentity.GetCurrentIsolationSid();

        if (options.ProtectManagedDirectories)
        {
            ApplyInstallRootAcl(options.InstallRoot, hostSid, options);
            ApplyServiceRootAcl(serviceRootDirectory, serviceSid, hostSid, options);
            ApplyPackageTreeAcl(installDirectory, serviceSid, hostSid, options);

            var managementDirectory = Path.Combine(
                serviceRootDirectory,
                ManagedServiceProvisioningDefaults.StateDirectoryName);

            if (Directory.Exists(managementDirectory))
            {
                ApplyStateTreeAcl(managementDirectory, serviceSid, hostSid, options);
            }
        }

        var state = new WindowsManagedServiceIsolationState
        {
            Version = WindowsManagedServiceIsolationState.CurrentVersion,
            ServiceName = serviceName,
            ServiceSid = serviceSid.Value,
            ManagementHostSid = hostSid.Value,
            CreatedAtUtc = timeProvider.GetUtcNow()
        };

        WindowsManagedServiceIsolationState.Save(serviceRootDirectory, state);

        if (options.ProtectManagedDirectories)
        {
            ApplyIsolationMetadataAcl(serviceRootDirectory, serviceSid, hostSid, options);
        }

        return state;
    }

    public static void ApplyProvisionedManagementPipeAccess(
        WindowsManagementServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.UseProvisionedWindowsIsolation)
        {
            return;
        }

        var state = WindowsManagedServiceIsolationState.TryLoadForInstalledApplication();
        if (state is null)
        {
            return;
        }

        options.ManagementPipeAccess.CurrentUserOnly = false;
        options.ManagementPipeAccess.AllowSid(state.ManagementHostSid);
    }

    private static void ApplyInstallRootAcl(
        string installRoot,
        SecurityIdentifier hostSid,
        WindowsServiceManagementOptions options)
    {
        Directory.CreateDirectory(installRoot);
        var security = CreateDirectorySecurity(hostSid, serviceSid: null, serviceRights: 0, options);
        SetDirectorySecurity(installRoot, security);
    }

    private static void ApplyServiceRootAcl(
        string serviceRootDirectory,
        SecurityIdentifier serviceSid,
        SecurityIdentifier hostSid,
        WindowsServiceManagementOptions options)
    {
        Directory.CreateDirectory(serviceRootDirectory);
        var security = CreateDirectorySecurity(
            hostSid,
            serviceSid,
            ServicePackageRights,
            options);
        SetDirectorySecurity(serviceRootDirectory, security);
    }

    private static void ApplyPackageTreeAcl(
        string rootDirectory,
        SecurityIdentifier serviceSid,
        SecurityIdentifier hostSid,
        WindowsServiceManagementOptions options)
    {
        ValidateTreeHasNoReparsePoints(new DirectoryInfo(rootDirectory));

        foreach (var directory in Directory.EnumerateDirectories(rootDirectory, "*", SearchOption.AllDirectories))
        {
            SetDirectorySecurity(
                directory,
                CreateDirectorySecurity(hostSid, serviceSid, ServicePackageRights, options));
        }

        foreach (var file in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories))
        {
            SetFileSecurity(
                file,
                CreateFileSecurity(hostSid, serviceSid, ServicePackageRights, options));
        }

        SetDirectorySecurity(
            rootDirectory,
            CreateDirectorySecurity(hostSid, serviceSid, ServicePackageRights, options));
    }

    private static void ApplyStateTreeAcl(
        string rootDirectory,
        SecurityIdentifier serviceSid,
        SecurityIdentifier hostSid,
        WindowsServiceManagementOptions options)
    {
        ValidateTreeHasNoReparsePoints(new DirectoryInfo(rootDirectory));

        SetDirectorySecurity(
            rootDirectory,
            CreateDirectorySecurity(hostSid, serviceSid, ServicePackageRights, options));

        foreach (var file in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            SetFileSecurity(
                file,
                CreateFileSecurity(hostSid, serviceSid, ServiceStateRights, options));
        }

        foreach (var childDirectory in Directory.EnumerateDirectories(rootDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            if (string.Equals(
                    Path.GetFileName(childDirectory),
                    WindowsManagedServiceIsolationState.DirectoryName,
                    StringComparison.OrdinalIgnoreCase))
            {
                ApplyReadOnlyStateSubtreeAcl(childDirectory, serviceSid, hostSid, options);
                continue;
            }

            ApplyWritableStateSubtreeAcl(childDirectory, serviceSid, hostSid, options);
        }
    }

    private static void ApplyWritableStateSubtreeAcl(
        string rootDirectory,
        SecurityIdentifier serviceSid,
        SecurityIdentifier hostSid,
        WindowsServiceManagementOptions options)
    {
        foreach (var directory in Directory.EnumerateDirectories(rootDirectory, "*", SearchOption.AllDirectories))
        {
            SetDirectorySecurity(
                directory,
                CreateDirectorySecurity(hostSid, serviceSid, ServiceStateRights, options));
        }

        foreach (var file in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories))
        {
            SetFileSecurity(
                file,
                CreateFileSecurity(hostSid, serviceSid, ServiceStateRights, options));
        }

        SetDirectorySecurity(
            rootDirectory,
            CreateDirectorySecurity(hostSid, serviceSid, ServiceStateRights, options));
    }

    private static void ApplyReadOnlyStateSubtreeAcl(
        string rootDirectory,
        SecurityIdentifier serviceSid,
        SecurityIdentifier hostSid,
        WindowsServiceManagementOptions options)
    {
        foreach (var directory in Directory.EnumerateDirectories(rootDirectory, "*", SearchOption.AllDirectories))
        {
            SetDirectorySecurity(
                directory,
                CreateDirectorySecurity(hostSid, serviceSid, ServicePackageRights, options));
        }

        foreach (var file in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories))
        {
            SetFileSecurity(
                file,
                CreateFileSecurity(hostSid, serviceSid, FileSystemRights.Read, options));
        }

        SetDirectorySecurity(
            rootDirectory,
            CreateDirectorySecurity(hostSid, serviceSid, ServicePackageRights, options));
    }

    private static void ApplyIsolationMetadataAcl(
        string serviceRootDirectory,
        SecurityIdentifier serviceSid,
        SecurityIdentifier hostSid,
        WindowsServiceManagementOptions options)
    {
        var stateDirectory = WindowsManagedServiceIsolationState.GetStateDirectory(serviceRootDirectory);
        if (!Directory.Exists(stateDirectory))
        {
            return;
        }

        var readRights = FileSystemRights.ReadAndExecute;
        SetDirectorySecurity(
            stateDirectory,
            CreateDirectorySecurity(hostSid, serviceSid, readRights, options));

        var statePath = WindowsManagedServiceIsolationState.GetStatePath(serviceRootDirectory);
        if (File.Exists(statePath))
        {
            SetFileSecurity(
                statePath,
                CreateFileSecurity(hostSid, serviceSid, FileSystemRights.Read, options));
        }
    }

    private static DirectorySecurity CreateDirectorySecurity(
        SecurityIdentifier hostSid,
        SecurityIdentifier? serviceSid,
        FileSystemRights serviceRights,
        WindowsServiceManagementOptions options)
    {
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        AddDirectoryRule(security, hostSid, FileSystemRights.FullControl);

        if (options.AllowLocalSystemFullControl)
        {
            AddDirectoryRule(security, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), FileSystemRights.FullControl);
        }

        if (options.AllowBuiltInAdministratorsFullControl)
        {
            AddDirectoryRule(security, new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), FileSystemRights.FullControl);
        }

        if (serviceSid is not null)
        {
            AddDirectoryRule(security, serviceSid, serviceRights);
        }

        return security;
    }

    private static FileSecurity CreateFileSecurity(
        SecurityIdentifier hostSid,
        SecurityIdentifier? serviceSid,
        FileSystemRights serviceRights,
        WindowsServiceManagementOptions options)
    {
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        security.AddAccessRule(new FileSystemAccessRule(hostSid, FileSystemRights.FullControl, AccessControlType.Allow));

        if (options.AllowLocalSystemFullControl)
        {
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                FileSystemRights.FullControl,
                AccessControlType.Allow));
        }

        if (options.AllowBuiltInAdministratorsFullControl)
        {
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                FileSystemRights.FullControl,
                AccessControlType.Allow));
        }

        if (serviceSid is not null)
        {
            security.AddAccessRule(new FileSystemAccessRule(serviceSid, serviceRights, AccessControlType.Allow));
        }

        return security;
    }

    private static void AddDirectoryRule(
        DirectorySecurity security,
        SecurityIdentifier sid,
        FileSystemRights rights)
    {
        security.AddAccessRule(new FileSystemAccessRule(
            sid,
            rights,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));
    }

    private static void SetDirectorySecurity(string path, DirectorySecurity security)
    {
        FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(path), security);
    }

    private static void SetFileSecurity(string path, FileSecurity security)
    {
        FileSystemAclExtensions.SetAccessControl(new FileInfo(path), security);
    }

    private static SecurityIdentifier ResolveAccountSid(string accountName)
    {
        var account = new NTAccount(accountName);
        return (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
    }

    private static void ValidateTreeHasNoReparsePoints(DirectoryInfo directory)
    {
        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException(
                $"Windows ACL не применяется к reparse point: {directory.FullName}");
        }

        foreach (var entry in directory.EnumerateFileSystemInfos())
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException(
                    $"Windows ACL не применяется к дереву с reparse point: {entry.FullName}");
            }

            if (entry is DirectoryInfo childDirectory)
            {
                ValidateTreeHasNoReparsePoints(childDirectory);
            }
        }
    }
}
