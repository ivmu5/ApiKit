using ApiKit.Management.Models;

namespace ApiKit.Management.Builders;

/// <summary>
/// Builds the published service descriptor from discovery contributors.
/// </summary>
public sealed class ManagementServiceDescriptorBuilder
{
    private readonly HashSet<string> _addresses = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, ManagementTransportDescriptor> _transports =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, ManagementEndpointDescriptor> _endpoints =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, ManagementResourceDescriptor> _resources =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, ManagementOperationDescriptor> _operations =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _capabilities = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _metadata = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new ManagementServiceDescriptorBuilder instance.
    /// </summary>
    public ManagementServiceDescriptorBuilder(ManagementServiceIdentity identity)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
    }

    /// <summary>
    /// Gets identity.
    /// </summary>
    public ManagementServiceIdentity Identity { get; }

    /// <summary>
    /// Registers address.
    /// </summary>
    public void AddAddress(string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        _addresses.Add(address.Trim());
    }

    /// <summary>
    /// Registers transport.
    /// </summary>
    public void AddTransport(ManagementTransportDescriptor transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentException.ThrowIfNullOrWhiteSpace(transport.Transport);
        ArgumentException.ThrowIfNullOrWhiteSpace(transport.Endpoint);

        var key = string.Join("|", transport.Transport, transport.Purpose, transport.Endpoint);
        _transports[key] = transport;
    }

    /// <summary>
    /// Registers endpoint.
    /// </summary>
    public void AddEndpoint(ManagementEndpointDescriptor endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var key = string.Join(
            "|",
            endpoint.Name,
            endpoint.Route,
            string.Join(',', endpoint.HttpMethods));

        _endpoints[key] = endpoint;
    }

    /// <summary>
    /// Registers resource.
    /// </summary>
    public void AddResource(ManagementResourceDescriptor resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        _resources[resource.Name] = resource;
    }

    /// <summary>
    /// Registers operation.
    /// </summary>
    public void AddOperation(ManagementOperationDescriptor operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _operations[operation.Name] = operation;
    }

    /// <summary>
    /// Registers capability.
    /// </summary>
    public void AddCapability(string capability)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capability);
        _capabilities.Add(capability.Trim());
    }

    /// <summary>
    /// Sets metadata.
    /// </summary>
    public void SetMetadata(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        _metadata[key.Trim()] = value;
    }

    /// <summary>
    /// Builds the final validated management service descriptor.
    /// </summary>
    public ManagementServiceDescriptor Build() => new()
    {
        Identity = Identity,
        Addresses = _addresses
            .OrderBy(static address => address)
            .Select(static address => new ManagementServiceAddress(address))
            .ToArray(),
        Transports = _transports.Values
            .OrderBy(transport => transport.Transport)
            .ThenBy(transport => transport.Purpose)
            .ThenBy(transport => transport.Endpoint)
            .ToArray(),
        Endpoints = _endpoints.Values
            .OrderBy(endpoint => endpoint.GroupName)
            .ThenBy(endpoint => endpoint.Route)
            .ThenBy(endpoint => endpoint.Name)
            .ToArray(),
        Resources = _resources.Values
            .OrderBy(resource => resource.GroupName)
            .ThenBy(resource => resource.Name)
            .ToArray(),
        Operations = _operations.Values
            .OrderBy(operation => operation.GroupName)
            .ThenBy(operation => operation.Name)
            .ToArray(),
        Capabilities = _capabilities.OrderBy(value => value).ToArray(),
        Metadata = new Dictionary<string, string>(_metadata, StringComparer.OrdinalIgnoreCase)
    };
}
