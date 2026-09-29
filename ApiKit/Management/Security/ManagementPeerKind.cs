namespace ApiKit.Management.Security;

/// <summary>
/// Определяет роль участника локального management plane.
/// </summary>
public enum ManagementPeerKind
{
    /// <summary>Роль участника не определена.</summary>
    Unknown = 0,

    /// <summary>Центральный management host, координирующий локальные сервисы.</summary>
    ManagementHost = 1,

    /// <summary>Административный клиент, например локальная админ-панель.</summary>
    AdminClient = 2,

    /// <summary>Управляемый микросервис, зарегистрированный в management plane.</summary>
    ManagedService = 3
}
