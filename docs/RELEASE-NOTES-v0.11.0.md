# Release Notes v0.11.0 - Major Architecture Refactor

**Release Date**: October 4, 2024  
**Breaking Changes**: Yes - Major API changes  
**Migration Required**: Yes - See migration guide below

## 🎯 What's New

### Major Architectural Improvement: CapabilityScope

This release introduces **CapabilityScope** - a fundamental improvement that eliminates static dependencies and provides proper lifetime management for capability compositions.

## 🚨 Breaking Changes Summary

### 1. API Changes - Before vs After

**❌ Old Static API (v1.x):**
```csharp
// Static methods - no longer available
var composition = Composer.For(userService)
    .Add(new LoggingCapability<UserService>())
    .Build();

var updated = Composer.Recompose(composition)
    .Add(new CachingCapability<UserService>())
    .Build();
```

**✅ New Scoped API (v0.11.0):**
```csharp
// Scoped approach with proper resource management
using var scope = new CapabilityScope();

var composition = scope.For(userService)
    .Add(new LoggingCapability<UserService>())
    .Build();

var updated = scope.Recompose(composition)
    .Add(new CachingCapability<UserService>())
    .Build();
```

### 2. Package Structure Changes

| Change Type | v1.x | v0.11.0 |
|-------------|------|------|
| **Packages** | `Cocoar.Capabilities.Core` + `Cocoar.Capabilities` | `Cocoar.Capabilities` only |
| **Total Size** | ~37KB (21KB + 16KB) | ~28KB |
| **Dependencies** | Zero | Zero (maintained) |

### 3. Namespace Changes

```csharp
// Remove these imports
using Cocoar.Capabilities.Core;

// Use this instead
using Cocoar.Capabilities;
```

## 🚀 Quick Migration Guide

### Step 1: Update Package References

```xml
<!-- In your .csproj file -->

<!-- REMOVE this package reference -->
<PackageReference Include="Cocoar.Capabilities.Core" Version="1.x.x" />

<!-- UPDATE this to v0.11.0 -->
<PackageReference Include="Cocoar.Capabilities" Version="0.11.0" />
```

### Step 2: Update Code Patterns

#### Pattern 1: Basic Composition
```csharp
// Before
var composition = Composer.For(subject).Add(capability).Build();

// After
using var scope = new CapabilityScope();
var composition = scope.For(subject).Add(capability).Build();
```

#### Pattern 2: Recomposition
```csharp
// Before
var updated = Composer.Recompose(existing).Add(newCapability).Build();

// After
using var scope = new CapabilityScope();
var updated = scope.Recompose(existing).Add(newCapability).Build();
```

#### Pattern 3: Dependency Injection
```csharp
// Before - Global static access
public class MyService
{
    public void DoSomething(MyObject obj)
    {
        var composition = Composer.For(obj).Add(capability).Build();
        // use composition
    }
}

// After - Inject scope or create as needed
public class MyService
{
    private readonly CapabilityScope _scope;
    
    public MyService(CapabilityScope scope) // Or create new scope
    {
        _scope = scope;
    }
    
    public void DoSomething(MyObject obj)
    {
        var composition = _scope.For(obj).Add(capability).Build();
        // use composition
    }
}
```

### Step 3: Handle Scope Lifetime

Choose one of these patterns based on your needs:

#### Option A: Short-lived Scope (Recommended for most cases)
```csharp
public void ProcessRequest()
{
    using var scope = new CapabilityScope();
    var composition = scope.For(subject).Add(capability).Build();
    // Use composition within this method
    // Scope automatically disposed at end
}
```

#### Option B: Longer-lived Scope (DI Container)
```csharp
// In DI registration
services.AddSingleton<CapabilityScope>();
// Or
services.AddScoped<CapabilityScope>();

// In your class
public class MyService
{
    private readonly CapabilityScope _scope;
    
    public MyService(CapabilityScope scope)
    {
        _scope = scope;
    }
    
    // Use _scope.For(...) throughout the service
}
```

