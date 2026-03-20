# Why Capabilities?

Cocoar.Capabilities is a **composition layer for typed metadata**. It is not a DI container, plugin framework, or ECS — it solves a specific problem: attaching and querying typed metadata across assembly boundaries without coupling.

## The Cross-Assembly Metadata Problem

When building fluent APIs that span multiple assemblies, attaching metadata to builder objects becomes a fundamental challenge. A core assembly defines a builder, extension assemblies need to attach metadata to it, and a consuming assembly needs to read all attached metadata — without circular dependencies.

```csharp
// Core assembly — defines the builder
public class ConfigBuilder<T> { /* ... */ }

// DI assembly — wants to tag builders with DI metadata
public static ConfigBuilder<T> AsSingleton<T>(this ConfigBuilder<T> b) { /* ??? */ }

// Host assembly — needs to read DI metadata from builders
foreach (var builder in builders)
{
    // How do we get the DI metadata that was attached?
}
```

## Why Standard Solutions Fail

| Approach | Problem |
|----------|---------|
| `Dictionary<string, object>` | No type safety, runtime key collisions, no discoverability |
| `[Attribute]` | Static, compile-time only — can't attach dynamically from extensions |
| Method parameters | API explosion, breaks every caller when adding new metadata |
| Builder internal state | Core assembly must know about all possible metadata — defeats extensibility |

## The Capability Composition Approach

Cocoar.Capabilities introduces a **composition layer** that sits alongside your objects. Any assembly can attach typed metadata ("capabilities") to any object through a shared `CapabilityScope`, and any assembly can read them back — all without coupling.

```csharp
// Core assembly — builder uses a shared scope
public class ConfigBuilder<T>
{
    internal CapabilityScope Scope { get; }

    public ConfigBuilder(CapabilityScope scope)
    {
        Scope = scope;
        scope.Compose(this).Build(); // Create initial composition
    }
}

// DI assembly — attaches DI metadata via extension method
public static ConfigBuilder<T> AsSingleton<T>(this ConfigBuilder<T> b)
{
    b.Scope.Recompose(b.Scope.Compositions.GetRequired(b))
        .Add(new ServiceLifetimeCapability(ServiceLifetime.Singleton))
        .Build();
    return b;
}

// Host assembly — reads metadata from any builder
foreach (var builder in builders)
{
    var composition = scope.Compositions.GetRequired(builder);
    if (composition.TryGetFirst<ServiceLifetimeCapability>(out var lifetime))
    {
        // Register with the appropriate lifetime
    }
}
```

This is exactly how [Cocoar.Configuration](https://github.com/cocoar-dev/Cocoar.Configuration) works in production — capability composition solves the cross-assembly metadata problem that every fluent API framework eventually faces.

## What You Get

| Feature | Benefit |
|---------|---------|
| Type-safe composition | No string keys, no casting — compile-time checked |
| Cross-assembly extensibility | Any assembly can attach capabilities without knowing about others |
| Immutable results | Thread-safe, shareable compositions |
| Fluent API | Chain `.Add()`, `.WithPrimary()`, `.Build()` naturally |
| Ordering | Control execution order when multiple capabilities exist |
| Recomposition | Modify compositions without mutation |
| No circular dependencies | Extensions don't need to reference each other |
