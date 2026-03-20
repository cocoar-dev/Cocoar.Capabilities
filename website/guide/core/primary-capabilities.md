# Primary Capabilities

A primary capability represents the "identity" or "self-description" of a composition. While a composition can hold many capabilities, at most one can be primary.

## Defining a Primary Capability

Implement the `IPrimaryCapability` marker interface:

```csharp
public record ServiceIdentity(string Name, string Version) : IPrimaryCapability;

public record PluginMetadata(string Id, string Author) : IPrimaryCapability;
```

## Setting a Primary

Use `WithPrimary()` on the `Composer`:

```csharp
scope.Compose("auth-service")
    .WithPrimary(new ServiceIdentity("AuthService", "2.0.0"))
    .Add(new LoggingCapability())
    .Add(new MetricsCapability())
    .Build();
```

Rules:
- Only one primary per composition
- Calling `WithPrimary()` again replaces the previous one
- Pass `null` to clear the primary

## Querying the Primary

```csharp
var composition = scope.Compositions.GetRequired<string>("auth-service");

// Check
if (composition.HasPrimary())
{
    var primary = composition.GetPrimary();
}

// Typed access
if (composition.HasPrimary<ServiceIdentity>())
{
    var identity = composition.GetRequiredPrimaryAs<ServiceIdentity>();
    Console.WriteLine($"{identity.Name} v{identity.Version}");
}

// Try-pattern
if (composition.TryGetPrimaryAs<ServiceIdentity>(out var id))
{
    Console.WriteLine(id.Name);
}

// Default
var idOrNull = composition.GetPrimaryOrDefaultAs<ServiceIdentity>();
```

## Replacing via Recomposition

```csharp
var existing = scope.Compositions.GetRequired<string>("auth-service");
scope.Recompose(existing)
    .WithPrimary(new ServiceIdentity("AuthService", "3.0.0"))
    .Build();
```

## When to Use

Use primary capabilities when a composition needs an identity:
- **Service metadata**: Name, version, description
- **Plugin self-description**: ID, author, dependencies
- **Entity classification**: Type, category, role

If you just need to query capabilities by type, regular capabilities (via `Add()`) are sufficient.
