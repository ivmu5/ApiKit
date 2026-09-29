using System.IO.Pipes;
using System.Security.Principal;

namespace ApiKit.Management.Windows.Internal;

internal static class WindowsNamedPipeClientConnection
{
    public static async ValueTask<NamedPipeClientStream> ConnectAsync(
        string pipeName,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        var client = new NamedPipeClientStream(
            serverName: ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous,
            TokenImpersonationLevel.Identification);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            await client.ConnectAsync(timeoutSource.Token);
            return client;
        }
        catch
        {
            await client.DisposeAsync();
            throw;
        }
    }
}
