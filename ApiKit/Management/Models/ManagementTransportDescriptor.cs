namespace ApiKit.Management.Models;

/// <summary>
/// Describes the transport capabilities and addressing of a management endpoint.
/// </summary>
/// <param name="Transport">The transport value.</param>
/// <param name="Endpoint">The endpoint value.</param>
public sealed record ManagementTransportDescriptor(
    string Transport,
    string Endpoint)
{
    /// <summary>
    /// Gets or sets purpose.
    /// </summary>
    public string? Purpose { get; init; }

    /// <summary>
    /// Gets or sets metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();
}
