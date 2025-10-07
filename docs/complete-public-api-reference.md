# Complete Public API Reference

This document provides a comprehensive list of all public APIs available in **Cocoar.Capabilities v1.0.0**.

## Package Information

**Package**: `Cocoar.Capabilities`  
**Namespace**: `Cocoar.Capabilities`  
**Target Frameworks**: .NET 8.0+  
**Dependencies**: None  

## Core Interfaces

### ICapability
```csharp
public interface ICapability { }
```
Base marker interface for all capabilities.

### ICapability&lt;in TSubject&gt;
```csharp
public interface ICapability<in TSubject> : ICapability { }
```
Generic capability interface that defines a capability for a specific subject type.

### IPrimaryCapability&lt;in T&gt;
```csharp
public interface IPrimaryCapability<in T> : ICapability<T> { }
```
Marker interface for primary capabilities. Only one primary capability per subject allowed.

### IOrderedCapability
```csharp
public interface IOrderedCapability
{
    int Order { get; }
}
```
Interface for capabilities that need specific ordering within their type group. Lower values execute first.

### IComposition
```csharp
public interface IComposition
{
    object Subject { get; }
    int TotalCapabilityCount { get; }
}
```
Non-generic interface for accessing basic composition information.

### IComposition&lt;TSubject&gt;
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
Generic interface for typed access to capabilities attached to a subject.

## Main Classes

### CapabilityScope
```csharp
public sealed class CapabilityScope : IDisposable
{
    // Constructor
    public CapabilityScope(CapabilityScopeOptions? options = null);
    
    // Properties
    public ComposerRegistryApi Composers { get; }
    public CompositionRegistryApi Compositions { get; }
    internal bool IsDisposed { get; }
    
    // Methods
    public Composer<TSubject> For<TSubject>(TSubject subject, bool? useRegistry = null) where TSubject : notnull;
    public Composer<TSubject> Recompose<TSubject>(IComposition<TSubject> composition, bool? useRegistry = null) where TSubject : notnull;
    public void Dispose();
}
```
Main entry point for creating and managing capability compositions within a scope.

### Composer&lt;TSubject&gt;
```csharp
public sealed class Composer<TSubject> where TSubject : notnull
{
    // Property
    public TSubject Subject { get; }

    // Basic registration
    public Composer<TSubject> Add(ICapability<TSubject> capability);
    
    // Contract registration
    public Composer<TSubject> AddAs<TContract>(ICapability<TSubject> capability) where TContract : class, ICapability<TSubject>;
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
    
    // Build composition
    public IComposition<TSubject> Build(bool? useRegistry = null);
}
```
Fluent builder for capability registration and composition creation.

## Configuration

### CapabilityScopeOptions
```csharp
public record CapabilityScopeOptions
{
    public bool UseComposerRegistry { get; init; } = true;
    public bool UseCompositionRegistry { get; init; } = true;
    public IReadOnlyList<ISubjectKeyMapper> SubjectKeyMappers { get; init; } = Array.Empty<ISubjectKeyMapper>();
}
```
Configuration options for `CapabilityScope` behavior.

### ISubjectKeyMapper
```csharp
public interface ISubjectKeyMapper
{
    bool CanMap(Type subjectType);
    string MapToKey(object subject);
}
```
Interface for custom subject key mapping strategies.

## Registry APIs

### ComposerRegistryApi
```csharp
public class ComposerRegistryApi : IDisposable
{
    // Find methods
    public bool TryFind<TSubject>(TSubject subject, out Composer<TSubject> composer) where TSubject : notnull;
    public Composer<TSubject>? FindOrDefault<TSubject>(TSubject subject) where TSubject : notnull;
    public Composer<TSubject> FindRequired<TSubject>(TSubject subject) where TSubject : notnull;
    
    // Removal
    public bool Remove<TSubject>(TSubject subject) where TSubject : notnull;
    public bool Remove(object subject);
    
    // Disposal
    public void Dispose();
}
```
Scope-level registry for composer lookup and management.

