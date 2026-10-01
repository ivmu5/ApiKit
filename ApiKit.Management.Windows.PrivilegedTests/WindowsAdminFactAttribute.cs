using System.Security.Principal;

namespace ApiKit.Management.Windows.PrivilegedTests;

/// <summary>
/// SCM/ACL tests are discoverable but never execute without an explicit,
/// distinct administrative opt-in AND an elevated Windows token.
/// </summary>
public sealed class WindowsAdminFactAttribute : Xunit.FactAttribute
{
    public WindowsAdminFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Windows SCM is required.";
        }
        else if (!string.Equals(
                     Environment.GetEnvironmentVariable("APIKIT_RUN_WINDOWS_ADMIN_TESTS"),
                     "I_ACCEPT_SCM_AND_ACL_CHANGES", StringComparison.Ordinal))
        {
            Skip = "Explicit opt-in: set APIKIT_RUN_WINDOWS_ADMIN_TESTS=I_ACCEPT_SCM_AND_ACL_CHANGES.";
        }
        else
        {
            using var identity = WindowsIdentity.GetCurrent();
            if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            {
                Skip = "Elevation / Windows Administrators token required.";
            }
        }
    }
}
