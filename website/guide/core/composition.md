# Composition

An `IComposition` is the immutable, thread-safe result of building capabilities. Once created, it cannot be modified — only replaced via recomposition.

## Querying Capabilities

### GetAll

```csharp
IReadOnlyList<IPlugin> plugins = composition.GetAll<IPlugin>();
IReadOnlyList<object> everything = composition.GetAll();
```

### First and Last

```csharp
var first = composition.GetFirstOrDefault<IPlugin>();   // null if none
var last = composition.GetLastOrDefault<IPlugin>();     // null if none
```

### Required Queries (throw if missing)

```csharp
var first = composition.GetRequiredFirst<IPlugin>();    // throws InvalidOperationException
var last = composition.GetRequiredLast<IPlugin>();      // throws InvalidOperationException
```

### Try-Pattern

```csharp
if (composition.TryGetFirst<IPlugin>(out var plugin))
{
    plugin.Initialize();
}

if (composition.TryGetLast<ILogger>(out var logger))
{
    logger.Log("ready");
}
```

### Has and Count

```csharp
bool hasPlugins = composition.Has<IPlugin>();
int pluginCount = composition.Count<IPlugin>();
```

## Primary Capability Queries

```csharp
// Check existence
bool hasPrimary = composition.HasPrimary();
bool hasTyped = composition.HasPrimary<ServiceIdentity>();

// Get
IPrimaryCapability? primary = composition.GetPrimaryOrDefault();
IPrimaryCapability required = composition.GetPrimary();  // throws if none

// Typed access
if (composition.TryGetPrimaryAs<ServiceIdentity>(out var identity))
{
    Console.WriteLine(identity.Name);
}

var id = composition.GetRequiredPrimaryAs<ServiceIdentity>();  // throws if missing/wrong type
var idOrNull = composition.GetPrimaryOrDefaultAs<ServiceIdentity>();
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Subject` | `object` | The subject this composition belongs to |
| `TotalCapabilityCount` | `int` | Total number of capabilities (across all types) |

## Thread Safety

Compositions are fully immutable after `Build()`. They can be safely shared across threads, cached, passed between components, and stored in concurrent collections without synchronization.