### CompositionRegistryApi
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
    
    // Removal
    public bool Remove<TSubject>(TSubject subject) where TSubject : notnull;
    public bool Remove(object subject);
    
    // Disposal
    public void Dispose();
}
```
Scope-level registry for composition lookup and management.

## Extension Methods

### ReadOnlyListExtensions
```csharp
public static class ReadOnlyListExtensions
{
    public static void ForEach<T>(this IReadOnlyList<T> list, Action<T> action);
}
```
Utility extensions for capability collections.

## Internal Interfaces (Advanced Usage)

### ICapabilityRegistry
```csharp
public interface ICapabilityRegistry : IDisposable
{
    void RegisterComposer<TSubject>(Composer<TSubject> composer) where TSubject : notnull;
    bool TryFindComposer<TSubject>(TSubject subject, out Composer<TSubject> composer) where TSubject : notnull;
    bool RemoveComposer<TSubject>(TSubject subject) where TSubject : notnull;
    bool RemoveComposer(object subject);
    
    void RegisterComposition<TSubject>(TSubject subject, IComposition<TSubject> composition) where TSubject : notnull;
    bool TryFindComposition<TSubject>(TSubject subject, out IComposition<TSubject> composition) where TSubject : notnull;
    bool TryFindComposition(object subject, out IComposition composition);
    bool RemoveComposition<TSubject>(TSubject subject) where TSubject : notnull;
    bool RemoveComposition(object subject);
}
```

### IComposerRegistry
```csharp
public interface IComposerRegistry : IDisposable
{
    void Register<TSubject>(Composer<TSubject> composer) where TSubject : notnull;
    bool TryFind<TSubject>(TSubject subject, out Composer<TSubject> composer) where TSubject : notnull;
    bool Remove<TSubject>(TSubject subject) where TSubject : notnull;
    bool Remove(object subject);
}
```

### ICompositionRegistry
```csharp
public interface ICompositionRegistry : IDisposable
{
    void Register<TSubject>(TSubject subject, IComposition<TSubject> composition) where TSubject : notnull;
    bool TryFind<TSubject>(TSubject subject, out IComposition<TSubject> composition) where TSubject : notnull;
    bool TryFind(object subject, out IComposition composition);
    bool Remove<TSubject>(TSubject subject) where TSubject : notnull;
    bool Remove(object subject);
}
```

## Usage Examples

### Basic Usage
```csharp
using var scope = new CapabilityScope();
var subject = new MyClass();

var composition = scope.For(subject)
    .Add(new LoggingCapability<MyClass>(LogLevel.Info))
    .Add(new CachingCapability<MyClass>(TimeSpan.FromMinutes(5)))
    .Build();

// Query capabilities
var hasLogging = composition.Has<LoggingCapability<MyClass>>();
var allCapabilities = composition.GetAll<ICapability<MyClass>>();
```

### Contract Registration
```csharp
using var scope = new CapabilityScope();
var validator = new EmailValidator<User>();

var composition = scope.For(user)
    .AddAs<IValidationCapability<User>>(validator)
    .Build();

// Query by contract
var validators = composition.GetAll<IValidationCapability<User>>();
```

### Primary Capabilities
```csharp
using var scope = new CapabilityScope();
var composition = scope.For(service)
    .WithPrimary(new DatabasePrimaryCapability<Service>())
    .Add(new LoggingCapability<Service>(LogLevel.Debug))
    .Build();

if (composition.TryGetPrimary(out var primary))
{
    // Handle primary capability
}
```

### Scope Registry
```csharp
using var scope = new CapabilityScope();
var composition = scope.For(subject)
    .Add(new SomeCapability<Subject>())
    .Build();

// Find later via scope
var found = scope.Compositions.FindOrDefault(subject);
```

## Exception Types

- **InvalidOperationException**: Multiple primary capabilities, builder used after Build(), required capabilities not found
- **ArgumentException**: Invalid contract types, recomposition with invalid types
- **ArgumentNullException**: Null subjects or capabilities
- **ObjectDisposedException**: Using disposed scope

## Performance Characteristics

- **Registration**: O(1) for single capabilities, O(k) for tuple registration
- **Query**: O(1) for capability lookup, O(n) for GetAll() where n = capabilities of that type
- **Memory**: Array-based storage, minimal overhead per composition
- **Threading**: Thread-safe through immutability, no locks required

---

**Version**: 1.0.0  
**Last Updated**: October 7, 2025  
**Documentation Status**: ✅ Current