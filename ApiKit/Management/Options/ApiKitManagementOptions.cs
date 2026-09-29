namespace ApiKit.Management.Options;

/// <summary>
/// Настройки management metadata и регистрации текущего сервиса.
/// </summary>
public sealed class ApiKitManagementOptions
{
    private readonly Dictionary<Type, ManagementResourceOptions> _resourceMappings = new();
    /// <summary>
    /// Стабильное техническое имя сервиса. Если не задано, ApiKit использует
    /// нормализованное имя entry assembly приложения.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Отображаемое имя сервиса. Если не задано, используется имя приложения.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Идентификатор текущего экземпляра. По умолчанию создаётся новый GUID при запуске процесса.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Версия сервиса. Если не задана, берётся из entry assembly.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>Автоматически публиковать адреса ASP.NET Core server.</summary>
    public bool DiscoverServerAddresses { get; set; } = true;

    /// <summary>Автоматически обнаруживать HTTP endpoints ASP.NET Core.</summary>
    public bool DiscoverEndpoints { get; set; } = true;

    /// <summary>Автоматически обнаруживать generic CRUD-ресурсы ApiKit.</summary>
    public bool DiscoverCrudResources { get; set; } = true;

    /// <summary>Интервал heartbeat для зарегистрированных publishers.</summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Срок действия регистрации без heartbeat.</summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Дополнительные capabilities сервиса.</summary>
    public ISet<string> Capabilities { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Дополнительные метаданные, публикуемые вместе с descriptor.</summary>
    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Переопределяет имя или группу автоматически обнаруженного CRUD-ресурса.
    /// </summary>
    /// <typeparam name="TEntity">Тип EF entity ресурса.</typeparam>
    /// <param name="configure">Настройка представления ресурса в management metadata.</param>
    /// <returns>Текущие настройки для цепочки вызовов.</returns>
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
    /// Исключает CRUD-ресурс из management discovery и management-доступа.
    /// </summary>
    public ApiKitManagementOptions IgnoreResource<TEntity>()
        where TEntity : class =>
        MapResource<TEntity>(options => options.Enabled = false);

    /// <summary>
    /// Переопределяет техническое, отображаемое имя и группу CRUD-ресурса.
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
