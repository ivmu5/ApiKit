using System.ComponentModel;
using System.ServiceProcess;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Lifecycle;
using ApiKit.Management.Windows.Options;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Windows.Internal;

internal sealed class WindowsManagedServiceLifecycleController(
    IOptions<WindowsServiceManagementOptions> options,
    TimeProvider timeProvider)
    : IManagedServiceLifecycleController
{
    private const int ErrorServiceDoesNotExist = 1060;

    public ValueTask<ManagedServiceState> GetStateAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var controller = new ServiceController(serviceName);
            return ValueTask.FromResult(MapState(controller.Status));
        }
        catch (InvalidOperationException exception)
            when (IsServiceMissing(exception))
        {
            return ValueTask.FromResult(ManagedServiceState.Unknown);
        }
    }

    public async ValueTask StartAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        using var controller = new ServiceController(serviceName);
        controller.Refresh();

        if (controller.Status == ServiceControllerStatus.Running)
        {
            return;
        }

        if (controller.Status == ServiceControllerStatus.Paused)
        {
            controller.Continue();
        }
        else if (controller.Status is not ServiceControllerStatus.StartPending
                 and not ServiceControllerStatus.ContinuePending)
        {
            controller.Start();
        }

        await WaitForStatusAsync(controller, ServiceControllerStatus.Running, cancellationToken);
    }

    public async ValueTask StopAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        using var controller = new ServiceController(serviceName);
        controller.Refresh();

        if (controller.Status == ServiceControllerStatus.Stopped)
        {
            return;
        }

        if (controller.Status is not ServiceControllerStatus.StopPending)
        {
            if (!controller.CanStop)
            {
                throw new InvalidOperationException($"Windows Service '{serviceName}' не поддерживает остановку.");
            }

            controller.Stop();
        }

        await WaitForStatusAsync(controller, ServiceControllerStatus.Stopped, cancellationToken);
    }

    public async ValueTask RestartAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        await StopAsync(serviceName, cancellationToken);
        await StartAsync(serviceName, cancellationToken);
    }

    private async Task WaitForStatusAsync(
        ServiceController controller,
        ServiceControllerStatus requiredStatus,
        CancellationToken cancellationToken)
    {
        var timeout = options.Value.OperationTimeout;
        var deadline = timeProvider.GetUtcNow() + timeout;

        while (timeProvider.GetUtcNow() < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            controller.Refresh();

            if (controller.Status == requiredStatus)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), timeProvider, cancellationToken);
        }

        throw new TimeoutException(
            $"Windows Service '{controller.ServiceName}' не перешёл в состояние '{requiredStatus}' за {timeout}.");
    }

    private static bool IsServiceMissing(InvalidOperationException exception) =>
        exception.InnerException is Win32Exception
        {
            NativeErrorCode: ErrorServiceDoesNotExist
        };

    private static ManagedServiceState MapState(ServiceControllerStatus status) => status switch
    {
        ServiceControllerStatus.Stopped => ManagedServiceState.Stopped,
        ServiceControllerStatus.StartPending => ManagedServiceState.Starting,
        ServiceControllerStatus.Running => ManagedServiceState.Running,
        ServiceControllerStatus.StopPending => ManagedServiceState.Stopping,
        ServiceControllerStatus.Paused => ManagedServiceState.Paused,
        ServiceControllerStatus.PausePending => ManagedServiceState.Paused,
        ServiceControllerStatus.ContinuePending => ManagedServiceState.Starting,
        _ => ManagedServiceState.Unknown
    };
}
