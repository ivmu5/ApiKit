using System.Security.Claims;
using System.Text.Json;
using ApiKit.Management.Models;
using ApiKit.Management.Security;

namespace ApiKit.Management.Windows.Internal;

internal static class WindowsNamedPipeProtocol
{
    public const int Version = 6;
}

internal static class WindowsNamedPipeRequestTypes
{
    public const string Register = "registry.register";
    public const string Withdraw = "registry.withdraw";
    public const string GetServices = "catalog.all";
    public const string GetServicesByName = "catalog.by-name";
    public const string FindService = "catalog.find";
    public const string ExecuteOperation = "operation.execute";
    public const string ExecuteResource = "resource.execute";
    public const string ExecuteServiceOperation = "service.operation.execute";
    public const string ExecuteServiceResource = "service.resource.execute";
    public const string GetServiceState = "service.lifecycle.state";
    public const string StartService = "service.lifecycle.start";
    public const string StopService = "service.lifecycle.stop";
    public const string RestartService = "service.lifecycle.restart";
    public const string InstallService = "service.install";
    public const string UpdateService = "service.update";
    public const string UninstallService = "service.uninstall";
}

internal sealed record WindowsNamedPipeSecurityHello
{
    public int ProtocolVersion { get; init; } = WindowsNamedPipeProtocol.Version;
    public required ManagementPeerIdentity Identity { get; init; }
}

internal sealed record WindowsNamedPipeSecurityChallenge
{
    public int ProtocolVersion { get; init; } = WindowsNamedPipeProtocol.Version;
    public required ManagementChallenge Challenge { get; init; }
}

internal sealed record WindowsNamedPipeSecurityResponse
{
    public int ProtocolVersion { get; init; } = WindowsNamedPipeProtocol.Version;
    public required ManagementChallengeResponse Response { get; init; }
}

internal sealed record WindowsNamedPipeSecurityResult
{
    public int ProtocolVersion { get; init; } = WindowsNamedPipeProtocol.Version;
    public required bool Succeeded { get; init; }
    public ManagementPeerIdentity? Identity { get; init; }
    public string? CredentialId { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

internal sealed record WindowsNamedPipeRequest
{
    public int ProtocolVersion { get; init; } = WindowsNamedPipeProtocol.Version;
    public required string Type { get; init; }
    public JsonElement? Payload { get; init; }
}

internal sealed record WindowsNamedPipeResponse
{
    public int ProtocolVersion { get; init; } = WindowsNamedPipeProtocol.Version;
    public required bool Succeeded { get; init; }
    public JsonElement? Payload { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

internal sealed record WithdrawRegistrationRequest(string InstanceId);
internal sealed record ServiceNameRequest(string ServiceName);
internal sealed record InstanceRequest(string InstanceId);
internal sealed record ManagedServiceNameRequest(string ServiceName);
internal sealed record ManagedServicePackageRequest(
    ApiKit.Management.Lifecycle.ManagedServicePackageManifest Manifest,
    string PackageDirectory);
internal sealed record ExecuteOperationRequest(
    string InstanceId,
    string OperationName,
    JsonElement? Request);

internal sealed record ExecuteResourceRequest(
    string InstanceId,
    string ResourceName,
    ManagementResourceOperation Operation,
    JsonElement? Key,
    JsonElement? Model,
    int Page,
    int PageSize);

internal sealed record ExecuteServiceOperationRequest(
    string OperationName,
    JsonElement? Request,
    IReadOnlyList<SerializedClaim> Claims);

internal sealed record ExecuteServiceResourceRequest(
    string ResourceName,
    ManagementResourceOperation Operation,
    JsonElement? Key,
    JsonElement? Model,
    int Page,
    int PageSize,
    IReadOnlyList<SerializedClaim> Claims);

internal sealed record SerializedClaim(
    string Type,
    string Value,
    string ValueType,
    string Issuer)
{
    public static SerializedClaim FromClaim(Claim claim) => new(
        claim.Type,
        claim.Value,
        claim.ValueType,
        claim.Issuer);

    public Claim ToClaim() => new(Type, Value, ValueType, Issuer);
}
