# ApiKit

English | [Русский](README.ru.md)

The platform-independent .NET 10 library for JWT authentication, ASP.NET Core permissions, EF Core CRUD and management contracts. The main assembly references ASP.NET Core but does not reference Windows APIs or the CryptoKit adapter.

[Repository overview](../README.md) | Libraries: **ApiKit** · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.md) · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.md)

Test projects: [ApiKit.Tests](../ApiKit.Tests/README.md) · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.md) · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.md) · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.md) · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.md) | [CryptoKit](../external/CryptoKit/README.md)

## Contents

- [Capabilities](#capabilities)
- [Architecture](#architecture)
- [Public API](#public-api)
- [Lifecycle and execution](#lifecycle-and-execution)
- [Testing](#testing)
- [Important behavior](#important-behavior)
- [Modification and extension points](#modification-and-extension-points)
- [Dependencies](#dependencies)

## Capabilities

- Configure JWT issuance and bearer validation with HMAC keys or replaceable signing/validation providers.
- Enforce literal or resource-scoped permissions through ordinary ASP.NET Core policies.
- Build DTO-based EF Core CRUD controllers with pagination and System.Text.Json JSON Patch.
- Register PostgreSQL `DbContext` services through EF Core/Npgsql.
- Advertise services, discover HTTP endpoints and CRUD resources, and register custom management operations without depending on a particular transport.

## Architecture

The public surface is grouped by responsibility: [`Authentication`](Authentication/DependencyInjection/JwtServiceCollectionExtensions.cs) manages JWT services; [`Authorization`](Authorization/DependencyInjection/AuthorizationServiceCollectionExtensions.cs) integrates ASP.NET Core policy authorization; [`CrudController`](Crud/Controllers/CrudController.cs) implements reusable MVC actions; [`Data`](Data/DependencyInjection/PostgreSqlServiceCollectionExtensions.cs) configures Npgsql; [`Management`](Management/DependencyInjection/ManagementServiceCollectionExtensions.cs) supplies transport-neutral discovery, dispatch, challenge and installer contracts.

```text
HTTP request → ASP.NET Core JWT middleware → authorization policy
  → concrete CrudController action → EF Core DbContext → DTO/HTTP response

Management resource call → ManagementResourceDispatcher
  → ASP.NET Core MVC action invoker → ordinary authorization and CRUD pipeline
```

<details>
<summary>Management discovery and the MVC bridge</summary>

[`DefaultManagementServiceDescriptorProvider`](Management/Internal/DefaultManagementServiceDescriptorProvider.cs) combines contributions from HTTP endpoints, server addresses, CRUD metadata and registered operations. [`ManagementRegistrationHostedService`](Management/Internal/ManagementRegistrationHostedService.cs) publishes descriptors and heartbeats to registered publishers. [`ManagementResourceDispatcher`](Management/Internal/ManagementResourceDispatcher.cs) invokes real MVC actions rather than duplicating their business logic. It requires an authenticated administrative principal before injecting a narrowly scoped synthetic CRUD permission; MVC authorization still evaluates. [`InMemoryManagementChallengeProvider`](Management/Internal/InMemoryManagementChallengeProvider.cs) issues and consumes single-use challenges, while concrete signatures are supplied by an adapter.

</details>

## Public API

Register only the required services in an ASP.NET Core application's startup code:

```csharp
using ApiKit.Authentication;
using ApiKit.Authorization;
using ApiKit.Crud.Authorization;
using ApiKit.Data;

builder.Services.AddApiKitJwtHmacKeyProvider(builder.Configuration);
builder.Services.AddApiKitJwtTokenGeneration(builder.Configuration);
builder.Services.AddApiKitJwtBearerAuthentication(builder.Configuration);
builder.Services.AddApiKitAuthorization();
builder.Services.AddApiKitPostgreSqlDbContext<AppDbContext>(builder.Configuration);
builder.Services.AddControllers().AddApiKitCrudPermissions();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

`AppDbContext` is your application type, not part of ApiKit. Supply `ConnectionStrings:DefaultConnection`, `Jwt:Generation`, `Jwt:Validation` and `Jwt:Hmac` for this example. The provider contracts [`IJwtTokenGenerator`](Authentication/Abstractions/IJwtTokenGenerator.cs), [`IJwtSigningCredentialsProvider`](Authentication/Abstractions/IJwtSigningCredentialsProvider.cs) and [`IJwtValidationKeysProvider`](Authentication/Abstractions/IJwtValidationKeysProvider.cs) allow key issuance and validation to be separated.

<details>
<summary>Generic CRUD and management registration</summary>

```csharp
using ApiKit.Crud.Controllers;

public sealed class ItemsController
    : CrudController<AppDbContext, Item, Guid, ItemReadDto, ItemCreateDto, ItemUpdateDto>
{
    public ItemsController(AppDbContext dbContext) : base(dbContext) { }

    // Override GetReadProjection/MapReadModelAsync and the create/update mapping hooks.
}
```

Implement the model-mapping overrides relevant to the inherited actions. To attach resource permissions automatically, use `AddApiKitCrudPermissions()` alongside `AddApiKitAuthorization()`. Explicit action authorization attributes take precedence over automatic CRUD conventions.

```csharp
using ApiKit.Management;
using ApiKit.Management.Builders;

var management = services.AddApiKitManagement(options =>
{
    options.ServiceName = "messages";
    options.DiscoverCrudResources = true;
});
management.AddCapability("messages");
```

The management builder also provides `AddOperation<TOperation, TRequest, TResult>` and `AddPublisher<TPublisher>`. Register a transport and cryptographic credentials separately when communicating outside the process.

</details>

## Lifecycle and execution

JWT options are validated during application startup. EF Core context lifetimes default to scoped; the generator is transient. Management service identity and its descriptor are resolved by registered services; the hosted registration service owns periodic heartbeats and withdrawal. The in-memory challenge provider is singleton and uses bounded, expiring pending challenges. Most CRUD and dispatcher operations are asynchronous and accept `CancellationToken`; application code must handle its own DbContext concurrency and persistence boundaries.

## Testing

The companion [ApiKit.Tests](../ApiKit.Tests/README.md) project tests authorization policy behavior, MVC bridge permission boundaries, one-time challenge consumption, expiry, identity binding and canonical encoding. Run from the repository root:

```powershell
dotnet test ApiKit.Tests/ApiKit.Tests.csproj -c Release
```

These tests do not exercise Windows transport or external RSA storage; see the adapter and Windows test projects for those integrations.

## Important behavior

`CrudController` supports a single EF Core primary key in its default routes and mapping; override relevant hooks or actions for other schemas. JSON Patch operates on update models and must be constrained by application validation. The built-in HMAC option requires at least 32 UTF-8 bytes; never commit production secrets. Management contracts do not grant transport security by themselves, and management CRUD is still subject to the ASP.NET MVC authorization pipeline.

## Modification and extension points

| Change | Start with |
|---|---|
| JWT generation/validation | [JwtTokenGenerator](Authentication/Services/JwtTokenGenerator.cs), [JWT DI](Authentication/DependencyInjection/JwtServiceCollectionExtensions.cs) |
| Permissions and claim names | [Authorization DI](Authorization/DependencyInjection/AuthorizationServiceCollectionExtensions.cs), [PermissionNameResolver](Authorization/Permissions/PermissionNameResolver.cs) |
| CRUD mapping/query/patch | [CrudController.Mapping](Crud/Controllers/CrudController.Mapping.cs), [CrudController.Query](Crud/Controllers/CrudController.Query.cs), [CrudController.Patch](Crud/Controllers/CrudController.Patch.cs) |
| Resource discovery/dispatch | [CrudResourceDescriptorContributor](Management/Internal/CrudResourceDescriptorContributor.cs), [ManagementResourceDispatcher](Management/Internal/ManagementResourceDispatcher.cs) |
| Custom operations/transport contracts | [ApiKitManagementBuilder](Management/Builders/ApiKitManagementBuilder.cs), [IManagementPeerAuthenticator](Management/Abstractions/IManagementPeerAuthenticator.cs) |

## Dependencies

This project directly references ASP.NET Core, EF Core, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.JsonPatch.SystemTextJson` and `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`; the precise versions are in [ApiKit.csproj](ApiKit.csproj). See [repository dependency and license notes](../README.md#dependencies-and-third-party-licenses).
