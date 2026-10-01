namespace ApiKit.Management.Options;

/// <summary>
/// Defines configuration settings for ApiKit management options.
/// </summary>
public sealed class ApiKitManagementOptions
{
    private readonly Dictionary<Type, ManagementResourceOptions> _resourceMappings = new();
    /// <summary>
    /// Gets or sets service name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets instance id.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets whether ASP.NET Core server address discovery is enabled.
    /// </summary>
    public bool DiscoverServerAddresses { get; set; } = true;

    /// <summary>
    /// Gets or sets whether HTTP endpoint discovery is enabled.
    /// </summary>
    public bool DiscoverEndpoints { get; set; } = true;

    /// <summary>
    /// Gets or sets whether automatic CRUD resource discovery is enabled.
    /// </summary>
    public bool DiscoverCrudResources { get; set; } = true;

    /// <summary>
    /// Gets or sets heartbeat interval.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets lease duration.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets capabilities.
    /// </summary>
    public ISet<string> Capabilities { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets metadata.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Configures a mapping for resource.
    /// </summary>
    /// <typeparam name="TEntity">The EF Core entity type.</typeparam>
    /// <param name="configure">An optional configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public ApiKitManagementOptions MapResource<TEntity>(
        Action<ManagementResourceOptions> configure)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(configure);

        var resourceOptions = new ManagementResourceOptions();
        configure(resourceOptions);
        _resourceMappings[typeof(TEntity)] = resourceOptions;
        return this;
    }

    /// <summary>
    /// Excludes the specified entity type from management CRUD discovery.
    /// </summary>
    public ApiKitManagementOptions IgnoreResource<TEntity>()
        where TEntity : class =>
        MapResource<TEntity>(options => options.Enabled = false);

    /// <summary>
    /// Configures a mapping for resource.
    /// </summary>
    public ApiKitManagementOptions MapResource<TEntity>(
        string name,
        string? displayName = null,
        string? groupName = null)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return MapResource<TEntity>(options =>
        {
            options.Name = name;
            options.DisplayName = displayName;
            options.GroupName = groupName;
        });
    }

    internal bool TryGetResourceOptions(
        Type entityType,
        out ManagementResourceOptions? resourceOptions) =>
        _resourceMappings.TryGetValue(entityType, out resourceOptions);
}
