namespace ApiKit.Management.Lifecycle;

/// <summary>
/// Describes a package to install, including its entry point, version, dependencies, and metadata.
/// </summary>
public sealed record ManagedServicePackageManifest
{
    /// <summary>
    /// Gets or sets service name.
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets or sets version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets or sets entry point.
    /// </summary>
    public required string EntryPoint { get; init; }

    /// <summary>
    /// Gets or sets arguments.
    /// </summary>
    public IReadOnlyList<string> Arguments { get; init; } = [];

    /// <summary>
    /// Gets or sets dependencies.
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; init; } = [];

    /// <summary>
    /// Gets or sets metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();
}
