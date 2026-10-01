using ApiKit.Management.CryptoKit.Options;
using ApiKit.Management.CryptoKit.Provisioning;

namespace ApiKit.Management.CryptoKit.Internal;

internal sealed class CryptoKitManagedServiceProvisioningOptionsSnapshot
{
    internal CryptoKitManagedServiceProvisioningOptionsSnapshot(
        CryptoKitManagedServiceProvisioningOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ServiceRsaKeySize < 2048)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "ServiceRsaKeySize должен быть не меньше 2048 бит.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(options.CryptoKitDirectoryName);

        CryptoKitManagementProvisioningPaths.ValidateDirectoryName(
            options.CryptoKitDirectoryName);

        ServiceRsaKeySize = options.ServiceRsaKeySize;
        CryptoKitDirectoryName = options.CryptoKitDirectoryName;
    }

    internal int ServiceRsaKeySize { get; }

    internal string CryptoKitDirectoryName { get; }
}
