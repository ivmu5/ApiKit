using ApiKit.Management.Abstractions;
using ApiKit.Management.Lifecycle;
using ApiKit.Management.Windows.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Windows.Internal;

internal sealed class WindowsManagedServiceInstaller(
    IServiceScopeFactory scopeFactory,
    IManagedServiceLifecycleController lifecycleController,
    IOptions<WindowsServiceManagementOptions> options)
    : IManagedServiceInstaller
{
    public async ValueTask InstallAsync(
        ManagedServicePackageManifest manifest,
        string packageDirectory,
        CancellationToken cancellationToken = default)
    {
        WindowsServicePackageStorage.ValidateManifest(manifest, packageDirectory);

        var state = await lifecycleController.GetStateAsync(manifest.ServiceName, cancellationToken);
        if (state != ManagedServiceState.Unknown)
        {
            throw new InvalidOperationException(
                $"Windows Service '{manifest.ServiceName}' уже существует. Используйте update вместо install.");
        }

        var stagedDirectory = WindowsServicePackageStorage.StagePackage(
            options.Value.InstallRoot,
            manifest,
            packageDirectory);

        try
        {
            await VerifyPackageAsync(manifest, stagedDirectory, cancellationToken);
            WindowsServicePackageStorage.ResolveEntryPoint(manifest, stagedDirectory);

            var installDirectory = WindowsServicePackageStorage.CommitStagedPackage(
                options.Value.InstallRoot,
                manifest,
                stagedDirectory);
            stagedDirectory = string.Empty;

            var entryPoint = WindowsServicePackageStorage.ResolveEntryPoint(manifest, installDirectory);

            try
            {
                await WindowsServiceControlCommands.CreateAsync(
                    manifest,
                    entryPoint,
                    options.Value.ServiceAccount,
                    cancellationToken);

                await WindowsServiceControlCommands.ConfigureDependenciesAsync(manifest, cancellationToken);
                await ApplyDescriptionAsync(manifest, cancellationToken);

                if (options.Value.StartAfterInstall)
                {
                    await lifecycleController.StartAsync(manifest.ServiceName, cancellationToken);
                }
            }
            catch
            {
                await WindowsServiceControlCommands.TryDeleteAsync(manifest.ServiceName);
                throw;
            }
        }
        finally
        {
            WindowsServicePackageStorage.DeleteDirectoryIfExists(stagedDirectory);
        }
    }

    public async ValueTask UpdateAsync(
        ManagedServicePackageManifest manifest,
        string packageDirectory,
        CancellationToken cancellationToken = default)
    {
        WindowsServicePackageStorage.ValidateManifest(manifest, packageDirectory);

        var previousState = await lifecycleController.GetStateAsync(manifest.ServiceName, cancellationToken);
        if (previousState == ManagedServiceState.Unknown)
        {
            throw new InvalidOperationException(
                $"Windows Service '{manifest.ServiceName}' не найден. Используйте install вместо update.");
        }

        var stagedDirectory = WindowsServicePackageStorage.StagePackage(
            options.Value.InstallRoot,
            manifest,
            packageDirectory);

        try
        {
            // Проверяем snapshot до остановки сервиса: это уменьшает downtime и закрывает
            // окно между проверкой доверенности и использованием файлов пакета.
            await VerifyPackageAsync(manifest, stagedDirectory, cancellationToken);
            WindowsServicePackageStorage.ResolveEntryPoint(manifest, stagedDirectory);

            var shouldRestart = previousState is ManagedServiceState.Running
                or ManagedServiceState.Starting
                or ManagedServiceState.Paused;

            if (shouldRestart)
            {
                await lifecycleController.StopAsync(manifest.ServiceName, cancellationToken);
            }

            try
            {
                var installDirectory = WindowsServicePackageStorage.CommitStagedPackage(
                    options.Value.InstallRoot,
                    manifest,
                    stagedDirectory);
                stagedDirectory = string.Empty;

                var entryPoint = WindowsServicePackageStorage.ResolveEntryPoint(manifest, installDirectory);

                await WindowsServiceControlCommands.ConfigureAsync(
                    manifest,
                    entryPoint,
                    options.Value.ServiceAccount,
                    cancellationToken);

                await WindowsServiceControlCommands.ConfigureDependenciesAsync(manifest, cancellationToken);
                await ApplyDescriptionAsync(manifest, cancellationToken);

                if (shouldRestart)
                {
                    await lifecycleController.StartAsync(manifest.ServiceName, cancellationToken);
                }
            }
            catch
            {
                if (shouldRestart)
                {
                    try
                    {
                        await lifecycleController.StartAsync(manifest.ServiceName, CancellationToken.None);
                    }
                    catch
                    {
                        // Исходное исключение важнее best-effort восстановления процесса.
                    }
                }

                throw;
            }
        }
        finally
        {
            WindowsServicePackageStorage.DeleteDirectoryIfExists(stagedDirectory);
        }
    }

    public async ValueTask UninstallAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var state = await lifecycleController.GetStateAsync(serviceName, cancellationToken);
        if (state == ManagedServiceState.Unknown)
        {
            return;
        }

        if (state is ManagedServiceState.Running
            or ManagedServiceState.Starting
            or ManagedServiceState.Stopping
            or ManagedServiceState.Paused)
        {
            await lifecycleController.StopAsync(serviceName, cancellationToken);
        }

        await WindowsServiceControlCommands.DeleteAsync(serviceName, cancellationToken);
    }

    private async ValueTask VerifyPackageAsync(
        ManagedServicePackageManifest manifest,
        string packageDirectory,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var verifiers = scope.ServiceProvider
            .GetServices<IManagedServicePackageVerifier>()
            .ToArray();

        if (verifiers.Length == 0)
        {
            if (options.Value.RequirePackageVerifier)
            {
                throw new InvalidOperationException(
                    "Установка пакета запрещена: не зарегистрирован IManagedServicePackageVerifier.");
            }

            return;
        }

        foreach (var verifier in verifiers)
        {
            if (!await verifier.VerifyAsync(manifest, packageDirectory, cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Пакет сервиса '{manifest.ServiceName}' отклонён проверкой доверенности.");
            }
        }
    }

    private static Task ApplyDescriptionAsync(
        ManagedServicePackageManifest manifest,
        CancellationToken cancellationToken)
    {
        if (!manifest.Metadata.TryGetValue("description", out var description)
            || string.IsNullOrWhiteSpace(description))
        {
            return Task.CompletedTask;
        }

        return WindowsServiceControlCommands.SetDescriptionAsync(
            manifest.ServiceName,
            description,
            cancellationToken);
    }
}
