namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management operation.
/// </summary>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResult">The t result type.</typeparam>
public interface IManagementOperation<in TRequest, TResult>
{
    /// <summary>
    /// Executes async.
    /// </summary>
    /// <param name="request">The operation request.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<TResult> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken = default);
}
