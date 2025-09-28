# API Reference

Complete reference for all public APIs in Cocoar.Capabilities.

## Package Architecture

Cocoar.Capabilities is available in two packages:

- **`Cocoar.Capabilities.Core`** - Core functionality only (maximum performance)
- **`Cocoar.Capabilities`** - Includes Core + Registry functionality (convenience features)

**Registry-specific APIs** (marked with 📋) are only available in the full `Cocoar.Capabilities` package. All other APIs are available in both packages.

## Core Interfaces

### ICapability

Base marker interface for all capabilities.

```csharp
public interface ICapability { }
```

### ICapability&lt;in TSubject&gt;

Generic capability interface that defines a capability for a specific subject type.

```csharp
public interface ICapability<in TSubject> : ICapability { }
```

**Usage**:
```csharp
public record LoggingCapability<T>(LogLevel Level) : ICapability<T>;
public record CachingCapability<T>(TimeSpan Duration) : ICapability<T>;
```

### IPrimaryCapability&lt;in T&gt;

Marker interface for primary capabilities. Only one primary capability can be registered per subject.

```csharp
public interface IPrimaryCapability<in T> : ICapability<T> { }
```

**Usage**:
```csharp
public record DatabasePrimaryCapability<T> : IPrimaryCapability<T>;

// Only one primary capability allowed per subject
```

## Registry Extensions 📋

### BuildAndRegister&lt;TSubject&gt;

**Package**: `Cocoar.Capabilities` (Registry package only)

Builds the composition and automatically registers it globally for discovery.

```csharp
public static IComposition<TSubject> BuildAndRegister<TSubject>(this Composer<TSubject> composer) 
    where TSubject : notnull
```

**Usage**:
```csharp
// Build and register in one step
var composition = Composer.For(userService)
    .Add(new LoggingCapability<UserService>())
    .BuildAndRegister(); // Available globally via Composition.FindOrDefault

// Equivalent to:
var composition = composer.Build();
CompositionRegistryCore.Register(composition);
```
composer.WithPrimary(new DatabasePrimaryCapability<UserService>());
```

### IOrderedCapability

Interface for capabilities that need specific ordering within their type group.

```csharp
public interface IOrderedCapability
{
    int Order { get; }
}
```

**Usage**:
```csharp
public record OrderedMiddleware<T>(int Priority) : ICapability<T>, IOrderedCapability
{
    public int Order => Priority; // Lower values execute first
}
```

## Core Types

### IComposition

Non-generic interface for accessing basic composition information.

```csharp
public interface IComposition
{
    object Subject { get; }
    int TotalCapabilityCount { get; }
}
```

### IComposition&lt;TSubject&gt;

Generic interface for typed access to capabilities attached to a subject.

```csharp
public interface IComposition<TSubject> : IComposition
{
    new TSubject Subject { get; }

    // Primary capability methods
    bool HasPrimary();
    bool HasPrimary<TPrimaryCapability>() where TPrimaryCapability : class, IPrimaryCapability<TSubject>;
    bool TryGetPrimary(out IPrimaryCapability<TSubject> primary);
    IPrimaryCapability<TSubject>? GetPrimaryOrDefault();
    IPrimaryCapability<TSubject> GetPrimary();
    bool TryGetPrimaryAs<TPrimaryCapability>(out TPrimaryCapability primary) where TPrimaryCapability : class, IPrimaryCapability<TSubject>;
    TPrimaryCapability? GetPrimaryOrDefaultAs<TPrimaryCapability>() where TPrimaryCapability : class, IPrimaryCapability<TSubject>;
    TPrimaryCapability GetRequiredPrimaryAs<TPrimaryCapability>() where TPrimaryCapability : class, IPrimaryCapability<TSubject>;

    // Capability query methods
    IReadOnlyList<TCapability> GetAll<TCapability>() where TCapability : class, ICapability<TSubject>;
    IReadOnlyList<ICapability<TSubject>> GetAll();
    bool Has<TCapability>() where TCapability : class, ICapability<TSubject>;
    int Count<TCapability>() where TCapability : class, ICapability<TSubject>;
}
```

## Builder API

### Composer Static Class

Entry point for creating capability composers.

```csharp
public static class Composer
{
    // Create composer for subject
    public static Composer<TSubject> For<TSubject>(TSubject subject) where TSubject : notnull;
    
    // 📋 Find existing composer (Registry package only)
    public static bool TryFind<TSubject>(TSubject subject, out Composer<TSubject> composer) where TSubject : notnull;
    public static Composer<TSubject>? FindOrDefault<TSubject>(TSubject subject) where TSubject : notnull;
    public static Composer<TSubject> FindRequired<TSubject>(TSubject subject) where TSubject : notnull;
    
    // Recomposition
    public static Composer<TSubject> Recompose<TSubject>(IComposition<TSubject> existingComposition) where TSubject : notnull;
}
```

### Composer&lt;TSubject&gt;

Fluent builder for capability registration.

```csharp
public sealed class Composer<TSubject> where TSubject : notnull
{
    public TSubject Subject { get; }

    // Basic registration
    public Composer<TSubject> Add(ICapability<TSubject> capability);
    
    // Contract registration
    public Composer<TSubject> AddAs<TContract>(ICapability<TSubject> capability);
    
    // Conditional registration
    public Composer<TSubject> TryAdd<TCapability>(TCapability capability) where TCapability : class, ICapability<TSubject>;
    public Composer<TSubject> TryAddAs<TContract>(ICapability<TSubject> capability) where TContract : class, ICapability<TSubject>;
    
