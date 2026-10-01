using ApiKit.Management.Abstractions;
using ApiKit.Management.Internal;
using ApiKit.Management.Models;
using ApiKit.Management.Options;
using ApiKit.Management.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiKit.Management.Builders;

/// <summary>
/// Configures additional management capabilities, publishers, and operations.
/// </summary>
public sealed class ApiKitManagementBuilder
{
    internal ApiKitManagementBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// Gets services.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Registers capability.
    /// </summary>
    public ApiKitManagementBuilder AddCapability(string capability)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capability);
        Services.Configure<ApiKitManagementOptions>(
            options => options.Capabilities.Add(capability.Trim()));
        return this;
    }

    /// <summary>
    /// Registers contributor.
    /// </summary>
    public ApiKitManagementBuilder AddContributor<TContributor>()
        where TContributor : class, IManagementDescriptorContributor
    {
        Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IManagementDescriptorContributor, TContributor>());

        return this;
    }

    /// <summary>
    /// Registers in process discovery.
    /// </summary>
    public ApiKitManagementBuilder AddInProcessDiscovery()
    {
        ApiKit.Management.ManagementServiceCollectionExtensions.AddApiKitManagementRegistry(Services);
        return AddPublisher<InMemoryManagementServicePublisher>();
    }

    /// <summary>
    /// Registers publisher.
    /// </summary>
    public ApiKitManagementBuilder AddPublisher<TPublisher>()
        where TPublisher : class, IManagementServicePublisher
    {
        Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IManagementServicePublisher, TPublisher>());

        return this;
    }

    /// <summary>
    /// Registers operation.
    /// </summary>
    /// <typeparam name="TOperation">The t operation type.</typeparam>
    /// <typeparam name="TRequest">The t request type.</typeparam>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <param name="name">The resource or operation name.</param>
    /// <param name="configure">An optional configuration callback.</param>
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
    /// Registers operation.
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
