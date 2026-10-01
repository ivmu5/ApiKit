using ApiKit.Management.Lifecycle;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i managed service package verifier.
/// </summary>
public interface IManagedServicePackageVerifier
{
    /// <summary>
    /// Verifies async.
    /// </summary>
    /// <param name="manifest">The service package manifest.</param>
    /// <param name="packageDirectory">The directory containing the package.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<bool> VerifyAsync(
        ManagedServicePackageManifest manifest,
        string packageDirectory,
        CancellationToken cancellationToken = default);
}
