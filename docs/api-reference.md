# API Reference

Complete reference for all public APIs in Cocoar.Capabilities.

## Package Architecture

Distributed as a single package: **`Cocoar.Capabilities`**.

Scope-level options (`CapabilityScopeOptions`) enable or disable composer and composition registry tracking. No separate *Core* vs *Registry* packages exist anymore. Historical references to dual packaging and static helpers (like `BuildAndRegister()` / `Composition.FindOrDefault`) should be migrated to the `CapabilityScope` model.

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

Marker interface for primary capabilities. Exactly one primary capability may exist per subject at any time.
Adding rules:

- First registration may use Add, AddAs, tuple AddAs, or WithPrimary
- Replacement MUST use WithPrimary(newPrimary)
- Add / AddAs / tuple AddAs will THROW if a primary already exists
- WithPrimary(null) removes the current primary
- Tuples may not contain more than one IPrimaryCapability<> contract
 - TryAdd / TryAddAs of another primary silently no-op (they never replace)

```csharp
public interface IPrimaryCapability<in T> : ICapability<T> { }
```

**Usage**:
```csharp
public record DatabasePrimaryCapability<T> : IPrimaryCapability<T>;

// First time
composer.Add(new DatabasePrimaryCapability<UserService>());

// Replacing (must use WithPrimary)
composer.WithPrimary(new DatabasePrimaryCapability<UserService>());

// Removing
composer.WithPrimary(null);
```

## Registry Participation

Registration is controlled per scope and optionally per build call:

```csharp
using var scope = new CapabilityScope(new CapabilityScopeOptions
{
    UseCompositionRegistry = true // default
});

var composition = scope.For(user)
    .Add(new LoggingCapability<User>(LogLevel.Info))
    .Build(); // automatically registered because UseCompositionRegistry=true

// Explicit override (force register even if disabled in options)
var forced = scope.For(user)
    .Add(new CachingCapability<User>(TimeSpan.FromMinutes(5)))
    .Build(useRegistry: true);

// Lookup
var found = scope.Compositions.FindOrDefault(user);
```

To migrate from legacy `BuildAndRegister()` + static global lookup, see `static-api-migration-strategy.md`.
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

### CapabilityScope

Entry point for creating capability scopes and managing compositions.

```csharp
public sealed class CapabilityScope : IDisposable
{
    // Constructor
    public CapabilityScope(CapabilityScopeOptions? options = null);
    
    // Create composer for subject
    public Composer<TSubject> For<TSubject>(TSubject subject, bool? useRegistry = null) where TSubject : notnull;
    
    // Recomposition from existing composition
    public Composer<TSubject> Recompose<TSubject>(IComposition<TSubject> composition, bool? useRegistry = null) where TSubject : notnull;
    
    // Registry access
    public ComposerRegistryApi Composers { get; }
    public CompositionRegistryApi Compositions { get; }
    
    // Disposal
    public void Dispose();
}
```

### CapabilityScopeOptions

Configuration options for capability scopes.

```csharp
public record CapabilityScopeOptions
{
    public bool UseComposerRegistry { get; init; } = true;
    public bool UseCompositionRegistry { get; init; } = true;
    public IReadOnlyList<ISubjectKeyMapper> SubjectKeyMappers { get; init; } = Array.Empty<ISubjectKeyMapper>();
}
```

### Composer&lt;TSubject&gt;

Fluent builder for capability registration (created via `CapabilityScope.For()`).

```csharp
public sealed class Composer<TSubject> where TSubject : notnull
{
    public TSubject Subject { get; }

    // Basic registration
    public Composer<TSubject> Add(ICapability<TSubject> capability);
    
    // Contract registration
    public Composer<TSubject> AddAs<TContract>(ICapability<TSubject> capability) where TContract : class, ICapability<TSubject>;
    
    // Tuple contract registration
    public Composer<TSubject> AddAs<TContracts>(ICapability<TSubject> capability) where TContracts : ITuple;
    
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
    public IComposition<TSubject> Build(bool? useRegistry = null);
}
```

