using ApiKit.Management.Abstractions;
using ApiKit.Management.Internal;
using ApiKit.Management.Models;
using ApiKit.Management.Options;
using ApiKit.Management.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiKit.Management.Builders;

/// <summary>
/// Builder регистрации management-возможностей сервиса.
/// </summary>
public sealed class ApiKitManagementBuilder
{
    internal ApiKitManagementBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>Коллекция сервисов приложения.</summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Добавляет статическую capability в descriptor сервиса.
    /// </summary>
    public ApiKitManagementBuilder AddCapability(string capability)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capability);
        Services.Configure<ApiKitManagementOptions>(
            options => options.Capabilities.Add(capability.Trim()));
        return this;
    }

    /// <summary>
    /// Добавляет singleton-safe contributor, публикующий дополнительную metadata в descriptor сервиса.
    /// </summary>
    public ApiKitManagementBuilder AddContributor<TContributor>()
        where TContributor : class, IManagementDescriptorContributor
    {
        Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IManagementDescriptorContributor, TContributor>());

        return this;
    }

    /// <summary>
    /// Подключает in-process registry и publisher для тестов или однопроцессного management host.
    /// </summary>
    /// <remarks>
    /// Метод не обеспечивает межпроцессное обнаружение. Для реальных микросервисов
    /// следует добавить publisher из transport-модуля Named Pipes или Unix Domain Sockets.
    /// </remarks>
    public ApiKitManagementBuilder AddInProcessDiscovery()
    {
        ApiKit.Management.ManagementServiceCollectionExtensions.AddApiKitManagementRegistry(Services);
        return AddPublisher<InMemoryManagementServicePublisher>();
    }

    /// <summary>
    /// Добавляет singleton publisher, который будет получать descriptor и heartbeat текущего сервиса.
    /// </summary>
    public ApiKitManagementBuilder AddPublisher<TPublisher>()
        where TPublisher : class, IManagementServicePublisher
    {
        Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IManagementServicePublisher, TPublisher>());

        return this;
    }

    /// <summary>
    /// Регистрирует явно разрешённую management-операцию.
    /// </summary>
    /// <typeparam name="TOperation">Тип обработчика операции.</typeparam>
    /// <typeparam name="TRequest">Тип входной модели.</typeparam>
    /// <typeparam name="TResult">Тип результата.</typeparam>
    /// <param name="name">Стабильное техническое имя операции.</param>
    /// <param name="configure">Необязательная metadata-настройка операции.</param>
    public ApiKitManagementBuilder AddOperation<TOperation, TRequest, TResult>(
        string name,
        Action<ManagementOperationOptions>? configure = null)
        where TOperation : class, IManagementOperation<TRequest, TResult>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var options = new ManagementOperationOptions();
        configure?.Invoke(options);

        if (string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
        {
            throw new ArgumentException(
                "AuthorizationPolicy management-операции не может быть пустой.",
                nameof(configure));
        }

        var normalizedName = name.Trim();

        var duplicateExists = Services.Any(descriptor =>
            descriptor.ServiceType == typeof(IManagementOperationRegistration)
            && descriptor.ImplementationInstance is IManagementOperationRegistration registration
            && string.Equals(
                registration.Descriptor.Name,
                normalizedName,
                StringComparison.OrdinalIgnoreCase));

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                $"Management-операция '{normalizedName}' уже зарегистрирована.");
        }

        Services.TryAddScoped<TOperation>();
        Services.AddSingleton<IManagementOperationRegistration>(
            new ManagementOperationRegistration<TOperation, TRequest, TResult>(
                normalizedName,
                options));

        return this;
    }

    /// <summary>
    /// Регистрирует management-операцию без входных параметров и результата.
    /// </summary>
    public ApiKitManagementBuilder AddOperation<TOperation>(
        string name,
        Action<ManagementOperationOptions>? configure = null)
        where TOperation : class,
            IManagementOperation<ManagementEmptyRequest, ManagementEmptyResult> =>
        AddOperation<TOperation, ManagementEmptyRequest, ManagementEmptyResult>(
            name,
            configure);
}
