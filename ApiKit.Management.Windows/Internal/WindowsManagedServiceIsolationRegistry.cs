using System.Security.Principal;
using ApiKit.Management.Windows.Options;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Windows.Internal;

/// <summary>
/// Tracks provisioned service SIDs and signals when the registration pipe must renew its ACL.
/// </summary>
internal sealed class WindowsManagedServiceIsolationRegistry(
    IOptions<WindowsServiceManagementOptions> options)
{
    private readonly object _syncRoot = new();
    private TaskCompletionSource<bool> _changed = CreateSignal();
    private long _version;

    public long Version => Interlocked.Read(ref _version);

    public IReadOnlyCollection<string> GetProvisionedServiceSids()
    {
        var installRoot = Path.GetFullPath(options.Value.InstallRoot);
        if (!Directory.Exists(installRoot))
        {
            return [];
        }

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var serviceDirectory in Directory.EnumerateDirectories(installRoot))
        {
            var directoryName = Path.GetFileName(serviceDirectory);
            if (string.Equals(directoryName, ".staging", StringComparison.OrdinalIgnoreCase)
                || string.Equals(directoryName, ".management", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var state = WindowsManagedServiceIsolationState.TryLoad(serviceDirectory);
                if (state is null || !string.Equals(
                        state.ServiceName, directoryName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var sid = new SecurityIdentifier(state.ServiceSid);
                result.Add(sid.Value);
            }
            catch
            {
            }
        }

        return result.ToArray();
    }

    public void NotifyChanged()
    {
        TaskCompletionSource<bool> previous;

        lock (_syncRoot)
        {
            Interlocked.Increment(ref _version);
            previous = _changed;
            _changed = CreateSignal();
        }

        previous.TrySetResult(true);
    }

    public Task WaitForChangeAsync(long observedVersion, CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            if (_version != observedVersion)
            {
                return Task.CompletedTask;
            }

            return _changed.Task.WaitAsync(cancellationToken);
        }
    }

    private static TaskCompletionSource<bool> CreateSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
