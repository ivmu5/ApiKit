using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Публикует регистрацию текущего сервиса во внешний management registry или broker.
/// </summary>
/// <remarks>
/// Конкретный транспорт намеренно не определяется ApiKit. Реализация может использовать
/// Named Pipes, Unix Domain Sockets, gRPC или другой локальный IPC-механизм.
/// </remarks>
public interface IManagementServicePublisher
{
    /// <summary>
    /// Публикует регистрацию или heartbeat экземпляра.
    /// </summary>
    ValueTask PublishAsync(
        ManagementServiceRegistration registration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет регистрацию при корректной остановке сервиса.
    /// </summary>
    ValueTask WithdrawAsync(
        string instanceId,
        CancellationToken cancellationToken = default);
}
