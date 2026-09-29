namespace ApiKit.Management.Lifecycle;

/// <summary>
/// Унифицированное состояние управляемого системного сервиса.
/// </summary>
public enum ManagedServiceState
{
    /// <summary>Состояние неизвестно.</summary>
    Unknown,

    /// <summary>Сервис остановлен.</summary>
    Stopped,

    /// <summary>Сервис запускается.</summary>
    Starting,

    /// <summary>Сервис работает.</summary>
    Running,

    /// <summary>Сервис приостановлен.</summary>
    Paused,

    /// <summary>Сервис останавливается.</summary>
    Stopping,

    /// <summary>Сервис завершился с ошибкой или находится в ошибочном состоянии.</summary>
    Failed
}
