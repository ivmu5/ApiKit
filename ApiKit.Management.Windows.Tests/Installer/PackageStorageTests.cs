using ApiKit.Management.Lifecycle;
using ApiKit.Management.Windows.Internal;

namespace ApiKit.Management.Windows.Tests.Installer;

public sealed class PackageStorageTests
{
    [Theory]
    [InlineData("..", "1.0")]
    [InlineData(".staging", "1.0")]
    [InlineData("service/a", "1.0")]
    [InlineData("CON", "1.0")]
    [InlineData("service", "../outside")]
    [InlineData("service", ".management")]
    public void Manifest_rejects_unsafe_service_or_version_names(string name, string version)
    {
        using var dirs = new Folders();
        Assert.Throws<ArgumentException>(() => WindowsServicePackageStorage.ValidateManifest(
            Manifest(name, version), dirs.Package));
    }

    [Fact]
    public void Stage_rejects_a_source_containing_the_destination_staging_tree()
    {
        using var dirs = new Folders();
        Assert.Throws<InvalidOperationException>(() => WindowsServicePackageStorage.StagePackage(
            dirs.Root, Manifest(), dirs.Root));
    }

    [Fact]
    public void Commit_rejects_an_arbitrary_non_staging_directory()
    {
        using var dirs = new Folders();
        Assert.Throws<InvalidOperationException>(() => WindowsServicePackageStorage.CommitStagedPackage(
            dirs.Root, Manifest(), dirs.Package));
    }

    [Theory]
    [InlineData("CONIN$")]
    [InlineData("CONOUT$")]
    [InlineData(" ")]
    public void Manifest_rejects_special_or_blank_service_names(string name)
    {
        using var dirs = new Folders();
        Assert.Throws<ArgumentException>(() =>
            WindowsServicePackageStorage.ValidateManifest(Manifest(name: name), dirs.Package));
    }

    [Fact]
    public void Manifest_rejects_injected_dependency_separators_before_staging()
    {
        using var dirs = new Folders();
        var manifest = Manifest() with { Dependencies = ["trusted/attacker"] };
        Assert.Throws<ArgumentException>(() =>
            WindowsServicePackageStorage.ValidateManifest(manifest, dirs.Package));
    }

    [Fact]
    public void Manifest_rejects_missing_collections_from_untrusted_json()
    {
        using var dirs = new Folders();
        var manifest = Manifest() with { Arguments = null! };
        Assert.Throws<ArgumentException>(() =>
            WindowsServicePackageStorage.ValidateManifest(manifest, dirs.Package));
    }

    [Fact]
    public void Stage_takes_snapshot_instead_of_using_mutable_package_source()
    {
        using var dirs = new Folders();
        var original = Path.Combine(dirs.Package, "service.exe");
        File.WriteAllText(original, "signed-original");
        var manifest = Manifest();
        WindowsServicePackageStorage.ValidateManifest(manifest, dirs.Package);
        var staged = WindowsServicePackageStorage.StagePackage(dirs.Root, manifest, dirs.Package);
        File.WriteAllText(original, "source-changed-after-verification");
        Assert.Equal("signed-original", File.ReadAllText(Path.Combine(staged, "service.exe")));
        var committed = WindowsServicePackageStorage.CommitStagedPackage(dirs.Root, manifest, staged);
        Assert.Equal("signed-original", File.ReadAllText(Path.Combine(committed, "service.exe")));
    }

    [Fact]
    public void Install_commit_never_deletes_an_existing_version()
    {
        using var dirs = new Folders();
        var manifest = Manifest();
        var committed = WindowsServicePackageStorage.CommitStagedPackage(dirs.Root, manifest,
            WindowsServicePackageStorage.StagePackage(dirs.Root, manifest, dirs.Package));
        File.WriteAllText(Path.Combine(committed, "preserve.txt"), "existing-user-state");
        var secondStage = WindowsServicePackageStorage.StagePackage(dirs.Root, manifest, dirs.Package);
        Assert.Throws<IOException>(() => WindowsServicePackageStorage.CommitStagedPackage(
            dirs.Root, manifest, secondStage));
        Assert.Equal("existing-user-state", File.ReadAllText(Path.Combine(committed, "preserve.txt")));
    }

