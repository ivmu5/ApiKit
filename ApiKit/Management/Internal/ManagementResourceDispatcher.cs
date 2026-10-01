using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ApiKit.Authorization.Permissions;
using ApiKit.Management.Security;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiKit.Management.Internal;

internal sealed class ManagementResourceDispatcher(
    CrudManagementResourceRegistry registry,
    IServiceProvider serviceProvider,
    ILogger<ManagementResourceDispatcher> logger)
    : IManagementResourceDispatcher
{
    public async ValueTask<ManagementResourceExecutionResult> ExecuteAsync(
        string resourceName,
        ManagementResourceOperation operation,
        ClaimsPrincipal principal,
        JsonElement? key = null,
        JsonElement? model = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentNullException.ThrowIfNull(principal);

        // Management CRUD is an authenticated transport feature, even if the target
        // MVC action intentionally allows anonymous HTTP requests.
        if (principal.Identity?.IsAuthenticated != true)
        {
            return BridgeFailure(
                StatusCodes.Status401Unauthorized,
                "unauthorized",
                "Management CRUD requires an authenticated caller.");
        }

        var resource = registry.Find(resourceName);
        if (resource is null)
        {
            return BridgeFailure(
                StatusCodes.Status404NotFound,
                "resource_not_found",
                $"CRUD-ресурс '{resourceName}' не найден.");
        }

        if (!resource.Actions.TryGetValue(operation, out var actionDescriptor))
        {
            return BridgeFailure(
                StatusCodes.Status405MethodNotAllowed,
                "operation_not_available",
                $"Операция '{operation}' недоступна для ресурса '{resource.Descriptor.Name}'.");
        }

        if (RequiresKey(operation) && key is null)
        {
            return BridgeFailure(
                StatusCodes.Status400BadRequest,
                "key_required",
                "Для выбранной операции требуется ключ ресурса.");
        }

        if (RequiresModel(operation) && model is null)
        {
            return BridgeFailure(
                StatusCodes.Status400BadRequest,
                "model_required",
                "Для выбранной операции требуется входная модель.");
        }

        object? routeKey = null;
        if (key is not null)
        {
            try
            {
                routeKey = GetRouteValue(key.Value);
            }
            catch (ArgumentException exception)
            {
                return BridgeFailure(
                    StatusCodes.Status400BadRequest,
                    "invalid_key",
                    exception.Message);
            }
        }

        var actionInvokerFactory = serviceProvider.GetService<IActionInvokerFactory>();
        if (actionInvokerFactory is null)
        {
            return BridgeFailure(
                StatusCodes.Status501NotImplemented,
                "mvc_unavailable",
                "ASP.NET Core MVC не зарегистрирован в текущем сервисе.");
        }

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            await using var responseBody = new MemoryStream();
            using var requestBody = CreateRequestBody(model);

            var routeData = new RouteData();
            routeData.Values["controller"] = actionDescriptor.ControllerName;
            routeData.Values["action"] = actionDescriptor.ActionName;

            if (routeKey is not null)
            {
                routeData.Values["id"] = routeKey;
            }

            var httpContext = new DefaultHttpContext
            {
                RequestServices = scope.ServiceProvider,
                User = CreateResourcePrincipal(principal, resource, operation),
                RequestAborted = cancellationToken
            };

            httpContext.Request.Method = GetHttpMethod(operation);
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("localhost");
            httpContext.Request.Path = "/__apikit/management/resource";
            httpContext.Request.RouteValues = routeData.Values;
            httpContext.Request.Headers.Accept = "application/json";
            httpContext.Response.Body = responseBody;

            if (operation == ManagementResourceOperation.List)
            {
                httpContext.Request.QueryString = QueryString.Create(
                [
                    new KeyValuePair<string, string?>("page", page.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new KeyValuePair<string, string?>("pageSize", pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture))
                ]);
            }

            if (requestBody is not null)
            {
                httpContext.Request.Body = requestBody;
                httpContext.Request.ContentLength = requestBody.Length;
                httpContext.Request.ContentType = operation == ManagementResourceOperation.Patch
                    ? "application/json-patch+json"
                    : "application/json";
            }

            if (actionDescriptor.EndpointMetadata.Count > 0)
            {
                httpContext.SetEndpoint(new Endpoint(
                    static _ => Task.CompletedTask,
                    new EndpointMetadataCollection(actionDescriptor.EndpointMetadata.ToArray()),
                    $"ApiKit management bridge: {actionDescriptor.DisplayName}"));
            }

            var actionContext = new ActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                new ModelStateDictionary());

            var invoker = actionInvokerFactory.CreateInvoker(actionContext);
            if (invoker is null)
            {
                return BridgeFailure(
                    StatusCodes.Status500InternalServerError,
                    "action_invoker_unavailable",
                    "ASP.NET Core не смог создать invoker для CRUD action.");
            }

            await invoker.InvokeAsync();

            var payload = await ReadResponsePayloadAsync(responseBody, cancellationToken);
            var statusCode = httpContext.Response.StatusCode;
            var succeeded = statusCode is >= 200 and < 300;

            return new ManagementResourceExecutionResult
            {
                Succeeded = succeeded,
                StatusCode = statusCode,
                Payload = payload,
                ErrorCode = succeeded ? null : $"http_{statusCode}",
                ErrorMessage = succeeded ? null : GetProblemMessage(payload, statusCode)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ошибка выполнения management CRUD-операции {Operation} для ресурса {ResourceName}.",
                operation,
                resourceName);

            return BridgeFailure(
                StatusCodes.Status500InternalServerError,
                "resource_execution_failed",
                "CRUD action завершился необработанной ошибкой внутри management bridge.");
        }
    }

    internal static ClaimsPrincipal CreateResourcePrincipal(
        ClaimsPrincipal principal,
        CrudManagementResourceDefinition resource,
        ManagementResourceOperation operation)
    {
        var identities = principal.Identities
            .Select(static identity => new ClaimsIdentity(identity))
            .ToList();

        // Only a verified administrative principal may receive the synthetic CRUD
        // permission for this particular management operation. Ordinary principals
        // must supply their own application permission; other MVC policies and roles
        // remain enforced by ASP.NET Core's authorization pipeline.
        if (principal.Identity?.IsAuthenticated == true
            && principal.HasClaim(
                ApiKitManagementAuthorizationDefaults.ClaimType,
                ApiKitManagementAuthorizationDefaults.AdministratorClaimValue)
            && resource.Descriptor.Permissions.TryGetValue(operation, out var permission)
            && !principal.HasClaim(PermissionAuthorizationDefaults.ClaimType, permission))
        {
            identities.Add(new ClaimsIdentity(
            [
                new Claim(PermissionAuthorizationDefaults.ClaimType, permission)
            ],
            authenticationType: "ApiKit.Management.Resource"));
        }

        return new ClaimsPrincipal(identities);
    }

    private static MemoryStream? CreateRequestBody(JsonElement? model)
    {
        if (model is null)
        {
            return null;
        }

        return new MemoryStream(
            Encoding.UTF8.GetBytes(model.Value.GetRawText()),
            writable: false);
    }

    private static string GetHttpMethod(ManagementResourceOperation operation) =>
        operation switch
        {
            ManagementResourceOperation.List or ManagementResourceOperation.Get => HttpMethods.Get,
            ManagementResourceOperation.Create => HttpMethods.Post,
            ManagementResourceOperation.Update => HttpMethods.Put,
            ManagementResourceOperation.Patch => HttpMethods.Patch,
            ManagementResourceOperation.Delete => HttpMethods.Delete,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

    private static bool RequiresKey(ManagementResourceOperation operation) =>
        operation is ManagementResourceOperation.Get
            or ManagementResourceOperation.Update
            or ManagementResourceOperation.Patch
            or ManagementResourceOperation.Delete;

    private static bool RequiresModel(ManagementResourceOperation operation) =>
        operation is ManagementResourceOperation.Create
            or ManagementResourceOperation.Update
            or ManagementResourceOperation.Patch;

    private static object GetRouteValue(JsonElement key) =>
        key.ValueKind switch
        {
            JsonValueKind.String => key.GetString() ?? string.Empty,
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => key.ToString(),
            _ => throw new ArgumentException(
                "Management resource key должен быть JSON string, number или boolean.",
                nameof(key))
        };

    private static async ValueTask<JsonElement?> ReadResponsePayloadAsync(
        MemoryStream responseBody,
        CancellationToken cancellationToken)
    {
        if (responseBody.Length == 0)
        {
            return null;
        }

        responseBody.Position = 0;

        try
        {
            using var document = await JsonDocument.ParseAsync(
                responseBody,
                cancellationToken: cancellationToken);

            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            responseBody.Position = 0;
            using var reader = new StreamReader(
                responseBody,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true);
            var text = await reader.ReadToEndAsync(cancellationToken);
            return JsonSerializer.SerializeToElement(text);
        }
    }

    private static string GetProblemMessage(JsonElement? payload, int statusCode)
    {
        if (payload is { ValueKind: JsonValueKind.Object } problem)
        {
            if (TryGetProperty(problem, "detail", out var detail)
                && detail.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(detail.GetString()))
            {
                return detail.GetString()!;
            }

            if (TryGetProperty(problem, "title", out var title)
                && title.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(title.GetString()))
            {
                return title.GetString()!;
            }
        }

        return $"CRUD action завершился HTTP-статусом {statusCode}.";
    }

    private static bool TryGetProperty(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static ManagementResourceExecutionResult BridgeFailure(
        int statusCode,
        string code,
        string message) => new()
        {
            Succeeded = false,
            StatusCode = statusCode,
            ErrorCode = code,
            ErrorMessage = message
        };
}