## Registry APIs

### ComposerRegistryApi

Scope-level registry for composer lookup and management.

```csharp
public class ComposerRegistryApi : IDisposable
{
    // Find existing composer by subject
    public bool TryFind<TSubject>(TSubject subject, out Composer<TSubject> composer) where TSubject : notnull;
    public Composer<TSubject>? FindOrDefault<TSubject>(TSubject subject) where TSubject : notnull;
    public Composer<TSubject> FindRequired<TSubject>(TSubject subject) where TSubject : notnull;
    
    // Remove composer
    public bool Remove<TSubject>(TSubject subject) where TSubject : notnull;
    public bool Remove(object subject);
    
    public void Dispose();
}
```

### CompositionRegistryApi

Scope-level registry for composition lookup and management.

```csharp
public class CompositionRegistryApi : IDisposable
{
    // Generic subject lookup
    public bool TryFind<TSubject>(TSubject subject, out IComposition<TSubject> composition) where TSubject : notnull;
    public IComposition<TSubject>? FindOrDefault<TSubject>(TSubject subject) where TSubject : notnull;
    public IComposition<TSubject> FindRequired<TSubject>(TSubject subject) where TSubject : notnull;
    
    // Non-generic subject lookup
    public bool TryFind(object subject, out IComposition composition);
    public IComposition? FindOrDefault(object subject);
    public IComposition FindRequired(object subject);
    
    // Composition removal
    public bool Remove<TSubject>(TSubject subject) where TSubject : notnull;
    public bool Remove(object subject);
    
    public void Dispose();
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

## Configuration

### ISubjectKeyMapper

Interface for custom subject key mapping strategies.

```csharp
public interface ISubjectKeyMapper
{
    bool CanMap(Type subjectType);
    string MapToKey(object subject);
}
```

## Usage Patterns

### Basic Registration and Query

```csharp
// Create scope and composition
using var scope = new CapabilityScope();
var composition = scope.For(subject)
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
using var scope = new CapabilityScope();
var composer = scope.For(subject)
    .AddAs<IValidationCapability<Subject>>(new EmailValidator<Subject>());

// Register under multiple contracts (tuple syntax)
composer.AddAs<(IValidationCapability<Subject>, EmailValidator<Subject>)>(validator);
```
composer.AddAs<(IValidationCapability<Subject>, EmailValidator<Subject>)>(validator);
```

### Primary Capability Usage

```csharp
// Set primary capability
using var scope = new CapabilityScope();
var composition = scope.For(subject)
    .WithPrimary(new DatabasePrimaryCapability<Subject>())
    .Build();

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
using var scope = new CapabilityScope();
var composer = scope.For(subject)
    .TryAdd(new LoggingCapability<Subject>(LogLevel.Info))
    .TryAddAs<IValidationCapability<Subject>>(new EmailValidator<Subject>());
```

### Capability Removal

```csharp
// Remove capabilities by predicate
using var scope = new CapabilityScope();
var composition = scope.For(subject)
    .Add(new LoggingCapability<Subject>(LogLevel.Debug, "Debug"))
    .Add(new LoggingCapability<Subject>(LogLevel.Info, "Info"))
    .RemoveWhere(cap => cap is LoggingCapability<Subject> log && log.Level == LogLevel.Debug)
    .Build();
```

### Scope Registry Usage

```csharp
// Create scope with configuration
using var scope = new CapabilityScope(new CapabilityScopeOptions
{
    UseCompositionRegistry = true,
    UseComposerRegistry = true
});

// Build and register
var composition = scope.For(subject)
    .Add(new LoggingCapability<Subject>(LogLevel.Info, "Test"))
    .Build(); // Automatically registered due to UseCompositionRegistry=true

// Find composition by subject
var foundComposition = scope.Compositions.FindOrDefault(subject);

// Remove composition
scope.Compositions.Remove(subject);

// Find composer (if still building)
var foundComposer = scope.Composers.FindOrDefault(subject);
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