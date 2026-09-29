using ApiKit.Management.Builders;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Добавляет metadata в descriptor текущего сервиса.
/// </summary>
public interface IManagementDescriptorContributor
{
    /// <summary>
    /// Добавляет собственные endpoints, ресурсы, операции или capabilities.
    /// </summary>
    /// <param name="builder">Builder итогового descriptor.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    ValueTask ContributeAsync(
        ManagementServiceDescriptorBuilder builder,
        CancellationToken cancellationToken = default);
}
