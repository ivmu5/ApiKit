using System.Text;

namespace ApiKit.Authorization.Permissions;

internal static class PermissionPolicyName
{
    private const string ResourcePrefix = "ApiKit.Permission.Resource:";
    private const string LiteralPrefix = "ApiKit.Permission.Value:";
    private const char Separator = ':';

    public static string ForResource(Type resourceType, PermissionOperation operation) =>
        ForResource(resourceType, operation.ToString());

    public static string ForResource(Type resourceType, string operation)
    {
        ArgumentNullException.ThrowIfNull(resourceType);

        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        var typeName = resourceType.AssemblyQualifiedName
            ?? throw new InvalidOperationException(
                $"Не удалось получить имя типа {resourceType}.");

        return string.Concat(
            ResourcePrefix,
            Encode(typeName),
            Separator,
            Encode(operation));
    }

    public static string ForPermission(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return LiteralPrefix + Encode(permission);
    }

    public static bool TryParse(
        string policyName,
        out PermissionPolicyDescriptor descriptor)
    {
        if (policyName.StartsWith(ResourcePrefix, StringComparison.Ordinal))
        {
            var payload = policyName.AsSpan(ResourcePrefix.Length);
            var separatorIndex = payload.IndexOf(Separator);

            if (separatorIndex <= 0 || separatorIndex >= payload.Length - 1)
            {
                descriptor = default;
                return false;
            }

            try
            {
                var typeName = Decode(payload[..separatorIndex].ToString());
                var operation = Decode(payload[(separatorIndex + 1)..].ToString());
                var resourceType = Type.GetType(typeName, throwOnError: false);

                if (resourceType is null || string.IsNullOrWhiteSpace(operation))
                {
                    descriptor = default;
                    return false;
                }

                descriptor = PermissionPolicyDescriptor.ForResource(
                    resourceType,
                    operation);

                return true;
            }
            catch (FormatException)
            {
                descriptor = default;
                return false;
            }
        }

        if (policyName.StartsWith(LiteralPrefix, StringComparison.Ordinal))
        {
            try
            {
                var permission = Decode(policyName[LiteralPrefix.Length..]);

                if (string.IsNullOrWhiteSpace(permission))
                {
                    descriptor = default;
                    return false;
                }

                descriptor = PermissionPolicyDescriptor.ForPermission(permission);
                return true;
            }
            catch (FormatException)
            {
                descriptor = default;
                return false;
            }
        }

        descriptor = default;
        return false;
    }

    private static string Encode(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static string Decode(string value) =>
        Encoding.UTF8.GetString(Convert.FromBase64String(value));
}

internal readonly record struct PermissionPolicyDescriptor(
    Type? ResourceType,
    string? Operation,
    string? Permission)
{
    public static PermissionPolicyDescriptor ForResource(
        Type resourceType,
        string operation) =>
        new(resourceType, operation, null);

    public static PermissionPolicyDescriptor ForPermission(string permission) =>
        new(null, null, permission);
}
