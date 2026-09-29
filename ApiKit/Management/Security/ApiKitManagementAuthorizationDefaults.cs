namespace ApiKit.Management.Security;

/// <summary>
/// Содержит стандартные имена, используемые авторизацией management plane ApiKit.
/// </summary>
public static class ApiKitManagementAuthorizationDefaults
{
    /// <summary>Базовая policy для просмотра каталога и вызова management-операций.</summary>
    public const string PolicyName = "ApiKit.Management";

    /// <summary>Policy для регистрации управляемого сервиса в локальном registry.</summary>
    public const string ServiceRegistrationPolicyName = "ApiKit.Management.ServiceRegistration";

    /// <summary>Policy, подтверждающая доверенный management host при обращении к сервису.</summary>
    public const string ManagementHostPolicyName = "ApiKit.Management.Host";

    /// <summary>Policy для запуска, остановки и перезапуска системных сервисов.</summary>
    public const string LifecyclePolicyName = "ApiKit.Management.Lifecycle";

    /// <summary>Policy для установки, обновления и удаления пакетов сервисов.</summary>
    public const string PackageManagementPolicyName = "ApiKit.Management.Packages";

    /// <summary>Claim, подтверждающий право principal работать с management plane.</summary>
    public const string ClaimType = "apikit.management";

    /// <summary>Значение claim для базового административного доступа.</summary>
    public const string AdministratorClaimValue = "admin";

    /// <summary>Значение claim для регистрации управляемого сервиса.</summary>
    public const string ServiceRegistrationClaimValue = "service-registration";

    /// <summary>Значение claim доверенного management host.</summary>
    public const string ManagementHostClaimValue = "host";

    /// <summary>Значение claim для управления жизненным циклом сервиса.</summary>
    public const string LifecycleClaimValue = "lifecycle";

    /// <summary>Значение claim для установки и обновления исполняемого кода.</summary>
    public const string PackageManagementClaimValue = "packages";
}
