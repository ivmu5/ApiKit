using System.Diagnostics;
using ApiKit.Management.Lifecycle;

namespace ApiKit.Management.Windows.Internal;

/// <summary>
/// Invokes Windows SCM through sc.exe without shell evaluation of user-supplied arguments.
/// </summary>
internal static class WindowsServiceControlCommands
{
    public static Task CreateAsync(
        ManagedServicePackageManifest manifest,
        string entryPoint,
        string serviceAccount,
        CancellationToken cancellationToken) =>
        RunAsync(
            [
                "create",
                manifest.ServiceName,
                "binPath=", BuildBinaryPath(entryPoint, manifest.Arguments),
                "start=", "auto",
                "obj=", serviceAccount,
                "displayname=", manifest.DisplayName
            ],
            cancellationToken);

    public static Task ConfigureAsync(
        ManagedServicePackageManifest manifest,
        string entryPoint,
        string serviceAccount,
        CancellationToken cancellationToken) =>
        RunAsync(
            [
                "config",
                manifest.ServiceName,
                "binPath=", BuildBinaryPath(entryPoint, manifest.Arguments),
                "obj=", serviceAccount,
                "displayname=", manifest.DisplayName
            ],
            cancellationToken);

    public static Task ConfigureDependenciesAsync(
        ManagedServicePackageManifest manifest,
        CancellationToken cancellationToken)
    {
        var dependencies = manifest.Dependencies.Count == 0
            ? string.Empty
            : string.Join('/', manifest.Dependencies);

        return RunAsync(
            ["config", manifest.ServiceName, "depend=", dependencies],
            cancellationToken);
    }


    public static Task ConfigureUnrestrictedServiceSidAsync(
        string serviceName,
        CancellationToken cancellationToken) =>
        RunAsync(["sidtype", serviceName, "unrestricted"], cancellationToken);

    public static Task SetDescriptionAsync(
        string serviceName,
        string description,
        CancellationToken cancellationToken) =>
        RunAsync(["description", serviceName, description], cancellationToken);

    public static Task DeleteAsync(
        string serviceName,
        CancellationToken cancellationToken) =>
        RunAsync(["delete", serviceName], cancellationToken);

    public static async Task TryDeleteAsync(string serviceName)
    {
        try
        {
            await DeleteAsync(serviceName, CancellationToken.None);
        }
        catch
        {
        }
    }

    private static string BuildBinaryPath(string entryPoint, IReadOnlyList<string> arguments)
    {
        var parts = new List<string> { QuoteArgument(entryPoint) };
        parts.AddRange(arguments.Select(QuoteArgument));
        return string.Join(' ', parts);
    }

    private static string QuoteArgument(string value)
    {
        if (value.Length == 0)
        {
            return "\"\"";
        }

        if (!value.Any(static ch => char.IsWhiteSpace(ch) || ch == '\"'))
        {
            return value;
        }

        var builder = new System.Text.StringBuilder(value.Length + 2);
        builder.Append('\"');
        var backslashes = 0;

        foreach (var character in value)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            if (character == '\"')
            {
                builder.Append('\\', backslashes * 2 + 1);
                builder.Append('\"');
                backslashes = 0;
                continue;
            }

            builder.Append('\\', backslashes);
            backslashes = 0;
            builder.Append(character);
        }

        builder.Append('\\', backslashes * 2);
        builder.Append('\"');
        return builder.ToString();
    }

    private static async Task RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "sc.exe"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Не удалось запустить sc.exe.");

        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A cancelled sc.exe command must not mutate SCM later, after the
            // installer has already started its rollback/recovery path.
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(CancellationToken.None);
                }
            }
            catch (InvalidOperationException)
            {
                // The process exited between the check and the kill request.
            }

            throw;
        }

        var output = await standardOutput;
        var error = await standardError;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"sc.exe завершился с кодом {process.ExitCode}: {error.Trim()} {output.Trim()}".Trim());
        }
    }
}