    [Fact]
    public void Failed_update_restores_previous_version_and_does_not_touch_different_versions()
    {
        using var dirs = new Folders();
        var manifest = Manifest();
        var original = WindowsServicePackageStorage.CommitStagedPackage(dirs.Root, manifest,
            WindowsServicePackageStorage.StagePackage(dirs.Root, manifest, dirs.Package));
        File.WriteAllText(Path.Combine(original, "build.txt"), "old-build");
        var nextVersion = Manifest(version: "2.0");
        WindowsServicePackageStorage.CommitStagedPackage(dirs.Root, nextVersion,
            WindowsServicePackageStorage.StagePackage(dirs.Root, nextVersion, dirs.Package));
        File.WriteAllText(Path.Combine(dirs.Package, "build.txt"), "new-build");
        var staged = WindowsServicePackageStorage.StagePackage(dirs.Root, manifest, dirs.Package);
        var rollback = WindowsServicePackageStorage.BackupVersionForUpdate(dirs.Root, manifest);
        Assert.NotNull(rollback);
        WindowsServicePackageStorage.CommitStagedPackage(dirs.Root, manifest, staged);
        Assert.Equal("new-build", File.ReadAllText(Path.Combine(original, "build.txt")));
        WindowsServicePackageStorage.RestoreVersionAfterFailedUpdate(dirs.Root, manifest, rollback);
        Assert.Equal("old-build", File.ReadAllText(Path.Combine(original, "build.txt")));
        Assert.True(File.Exists(Path.Combine(dirs.Root, "service", "2.0", "service.exe")));
        Assert.False(Directory.Exists(rollback));
    }

    [Fact]
    public void Failed_new_version_update_removes_only_the_newly_committed_version()
    {
        using var dirs = new Folders();
        var manifest = Manifest(version: "2.0");
        Assert.Null(WindowsServicePackageStorage.BackupVersionForUpdate(dirs.Root, manifest));
        var committed = WindowsServicePackageStorage.CommitStagedPackage(dirs.Root, manifest,
            WindowsServicePackageStorage.StagePackage(dirs.Root, manifest, dirs.Package));
        Assert.True(Directory.Exists(committed));
        WindowsServicePackageStorage.RestoreVersionAfterFailedUpdate(dirs.Root, manifest, backup: null);
        Assert.False(Directory.Exists(committed));
    }

    [Fact]
    public void Manifest_rejects_reserved_security_state_embedded_in_untrusted_package()
    {
        using var dirs = new Folders();
        Directory.CreateDirectory(Path.Combine(dirs.Package, ".management"));
        Assert.Throws<InvalidOperationException>(() =>
            WindowsServicePackageStorage.ValidateManifest(Manifest(), dirs.Package));
    }

    [Fact]
    public void Symlink_in_package_cannot_be_copied_or_followed()
    {
        using var dirs = new Folders();
        var outside = Path.Combine(dirs.Root, "outside.txt");
        File.WriteAllText(outside, "outside");
        var link = Path.Combine(dirs.Package, "link.txt");
        try
        {
            File.CreateSymbolicLink(link, outside);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
        {
            // Developer Mode / SeCreateSymbolicLinkPrivilege is not guaranteed on Windows CI.
            return;
        }
        Assert.Throws<InvalidOperationException>(() =>
            WindowsServicePackageStorage.ValidateManifest(Manifest(), dirs.Package));
        Assert.Equal("outside", File.ReadAllText(outside));
    }

    private static ManagedServicePackageManifest Manifest(string name = "service", string version = "1.0") => new()
    {
        ServiceName = name,
        DisplayName = "Test Service",
        Version = version,
        EntryPoint = "service.exe"
    };

    private sealed class Folders : IDisposable
    {
        public Folders()
        {
            Root = Path.Combine(Path.GetTempPath(), "apikit-package-test-" + Guid.NewGuid().ToString("N"));
            Package = Path.Combine(Root, "source");
            Directory.CreateDirectory(Package);
            File.WriteAllText(Path.Combine(Package, "service.exe"), "binary");
        }
        public string Root { get; }
        public string Package { get; }
        public void Dispose() => WindowsServicePackageStorage.DeleteDirectoryIfExists(Root);
    }
}
