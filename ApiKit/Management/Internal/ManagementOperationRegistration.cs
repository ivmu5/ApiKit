using System.Text.Json;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;
using ApiKit.Management.Operations;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Internal;

internal sealed class ManagementOperationRegistration<TOperation, TRequest, TResult>
    : IManagementOperationRegistration
    where TOperation : class, IManagementOperation<TRequest, TResult>
{
    public ManagementOperationRegistration(
        string name,
        ManagementOperationOptions options)
    {
        Descriptor = new ManagementOperationDescriptor
        {
            Name = name,
            DisplayName = string.IsNullOrWhiteSpace(options.DisplayName)
                ? name
                : options.DisplayName.Trim(),
            Description = string.IsNullOrWhiteSpace(options.Description)
                ? null
                : options.Description.Trim(),
            GroupName = string.IsNullOrWhiteSpace(options.GroupName)
                ? null
                : options.GroupName.Trim(),
            RequestTypeName = typeof(TRequest).FullName ?? typeof(TRequest).Name,
            ResultTypeName = typeof(TResult).FullName ?? typeof(TResult).Name,
            AuthorizationPolicy = options.AuthorizationPolicy.Trim()
        };
    }

    public ManagementOperationDescriptor Descriptor { get; }

    public async ValueTask<JsonElement?> ExecuteAsync(
        IServiceProvider serviceProvider,
        JsonElement? request,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<TOperation>();
        var serializerOptions = serviceProvider
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value
            .SerializerOptions;

        var requestValue = DeserializeRequest(request, serializerOptions);
        var result = await handler.ExecuteAsync(requestValue, cancellationToken);

        if (typeof(TResult) == typeof(ManagementEmptyResult))
        {
            return null;
        }

        return JsonSerializer.SerializeToElement(result, serializerOptions);
    }

    private static TRequest DeserializeRequest(
        JsonElement? request,
        JsonSerializerOptions serializerOptions)
    {
        if (typeof(TRequest) == typeof(ManagementEmptyRequest))
        {
            return (TRequest)(object)new ManagementEmptyRequest();
        }

        if (request is null)
        {
            throw new JsonException("Для management-операции отсутствует обязательная входная модель.");
        }

        var value = request.Value.Deserialize<TRequest>(serializerOptions);

        if (value is null)
        {
            throw new JsonException("Входная модель management-операции не может быть null.");
        }

        return value;
    }
}
