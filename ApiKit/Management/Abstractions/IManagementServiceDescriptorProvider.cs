using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Формирует актуальный descriptor management-возможностей текущего сервиса.
/// </summary>
public interface IManagementServiceDescriptorProvider
{
    /// <summary>
    /// Формирует descriptor текущего экземпляра сервиса.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Актуальный descriptor.</returns>
    ValueTask<ManagementServiceDescriptor> GetDescriptorAsync(
        CancellationToken cancellationToken = default);
}
