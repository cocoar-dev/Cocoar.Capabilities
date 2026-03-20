# Recomposition

Since compositions are immutable, you can't modify them after `Build()`. Recomposition creates a new composition based on an existing one, inheriting its capabilities while allowing additions, removals, and primary replacement.

## Basic Recomposition

```csharp
// Original composition
scope.Compose("service")
    .Add(new LoggingCapability())
    .Build();

// Recompose — add more capabilities
var existing = scope.Compositions.GetRequired<string>("service");
scope.Recompose(existing)
    .Add(new MetricsCapability())
    .Build();

// New composition has both LoggingCapability and MetricsCapability
```

The new composition replaces the old one in the registry.

## Removing Capabilities

Use `RemoveWhere()` to selectively exclude capabilities:

```csharp
var existing = scope.Compositions.GetRequired<string>("service");
scope.Recompose(existing)
    .RemoveWhere(c => c is LoggingCapability)
    .Add(new BetterLoggingCapability())
    .Build();
```

## Replacing the Primary

```csharp
var existing = scope.Compositions.GetRequired<string>("service");
scope.Recompose(existing)
    .WithPrimary(new ServiceIdentity("Service", "2.0.0"))
    .Build();
```

## Conditional Enrichment

A common pattern is enriching compositions from extension methods:

```csharp
public static void EnrichWithDiagnostics(this CapabilityScope scope, object subject)
{
    if (scope.Compositions.TryGet(subject, out var composition))
    {
        scope.Recompose(composition)
            .Add(new DiagnosticsCapability())
            .Build();
    }
}
```