    // Capability removal
    public Composer<TSubject> RemoveWhere(Func<ICapability<TSubject>, bool> predicate);
    
    // Primary capability management
    public Composer<TSubject> WithPrimary(IPrimaryCapability<TSubject>? primary);
    
    // Query builder state
    public bool HasPrimary();
    public bool Has<TCapability>() where TCapability : class, ICapability<TSubject>;
    
    // Build immutable composition
    public IComposition<TSubject> Build();
}
```

## 📋 Global Composition API (Registry Package Only)

### Composition Static Class

Global registry for finding compositions by subject.

```csharp
public static class Composition
{
    // Generic subject lookup
    public static bool TryFind<TSubject>(TSubject subject, out IComposition<TSubject> composition) where TSubject : notnull;
    public static IComposition<TSubject>? FindOrDefault<TSubject>(TSubject subject) where TSubject : notnull;
    public static IComposition<TSubject> FindRequired<TSubject>(TSubject subject) where TSubject : notnull;
    
    // Non-generic subject lookup
    public static bool TryFind(object subject, out IComposition composition);
    public static IComposition? FindOrDefault(object subject);
    public static IComposition FindRequired(object subject);
    
    // Composition removal
    public static bool Remove<TSubject>(TSubject subject) where TSubject : notnull;
    public static bool Remove(object subject);
}
```

## 📋 Configuration APIs (Registry Package Only)

### CompositionRegistryConfiguration

Configuration for the composition registry system.

```csharp
public static class CompositionRegistryConfiguration
{
    public static ICompositionRegistryProvider Provider { get; set; }
    public static void ClearValueTypes();
    public static int ValueTypeCount { get; }
}
```

## 📋 Extension Interfaces (Registry Package Only)

### ICompositionRegistryProvider

Interface for custom composition registry implementations.

```csharp
public interface ICompositionRegistryProvider
{
    void Register(object subject, IComposition composition);
    bool TryGet(object subject, out IComposition composition);
    bool Remove(object subject);
}
```

## Extension Methods

### ReadOnlyListExtensions

Utility extensions for capability collections.

```csharp
public static class ReadOnlyListExtensions
{
    public static void ForEach<T>(this IReadOnlyList<T> list, Action<T> action);
}
```

## Usage Patterns

### Basic Registration and Query

```csharp
// Create composition
var composition = Composer.For(subject)
    .Add(new FirstCapability<Subject>())
    .Add(new SecondCapability<Subject>())
    .Build();

// Query capabilities
var capabilities = composition.GetAll<FirstCapability<Subject>>();
if (composition.Has<SecondCapability<Subject>>())
{
    // Handle capability presence
}
```

### Contract-Based Registration

```csharp
// Register under interface contract
composer.AddAs<IValidationCapability<Subject>>(new EmailValidator<Subject>());

// Register under multiple contracts (tuple syntax)
composer.AddAs<(IValidationCapability<Subject>, EmailValidator<Subject>)>(validator);
```

### Primary Capability Usage

```csharp
// Set primary capability
composer.WithPrimary(new DatabasePrimaryCapability<Subject>());

// Query primary capability
if (composition.TryGetPrimary(out var primary))
{
    // Use primary capability
}

var typedPrimary = composition.GetPrimaryOrDefaultAs<DatabasePrimaryCapability<Subject>>();
```

### Conditional Registration

```csharp
// Only register if not already present
composer.TryAdd(new LoggingCapability<Subject>(LogLevel.Info));
composer.TryAddAs<IValidationCapability<Subject>>(new EmailValidator<Subject>());
```

### Capability Removal

```csharp
// Remove capabilities by predicate
composer.RemoveWhere(cap => cap is ILogCapability<Subject> log && log.Level == LogLevel.Debug);
```

### Global Registry Usage

```csharp
// Find composition by subject
var composition = Composition.FindOrDefault(subject);

// Remove composition
Composition.Remove(subject);

// Check value type composition count
var count = CompositionRegistryConfiguration.ValueTypeCount;
```

## Error Handling

### Common Exceptions

**InvalidOperationException**:
- Thrown when multiple primary capabilities are registered
- Thrown when required capabilities are not found
- Thrown when builder is used after `Build()` has been called

**ArgumentException**:
- Thrown when contract types don't implement `ICapability<TSubject>`
- Thrown when recomposing with invalid composition types

**ArgumentNullException**:
- Thrown when null subjects or capabilities are provided

### Exception Examples

```csharp
// Multiple primary capabilities
try
{
    var composition = Composer.For(subject)
        .WithPrimary(new FirstPrimary<Subject>())
        .WithPrimary(new SecondPrimary<Subject>()) // This will throw
        .Build();
}
catch (InvalidOperationException ex)
{
    // "Multiple primary capabilities registered for 'Subject'. Only one primary capability is allowed."
}

// Required capability not found
try
{
    var required = composition.GetRequiredPrimaryAs<MissingPrimary<Subject>>();
}
catch (InvalidOperationException ex)
{
    // "Primary capability of type 'MissingPrimary' not found for subject 'Subject'."
}
```

## Performance Notes

- **Registration**: O(1) for single capabilities, O(k) for tuple registration where k = number of contracts
- **Query**: O(1) for capability lookup, O(n) for GetAll() where n = capabilities of that type
- **Memory**: Compositions use array-based storage for optimal performance
- **Threading**: All operations are thread-safe through immutability

---

This API reference covers all public interfaces and methods in Cocoar.Capabilities. For usage examples and patterns, see the [guides](guides/) and [examples](examples/) sections.