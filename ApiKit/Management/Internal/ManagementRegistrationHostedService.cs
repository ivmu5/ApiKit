using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;
using ApiKit.Management.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Internal;

internal sealed class ManagementRegistrationHostedService(
    IEnumerable<IManagementServicePublisher> publishers,
    IManagementServiceDescriptorProvider descriptorProvider,
    IOptions<ApiKitManagementOptions> options,
    IHostApplicationLifetime applicationLifetime,
    ILogger<ManagementRegistrationHostedService> logger)
    : BackgroundService
{
    private readonly IManagementServicePublisher[] _publishers = publishers.ToArray();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_publishers.Length == 0)
        {
            return;
        }

        await WaitForApplicationStartedAsync(stoppingToken);

        using var timer = new PeriodicTimer(options.Value.HeartbeatInterval);

        do
        {
            await PublishAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_publishers.Length > 0)
        {
            try
            {
                var descriptor = await descriptorProvider.GetDescriptorAsync(cancellationToken);

                foreach (var publisher in _publishers)
                {
                    await publisher.WithdrawAsync(
                        descriptor.Identity.InstanceId,
                        cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Остановка приложения уже отменена; lease самостоятельно истечёт в registry.
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Не удалось удалить management-регистрацию при остановке сервиса.");
            }
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task PublishAsync(CancellationToken cancellationToken)
    {
        var descriptor = await descriptorProvider.GetDescriptorAsync(cancellationToken);
        var registration = new ManagementServiceRegistration(
            descriptor,
            options.Value.LeaseDuration);

        foreach (var publisher in _publishers)
        {
            try
            {
                await publisher.PublishAsync(registration, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Не удалось опубликовать management-регистрацию сервиса {ServiceName}.",
                    descriptor.Identity.ServiceName);
            }
        }
    }

    private Task WaitForApplicationStartedAsync(CancellationToken cancellationToken)
    {
        if (applicationLifetime.ApplicationStarted.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var startedRegistration = applicationLifetime.ApplicationStarted.Register(
            static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true),
            completion);

        var cancellationRegistration = cancellationToken.Register(
            static state => ((TaskCompletionSource<bool>)state!).TrySetCanceled(),
            completion);

        return AwaitAndDisposeAsync(completion.Task, startedRegistration, cancellationRegistration);
    }

    private static async Task AwaitAndDisposeAsync(
        Task task,
        CancellationTokenRegistration startedRegistration,
        CancellationTokenRegistration cancellationRegistration)
    {
        try
        {
            await task;
        }
        finally
        {
            startedRegistration.Dispose();
            cancellationRegistration.Dispose();
        }
    }
}
