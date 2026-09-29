namespace ApiKit.Management.Lifecycle;

/// <summary>
/// Платформенно-независимое описание пакета устанавливаемого сервиса.
/// </summary>
public sealed record ManagedServicePackageManifest
{
    /// <summary>Стабильное имя сервиса.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Отображаемое имя.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Версия пакета.</summary>
    public required string Version { get; init; }

    /// <summary>Относительный путь к запускаемому файлу внутри пакета.</summary>
    public required string EntryPoint { get; init; }

    /// <summary>Аргументы запуска.</summary>
    public IReadOnlyList<string> Arguments { get; init; } = [];

    /// <summary>Имена сервисов, необходимых до запуска этого сервиса.</summary>
    public IReadOnlyList<string> Dependencies { get; init; } = [];

    /// <summary>Дополнительные метаданные установщика.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();
}
