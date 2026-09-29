namespace ApiKit.Management.Models;

/// <summary>
/// Описывает конкретный запущенный экземпляр сервиса.
/// </summary>
/// <param name="ServiceName">Стабильное техническое имя сервиса.</param>
/// <param name="DisplayName">Отображаемое имя сервиса.</param>
/// <param name="InstanceId">Уникальный идентификатор текущего экземпляра процесса.</param>
/// <param name="Version">Версия сервиса, если она известна.</param>
/// <param name="Environment">Имя окружения приложения.</param>
/// <param name="MachineName">Имя компьютера, на котором работает сервис.</param>
/// <param name="ProcessId">Идентификатор процесса.</param>
/// <param name="StartedAtUtc">Время запуска экземпляра в UTC.</param>
public sealed record ManagementServiceIdentity(
    string ServiceName,
    string DisplayName,
    string InstanceId,
    string? Version,
    string Environment,
    string MachineName,
    int ProcessId,
    DateTimeOffset StartedAtUtc);
