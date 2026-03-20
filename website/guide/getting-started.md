# Getting Started

Install:

```bash
dotnet add package Cocoar.Capabilities
```

All types are in the `Cocoar.Capabilities` namespace.

## Quick Start: Compose and Query

```csharp
using Cocoar.Capabilities;

// 1. Create a scope
var scope = new CapabilityScope();

// 2. Compose capabilities onto a subject
scope.Compose("user-service")
    .Add(new LoggingCapability { Level = LogLevel.Debug })
    .Add(new RetryCapability { MaxAttempts = 3 })
    .Build();

// 3. Query the composition
var composition = scope.Compositions.GetRequired<string>("user-service");

var logging = composition.GetFirstOrDefault<LoggingCapability>();
Console.WriteLine(logging?.Level); // Debug

var all = composition.GetAll<RetryCapability>();
Console.WriteLine(composition.Has<LoggingCapability>()); // True
```

## Quick Start: Primary Capability

```csharp
public record ServiceIdentity(string Name, string Version) : IPrimaryCapability;

scope.Compose("user-service")
    .WithPrimary(new ServiceIdentity("UserService", "2.1.0"))
    .Add(new LoggingCapability { Level = LogLevel.Debug })
    .Build();

var composition = scope.Compositions.GetRequired<string>("user-service");
var identity = composition.GetRequiredPrimaryAs<ServiceIdentity>();
Console.WriteLine(identity.Name); // UserService
```

## Quick Start: Multiple Contracts

```csharp
public class EmailNotifier : INotifier, IHealthCheck
{
    public string Name => "Email";
    public bool IsHealthy => true;
}

scope.Compose("notifications")
    .AddAs<(INotifier, IHealthCheck)>(new EmailNotifier())
    .Build();

var composition = scope.Compositions.GetRequired<string>("notifications");
var notifiers = composition.GetAll<INotifier>();      // [EmailNotifier]
var checks = composition.GetAll<IHealthCheck>();       // [EmailNotifier]
```

## Key Types

| Type | Role |
|------|------|
| `CapabilityScope` | Container that manages composers and compositions |
| `Composer` | Fluent builder for attaching capabilities to a subject |
| `IComposition` | Immutable, thread-safe result of a composition |
| `IPrimaryCapability` | Marker interface for the "identity" capability |
| `CapabilityScopeOptions` | Configuration for scope behavior |

## Next Steps

- [Why Capabilities?](/guide/why-capabilities) — The problem this solves
- [CapabilityScope](/guide/core/capability-scope) — Deep dive into scopes
- [Composer](/guide/core/composer) — Full builder API
- [Composition](/guide/core/composition) — Query API reference
- [Examples](/reference/examples) — Real-world patterns
