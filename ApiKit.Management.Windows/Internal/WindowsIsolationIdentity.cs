using System.Security.Principal;

namespace ApiKit.Management.Windows.Internal;

/// <summary>
/// Resolves the current Windows management host isolation SID.
/// </summary>
internal static class WindowsIsolationIdentity
{
    public static SecurityIdentifier GetCurrentIsolationSid()
    {
        using var identity = WindowsIdentity.GetCurrent();

        var serviceSids = identity.Groups?
            .OfType<SecurityIdentifier>()
            .Where(static sid =>
                sid.Value.StartsWith("S-1-5-80-", StringComparison.OrdinalIgnoreCase)
                && !sid.Value.Equals("S-1-5-80-0", StringComparison.OrdinalIgnoreCase))
            .ToArray()
            ?? [];

        if (serviceSids.Length == 1)
        {
            return serviceSids[0];
        }

        return identity.User
            ?? throw new InvalidOperationException("Не удалось определить SID текущей Windows identity.");
    }
}
