namespace ApiKit.Management.Windows.Tests.Support;

/// <summary>Windows-specific tests produce a real xUnit skip, not a false positive.</summary>
public sealed class WindowsFactAttribute : Xunit.FactAttribute
{
    public WindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows()) Skip = "Requires Windows Named Pipes / Windows security APIs.";
    }
}
