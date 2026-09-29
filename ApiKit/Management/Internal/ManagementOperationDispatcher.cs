using System.Security.Claims;
using System.Text.Json;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiKit.Management.Internal;

internal sealed class ManagementOperationDispatcher(
    IEnumerable<IManagementOperationRegistration> registrations,
    IServiceScopeFactory scopeFactory,
    ILogger<ManagementOperationDispatcher> logger)
    : IManagementOperationDispatcher
{
    private readonly IReadOnlyDictionary<string, IManagementOperationRegistration> _registrations =
        registrations.ToDictionary(
            registration => registration.Descriptor.Name,
            StringComparer.OrdinalIgnoreCase);

    public async ValueTask<ManagementOperationExecutionResult> ExecuteAsync(
        string operationName,
        JsonElement? request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(principal);

        if (!_registrations.TryGetValue(operationName, out var registration))
        {
            return Failure("operation_not_found", "Management-операция не найдена.");
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var authorization = await authorizationService.AuthorizeAsync(
            principal,
            resource: null,
            registration.Descriptor.AuthorizationPolicy);

        if (!authorization.Succeeded)
        {
            return Failure("forbidden", "Недостаточно прав для выполнения management-операции.");
        }

        try
        {
            var value = await registration.ExecuteAsync(
                scope.ServiceProvider,
                request,
                cancellationToken);

            return new ManagementOperationExecutionResult
            {
                Succeeded = true,
                Value = value
            };
        }
        catch (JsonException exception)
        {
            return Failure("invalid_request", exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ошибка при выполнении management-операции {OperationName}.",
                operationName);

            return Failure("operation_failed", "Management-операция завершилась с ошибкой.");
        }
    }

    private static ManagementOperationExecutionResult Failure(
        string code,
        string message) => new()
        {
            Succeeded = false,
            ErrorCode = code,
            ErrorMessage = message
        };
}
