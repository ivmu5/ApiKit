namespace ApiKit.Management.Windows;

/// <summary>Константы Windows management транспорта.</summary>
public static class WindowsManagementTransportDefaults
{
    /// <summary>Стабильное имя транспорта в management descriptor.</summary>
    public const string TransportName = "named-pipe";

    /// <summary>Назначение pipe регистрации и heartbeat управляемых сервисов.</summary>
    public const string RegistrationPurpose = "registration";

    /// <summary>Назначение административного pipe management host.</summary>
    public const string AdministrationPurpose = "administration";

    /// <summary>Назначение pipe, принимающего management operations и CRUD-resource запросы.</summary>
    public const string ManagementPurpose = "management";
}
