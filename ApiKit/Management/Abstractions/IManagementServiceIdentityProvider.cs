using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Предоставляет стабильную идентификацию текущего экземпляра управляемого сервиса.
/// </summary>
public interface IManagementServiceIdentityProvider
{
    /// <summary>Возвращает идентификацию текущего процесса сервиса.</summary>
    ManagementServiceIdentity GetIdentity();
}
