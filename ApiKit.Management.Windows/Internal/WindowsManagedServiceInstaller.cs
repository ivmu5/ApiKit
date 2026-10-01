using ApiKit.Management.Abstractions;
using ApiKit.Management.Lifecycle;
using ApiKit.Management.Provisioning;
using ApiKit.Management.Security;
using ApiKit.Management.Windows.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Windows.Internal;

internal sealed class WindowsManagedServiceInstaller(
    IServiceScopeFactory scopeFactory,
    IManagedServiceLifecycleController lifecycleController,
    IManagementServiceCatalog serviceCatalog,
    TimeProvider timeProvider,
    WindowsManagedServiceIsolationRegistry isolationRegistry,
    IOptions<WindowsManagementHostOptions> hostOptions,
    IOptions<WindowsServiceManagementOptions> options)
    : IManagedServiceInstaller
{
    public async ValueTask InstallAsync(
        ManagedServicePackageManifest manifest,
        string packageDirectory,
        CancellationToken cancellationToken = default)
    {
        WindowsServicePackageStorage.ValidateManifest(manifest, packageDirectory);
        WindowsManagedServiceIsolation.ProtectInstallRoot(options.Value);

        var state = await lifecycleController.GetStateAsync(manifest.ServiceName, cancellationToken);
        if (state != ManagedServiceState.Unknown)
        {
            throw new InvalidOperationException(
                $"Windows Service '{manifest.ServiceName}' уже существует. Используйте update вместо install.");
        }

        var installationStartedAtUtc = timeProvider.GetUtcNow();
        var previousInstances = (await serviceCatalog
                .GetServicesAsync(manifest.ServiceName, cancellationToken))
            .ToDictionary(
                service => service.Descriptor.Identity.InstanceId,
                service => service.LastSeenAtUtc,
                StringComparer.Ordinal);

        var stagedDirectory = WindowsServicePackageStorage.StagePackage(
            options.Value.InstallRoot,
            manifest,
            packageDirectory);

        string? committedInstallDirectory = null;
        string? serviceRootDirectory = null;
        var attemptedProvisioners = new List<IManagedServiceCredentialProvisioner>();
        ManagedServiceCredentialProvisioningContext? provisioningContext = null;
        var isolationAttempted = false;

        await using var operationScope = scopeFactory.CreateAsyncScope();
        var services = operationScope.ServiceProvider;

        try
        {
            await VerifyPackageAsync(services, manifest, stagedDirectory, cancellationToken);
            WindowsServicePackageStorage.ResolveEntryPoint(manifest, stagedDirectory);

            var provisioners = GetCredentialProvisioners(
                services,
                requireAtLeastOne: options.Value.RequireCredentialProvisioner);

            committedInstallDirectory = WindowsServicePackageStorage.CommitStagedPackage(
                options.Value.InstallRoot,
                manifest,
                stagedDirectory);
            stagedDirectory = string.Empty;

            serviceRootDirectory = WindowsServicePackageStorage.GetServiceDirectory(
                options.Value.InstallRoot,
                manifest.ServiceName);

            provisioningContext = new ManagedServiceCredentialProvisioningContext(
                manifest,
                ManagementPeerIdentity.ForManagementHost(hostOptions.Value.ManagementHostPeerId),
                serviceRootDirectory,
                committedInstallDirectory);

            foreach (var provisioner in provisioners)
            {
                attemptedProvisioners.Add(provisioner);
                await provisioner.ProvisionAsync(provisioningContext, cancellationToken);
            }

            var entryPoint = WindowsServicePackageStorage.ResolveEntryPoint(
                manifest,
                committedInstallDirectory);

            try
            {
                await WindowsServiceControlCommands.CreateAsync(
                    manifest,
                    entryPoint,
                    options.Value.ServiceAccount,
                    cancellationToken);

                if (options.Value.EnableServiceSidIsolation)
                {
                    await WindowsServiceControlCommands.ConfigureUnrestrictedServiceSidAsync(
                        manifest.ServiceName,
                        cancellationToken);

                    isolationAttempted = true;
                    WindowsManagedServiceIsolation.Provision(
                        manifest.ServiceName,
                        serviceRootDirectory,
                        committedInstallDirectory,
                        options.Value,
                        timeProvider);
                    isolationRegistry.NotifyChanged();
                }

                await WindowsServiceControlCommands.ConfigureDependenciesAsync(manifest, cancellationToken);
                await ApplyDescriptionAsync(manifest, cancellationToken);

                if (options.Value.StartAfterInstall)
                {
                    await lifecycleController.StartAsync(manifest.ServiceName, cancellationToken);

                    if (options.Value.WaitForAuthenticatedRegistration)
                    {
                        await WaitForAuthenticatedRegistrationAsync(
                            manifest.ServiceName,
                            previousInstances,
                            installationStartedAtUtc,
                            cancellationToken);
                    }
                }
            }
            catch
            {
                await TryStopAndDeleteServiceAsync(manifest.ServiceName);
                throw;
            }
        }
        catch (Exception exception)
        {
            if (!await IsServiceSafeForCredentialCleanupAsync(manifest.ServiceName))
            {
                throw new InvalidOperationException(
                    $"Установка сервиса '{manifest.ServiceName}' завершилась ошибкой, но процесс сервиса " +
                    "не удалось подтвердить как остановленный. Security provisioning и package files сохранены " +
                    "для безопасного ручного восстановления/очистки.",
                    exception);
            }

            if (isolationAttempted && serviceRootDirectory is not null)
            {
                WindowsManagedServiceIsolationState.Delete(serviceRootDirectory);
                isolationRegistry.NotifyChanged();
            }

            if (provisioningContext is not null)
            {
                var rollbackErrors = await RollbackProvisioningAsync(
                    attemptedProvisioners,
                    provisioningContext,
                    CancellationToken.None);

                if (rollbackErrors.Count > 0)
                {
                    var allErrors = new List<Exception> { exception };
                    allErrors.AddRange(rollbackErrors);

                    throw new InvalidOperationException(
                        $"Установка сервиса '{manifest.ServiceName}' завершилась ошибкой, и security rollback " +
                        "не удалось завершить полностью. Package/security state сохранены для ручного восстановления.",
                        new AggregateException(allErrors));
                }
            }

            WindowsServicePackageStorage.DeleteDirectoryIfExists(committedInstallDirectory);

            if (serviceRootDirectory is not null)
            {
                WindowsServicePackageStorage.TryDeleteDirectoryIfEmpty(serviceRootDirectory);
            }

            throw;
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
        WindowsManagedServiceIsolation.ProtectInstallRoot(options.Value);

        var previousState = await lifecycleController.GetStateAsync(manifest.ServiceName, cancellationToken);
        if (previousState == ManagedServiceState.Unknown)
        {
            throw new InvalidOperationException(
                $"Windows Service '{manifest.ServiceName}' не найден. Используйте install вместо update.");
        }

        EnsureOwnedService(manifest.ServiceName);

        var stagedDirectory = WindowsServicePackageStorage.StagePackage(
            options.Value.InstallRoot,
            manifest,
            packageDirectory);

        await using var operationScope = scopeFactory.CreateAsyncScope();

        try
        {
            await VerifyPackageAsync(
                operationScope.ServiceProvider,
                manifest,
                stagedDirectory,
                cancellationToken);
            WindowsServicePackageStorage.ResolveEntryPoint(manifest, stagedDirectory);

            var shouldRestart = previousState is ManagedServiceState.Running
                or ManagedServiceState.Starting
                or ManagedServiceState.Paused;

            if (shouldRestart)
            {
                await lifecycleController.StopAsync(manifest.ServiceName, cancellationToken);
            }

            string? previousVersionSnapshot = null;
            var newPackageCommitted = false;
            var scmUpdateAttempted = false;

            try
            {
                previousVersionSnapshot = WindowsServicePackageStorage.BackupVersionForUpdate(
                    options.Value.InstallRoot,
                    manifest);

                var installDirectory = WindowsServicePackageStorage.CommitStagedPackage(
                    options.Value.InstallRoot,
                    manifest,
                    stagedDirectory);
                newPackageCommitted = true;
                stagedDirectory = string.Empty;

                var entryPoint = WindowsServicePackageStorage.ResolveEntryPoint(manifest, installDirectory);

                // An unsuccessful SCM command can still have applied a partial
                // configuration change. Keep the new package available for recovery.
                scmUpdateAttempted = true;
                await WindowsServiceControlCommands.ConfigureAsync(
                    manifest,
                    entryPoint,
                    options.Value.ServiceAccount,
                    cancellationToken);

                if (options.Value.EnableServiceSidIsolation)
                {
                    await WindowsServiceControlCommands.ConfigureUnrestrictedServiceSidAsync(
                        manifest.ServiceName,
                        cancellationToken);

                    var serviceRootDirectory = WindowsServicePackageStorage.GetServiceDirectory(
                        options.Value.InstallRoot,
                        manifest.ServiceName);

                    WindowsManagedServiceIsolation.Provision(
                        manifest.ServiceName,
                        serviceRootDirectory,
                        installDirectory,
                        options.Value,
                        timeProvider);
                    isolationRegistry.NotifyChanged();
                }

                await WindowsServiceControlCommands.ConfigureDependenciesAsync(manifest, cancellationToken);
                await ApplyDescriptionAsync(manifest, cancellationToken);

                if (shouldRestart)
                {
                    await lifecycleController.StartAsync(manifest.ServiceName, cancellationToken);
                }

                WindowsServicePackageStorage.DeleteDirectoryIfExists(previousVersionSnapshot);
                previousVersionSnapshot = null;
            }
            catch (Exception updateError)
            {
                Exception? rollbackError = null;

                if (!scmUpdateAttempted &&
                    (previousVersionSnapshot is not null || newPackageCommitted))
                {
                    try
                    {
                        WindowsServicePackageStorage.RestoreVersionAfterFailedUpdate(
                            options.Value.InstallRoot, manifest, previousVersionSnapshot);
                        previousVersionSnapshot = null;
                    }
                    catch (Exception exception)
                    {
                        rollbackError = exception;
                    }
                }

                if (shouldRestart)
                {
                    try
                    {
                        await lifecycleController.StartAsync(manifest.ServiceName, CancellationToken.None);
                    }
                    catch
                    {
                    }
                }

                if (scmUpdateAttempted)
                {
                    throw new InvalidOperationException(
                        "Update failed after SCM reconfiguration began. The new package and any " +
                        "rollback snapshot were retained for manual recovery; do not delete them " +
                        "until the actual SCM configuration has been inspected.",
                        updateError);
                }

                if (rollbackError is not null)
                {
                    throw new InvalidOperationException(
                        "Update завершился ошибкой, а rollback файловой версии выполнить не удалось. " +
                        "Сохранённый snapshot оставлен в .staging для ручного восстановления.",
                        new AggregateException(updateError, rollbackError));
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
        // An absent SCM entry and absent install directory are already uninstalled.
        var root = WindowsServicePackageStorage.GetServiceDirectory(options.Value.InstallRoot, serviceName);
        if (state == ManagedServiceState.Unknown && !Directory.Exists(root))
        {
            return;
        }

        EnsureOwnedService(serviceName);

        if (state != ManagedServiceState.Unknown)
        {
            if (state is ManagedServiceState.Running
                or ManagedServiceState.Starting
                or ManagedServiceState.Stopping
                or ManagedServiceState.Paused)
            {
                await lifecycleController.StopAsync(serviceName, cancellationToken);
            }

            await WindowsServiceControlCommands.DeleteAsync(serviceName, cancellationToken);
        }

        var serviceRootDirectory = WindowsServicePackageStorage.GetServiceDirectory(
            options.Value.InstallRoot,
            serviceName);

        var context = new ManagedServiceCredentialDeprovisioningContext(
            serviceName,
            ManagementPeerIdentity.ForManagementHost(hostOptions.Value.ManagementHostPeerId),
            serviceRootDirectory);

        if (options.Value.EnableServiceSidIsolation)
        {
            WindowsManagedServiceIsolationState.Delete(serviceRootDirectory);
            isolationRegistry.NotifyChanged();
        }

        await using var operationScope = scopeFactory.CreateAsyncScope();
        var provisioners = GetCredentialProvisioners(
            operationScope.ServiceProvider,
            requireAtLeastOne: false);

        foreach (var provisioner in provisioners.Reverse())
        {
            await provisioner.DeprovisionAsync(context, cancellationToken);
        }

        WindowsServicePackageStorage.DeleteDirectoryIfExists(serviceRootDirectory);
    }

    private async ValueTask WaitForAuthenticatedRegistrationAsync(
        string serviceName,
        IReadOnlyDictionary<string, DateTimeOffset> previousInstances,
        DateTimeOffset installationStartedAtUtc,
        CancellationToken cancellationToken)
    {
        var deadline = timeProvider.GetUtcNow() + options.Value.RegistrationTimeout;
        var minimumLastSeen = installationStartedAtUtc - TimeSpan.FromSeconds(1);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var registered = await serviceCatalog
                .GetServicesAsync(serviceName, cancellationToken);

            if (registered.Any(service =>
                {
                    if (service.LastSeenAtUtc < minimumLastSeen)
                    {
                        return false;
                    }

                    return !previousInstances.TryGetValue(
                               service.Descriptor.Identity.InstanceId,
                               out var previousLastSeen)
                           || service.LastSeenAtUtc > previousLastSeen;
                }))
            {
                return;
            }

            var now = timeProvider.GetUtcNow();
            if (now >= deadline)
            {
                throw new TimeoutException(
                    $"Сервис '{serviceName}' был запущен, но не выполнил authenticated registration " +
                    $"в течение {options.Value.RegistrationTimeout}.");
            }

            var remaining = deadline - now;
            var delay = remaining < TimeSpan.FromMilliseconds(200)
                ? remaining
                : TimeSpan.FromMilliseconds(200);

            await Task.Delay(delay, timeProvider, cancellationToken);
        }
    }

    private async ValueTask<bool> IsServiceSafeForCredentialCleanupAsync(string serviceName)
    {
        try
        {
            var currentState = await lifecycleController
                .GetStateAsync(serviceName, CancellationToken.None);

            return currentState is ManagedServiceState.Unknown
                or ManagedServiceState.Stopped;
        }
        catch
        {
            return false;
        }
    }

    private async ValueTask TryStopAndDeleteServiceAsync(string serviceName)
    {
        try
        {
            var currentState = await lifecycleController
                .GetStateAsync(serviceName, CancellationToken.None);

            if (currentState is ManagedServiceState.Running
                or ManagedServiceState.Starting
                or ManagedServiceState.Stopping
                or ManagedServiceState.Paused)
            {
                await lifecycleController.StopAsync(serviceName, CancellationToken.None);
            }
        }
        catch
        {
        }

        await WindowsServiceControlCommands.TryDeleteAsync(serviceName);
    }

    private void EnsureOwnedService(string serviceName)
    {
        var serviceRoot = WindowsServicePackageStorage.GetServiceDirectory(
            options.Value.InstallRoot, serviceName);
        if (!Directory.Exists(serviceRoot))
        {
            throw new InvalidOperationException(
                "Refusing to modify a Windows service that has no ApiKit installation directory.");
        }

        if (options.Value.EnableServiceSidIsolation)
        {
            var isolation = WindowsManagedServiceIsolationState.TryLoad(serviceRoot);
            if (isolation is null ||
                !string.Equals(isolation.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Refusing to modify a Windows service without a matching ApiKit isolation record.");
            }
        }
    }

    private IReadOnlyList<IManagedServiceCredentialProvisioner> GetCredentialProvisioners(
        IServiceProvider services,
        bool requireAtLeastOne)
    {
        var provisioners = services
            .GetServices<IManagedServiceCredentialProvisioner>()
            .ToArray();

        if (provisioners.Length == 0 && requireAtLeastOne)
        {
            throw new InvalidOperationException(
                "Установка сервиса запрещена: не зарегистрирован IManagedServiceCredentialProvisioner. " +
                "Отключите RequireCredentialProvisioner только если security bootstrap выполняется внешней системой.");
        }

        return provisioners;
    }

    private static async ValueTask<IReadOnlyList<Exception>> RollbackProvisioningAsync(
        IReadOnlyList<IManagedServiceCredentialProvisioner> provisioners,
        ManagedServiceCredentialProvisioningContext context,
        CancellationToken cancellationToken)
    {
        var deprovisioningContext = new ManagedServiceCredentialDeprovisioningContext(
            context.Manifest.ServiceName,
            context.ManagementHostIdentity,
            context.ServiceRootDirectory);
        var errors = new List<Exception>();

        for (var index = provisioners.Count - 1; index >= 0; index--)
        {
            try
            {
                await provisioners[index]
                    .DeprovisionAsync(deprovisioningContext, cancellationToken);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        return errors;
    }

    private async ValueTask VerifyPackageAsync(
        IServiceProvider services,
        ManagedServicePackageManifest manifest,
        string packageDirectory,
        CancellationToken cancellationToken)
    {
        var verifiers = services
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
