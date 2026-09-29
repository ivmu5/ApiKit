using System.Text;
using ApiKit.Management.Models;

namespace ApiKit.Management.Windows.Internal;

internal static class WindowsNamedPipeNames
{
    public static string GetServicePipeName(
        string prefix,
        ManagementServiceIdentity identity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentNullException.ThrowIfNull(identity);

        return $"{Normalize(prefix)}.{Normalize(identity.ServiceName)}.{Normalize(identity.InstanceId)}";
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Trim())
        {
            builder.Append(char.IsLetterOrDigit(character) || character is '.' or '-' or '_'
                ? character
                : '-');
        }

        return builder.ToString();
    }
}
