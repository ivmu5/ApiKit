using System.Text.Json;
using ApiKit.Management.Models;

namespace ApiKit.Management.Internal;

internal interface IManagementOperationRegistration
{
    ManagementOperationDescriptor Descriptor { get; }

    ValueTask<JsonElement?> ExecuteAsync(
        IServiceProvider serviceProvider,
        JsonElement? request,
        CancellationToken cancellationToken);
}
