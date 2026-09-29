using ApiKit.Management.Models;

namespace ApiKit.Management.Options;

/// <summary>
/// Переопределяет отображение и доступность автоматически обнаруженного CRUD-ресурса.
/// </summary>
public sealed class ManagementResourceOptions
{
    /// <summary>Указывает, должен ли ресурс публиковаться и быть доступным management plane.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Техническое имя ресурса.</summary>
    public string? Name { get; set; }

    /// <summary>Отображаемое имя ресурса.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Группа ресурса в админ-панели.</summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// JSON-имя свойства read-модели, содержащего primary key. Если не задано,
    /// ApiKit пытается определить стандартные Id / &lt;Entity&gt;Id соглашения.
    /// </summary>
    public string? KeyJsonName { get; set; }

    /// <summary>
    /// Разрешённые management-операции. <see langword="null"/> означает все операции,
    /// фактически опубликованные CRUD-контроллером.
    /// </summary>
    public ISet<ManagementResourceOperation>? AllowedOperations { get; set; }

    /// <summary>Ограничивает management-доступ указанным набором CRUD-операций.</summary>
    public ManagementResourceOptions AllowOnly(params ManagementResourceOperation[] operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        AllowedOperations = new HashSet<ManagementResourceOperation>(operations);
        return this;
    }
}
