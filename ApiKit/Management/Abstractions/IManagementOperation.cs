namespace ApiKit.Management.Abstractions;

/// <summary>
/// Представляет явно разрешённую произвольную management-операцию сервиса.
/// </summary>
/// <typeparam name="TRequest">Тип входных данных.</typeparam>
/// <typeparam name="TResult">Тип результата.</typeparam>
public interface IManagementOperation<in TRequest, TResult>
{
    /// <summary>
    /// Выполняет management-операцию.
    /// </summary>
    /// <param name="request">Входные данные.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат операции.</returns>
    ValueTask<TResult> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken = default);
}