#### Option C: Application-wide Scope
```csharp
public class Application
{
    private readonly CapabilityScope _globalScope;
    
    public Application()
    {
        _globalScope = new CapabilityScope();
    }
    
    public CapabilityScope Capabilities => _globalScope;
    
    // Remember to dispose in application shutdown
    public void Shutdown()
    {
        _globalScope?.Dispose();
    }
}
```

## ✨ New Features & Improvements

### 1. Enhanced Resource Management
- **Automatic Cleanup**: `CapabilityScope` implements `IDisposable` for proper resource management
- **Memory Efficiency**: Advanced weak reference handling for automatic cleanup
- **Type-Specific Storage**: Optimized storage patterns for reference vs value types

### 2. Performance Improvements
| Operation | v0.11.0 Performance | Notes |
|-----------|------------------|-------|
| Registry Lookups | ~25ns | Measured via benchmarks |
| Feature Queries | ~51ns | Measured via benchmarks |
| Composition Builds | ~4.5μs (50 caps) | Measured via benchmarks |

### 3. New Configuration Options
```csharp
var options = new CapabilityScopeOptions
{
    SubjectKeyMappers = new List<ISubjectKeyMapper>
    {
        new CustomStringMapper(),
        // Add your custom mappers
    }
};

using var scope = new CapabilityScope(options);
```

### 4. Enhanced Registry APIs
- `ComposerRegistryApi` - Registry-backed composer operations
- `CompositionRegistryApi` - Registry-backed composition operations
- Full registry integration while maintaining high performance

## 🔍 Why This Change?

### Problems Solved
1. **❌ Static Dependencies**: Old static API made testing and isolation difficult
2. **❌ Resource Leaks**: No explicit cleanup mechanism for internal resources
3. **❌ Package Complexity**: Two packages with overlapping concerns
4. **❌ Limited Flexibility**: Static configuration couldn't be scoped per use case

### Benefits Gained
1. **✅ Better Testability**: Scoped dependencies enable easier unit testing
2. **✅ Resource Management**: Explicit disposal and lifetime control
3. **✅ Simplified Deployment**: Single package with all functionality
4. **✅ Enhanced Flexibility**: Per-scope configuration and isolation

## 🎯 Upgrade Strategy

### Low-Risk Migration Approach

1. **Start Small**: Migrate one service/component at a time
2. **Test Thoroughly**: Each migrated component should have tests covering the new API
3. **Bridge Pattern**: Temporarily wrap old code with scope creation until fully migrated

### Example Bridge Pattern
```csharp
// Temporary wrapper to ease migration
public static class ComposerBridge
{
    [Obsolete("Use CapabilityScope directly")]
    public static Composer<T> For<T>(T subject) where T : notnull
    {
        // Create temporary scope - should be replaced with proper scope management
        var scope = new CapabilityScope();
        return scope.For(subject);
    }
}
```

## 📚 Additional Resources

- **Complete Migration Guide**: `docs/static-api-migration-strategy.md`
- **API Reference**: `docs/complete-public-api-reference.md`
- **Performance Guide**: `docs/guides/performance-optimization.md`
- **Lifecycle Management**: `docs/lifecycle-and-disposal.md`

## ❓ Common Questions

### Q: Do I need to change my capability implementations?
**A**: No! Your existing capability classes that implement `ICapability<T>` remain unchanged.

### Q: What about performance - is the new API slower?
**A**: No! Performance is actually improved. We have real benchmark data showing faster operations across all scenarios.

### Q: Can I gradually migrate or must I do it all at once?
**A**: You can migrate gradually. Consider using a bridge pattern during transition.

### Q: Why eliminate the Core package?
**A**: Simplified deployment and reduced confusion. The functionality is now consolidated into a single, well-organized package.

### Q: How do I handle dependency injection?
**A**: Register `CapabilityScope` with your DI container at the appropriate lifetime (singleton, scoped, or transient based on your needs).

## 🏁 Conclusion

This release represents a **major quality improvement** that enhances testability, resource management, and long-term maintainability while improving performance. The migration effort is moderate and can be done incrementally.

**We recommend upgrading** as this architecture provides a more robust foundation for capability-based development.

For questions or migration assistance, please refer to the documentation or create an issue in the repository.