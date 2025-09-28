# Memory Management and Lifecycle

## Overview

The Cocoar.Capabilities system implements a **dual storage strategy** for optimal memory management, automatically handling different requirements for value types vs reference types. This design prevents memory leaks while ensuring reliable access to capabilities.

## Dual Storage Architecture

### Value Types: Strong Reference Storage
**Storage**: `ConcurrentDictionary<object, IComposition>`
**Rationale**: Value types require strong references to prevent premature garbage collection
**Behavior**: Manual cleanup required via `Composition.Remove()`

### Reference Types: Weak Reference Storage  
**Storage**: `ConditionalWeakTable<object, IComposition>`
**Rationale**: Automatic cleanup when subject is no longer referenced
**Behavior**: Automatic cleanup via garbage collection

## Storage Decision Logic

```csharp
// This logic runs automatically during registration
if (subject.GetType().IsValueType)
{
    // Strong reference storage for value types
    _valueTypeStorage[subject] = composition;
}
else
{
    // Weak reference storage for reference types
    WeakRegistryCore.Register(subject, composition);
}
```

## Value Type Lifecycle

### Registration and Storage
```csharp
// Value type subjects (int, struct, enum, etc.)
var userId = 12345;
var pointData = new Point(10, 20);
var status = OrderStatus.Pending;

// All stored with strong references
var userComposition = Composer.For(userId).Add(userCapability).Build();
var pointComposition = Composer.For(pointData).Add(pointCapability).Build();  
var statusComposition = Composer.For(status).Add(statusCapability).Build();
```

### Memory Implications
```csharp
// ⚠️ Value types won't be automatically cleaned up
// These compositions stay in memory until explicitly removed

// Manual cleanup required
Composition.Remove(userId);
Composition.Remove(pointData);
Composition.Remove(status);
```

### Value Type Best Practices
```csharp
// Pattern 1: Explicit cleanup in using pattern
using var scope = new CompositionScope();
var composition = Composer.For(valueTypeSubject).Add(capability).Build();
// ... use composition
// Cleanup happens when scope disposes

// Pattern 2: Batch cleanup
var valueTypeSubjects = new List<object> { userId1, userId2, pointData };
// ... work with compositions
foreach (var subject in valueTypeSubjects)
{
    Composition.Remove(subject);
}

// Pattern 3: Application lifecycle cleanup
public void Shutdown()
{
    CompositionRegistryConfiguration.ClearValueTypes(); // Clears all value type compositions
}
```

## Reference Type Lifecycle

### Registration and Storage
```csharp
// Reference type subjects (classes)
var userService = new UserService();
var orderProcessor = new OrderProcessor();
var logger = new FileLogger();

// All stored with weak references
var userComposition = Composer.For(userService).Add(userCapability).Build();
var orderComposition = Composer.For(orderProcessor).Add(orderCapability).Build();
var loggerComposition = Composer.For(logger).Add(loggerCapability).Build();
```

### Automatic Cleanup
```csharp
// Reference types clean up automatically
var service = new UserService();
var composition = Composer.For(service).Add(capability).Build();

// When service goes out of scope and is garbage collected,
// the composition is automatically removed from storage
service = null; // No more references
GC.Collect(); // Composition automatically cleaned up
```

## Memory Management Strategies

### For Long-Running Applications
```csharp
// Strategy 1: Periodic value type cleanup
public class CapabilityMemoryManager
{
    private readonly Timer _cleanupTimer;
    
    public CapabilityMemoryManager()
    {
        _cleanupTimer = new Timer(CleanupValueTypes, null, 
            TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
    }
    
    private void CleanupValueTypes(object state)
    {
        // Application-specific logic to determine which value types to remove
        var expiredSubjects = GetExpiredValueTypeSubjects();
        foreach (var subject in expiredSubjects)
        {
            Composition.Remove(subject);
        }
    }
}
```

### For Request-Scoped Applications
```csharp
// Strategy 2: Request-scoped cleanup
public class RequestScopeManager : IDisposable
{
    private readonly List<object> _valueTypeSubjects = new();
    
    public IComposition<T> CreateComposition<T>(T subject, params ICapability<T>[] capabilities) where T : notnull
    {
        var composer = Composer.For(subject);
        foreach (var capability in capabilities)
        {
            composer.Add(capability);
        }
        
        var composition = composer.Build();
        
        // Track value types for cleanup
        if (typeof(T).IsValueType)
        {
            _valueTypeSubjects.Add(subject);
        }
        
        return composition;
    }
    
    public void Dispose()
    {
        // Cleanup all value type compositions created in this scope
        foreach (var subject in _valueTypeSubjects)
        {
            Composition.Remove(subject);
        }
    }
}
```

## Best Practices

### 1. **Prefer Reference Types for Subjects**
When possible, use class-based subjects for automatic cleanup.

### 2. **Implement Cleanup for Value Types**
Always have a strategy for cleaning up value type compositions.

### 3. **Use Scoped Patterns**
Implement scoped lifetime management for value type compositions.

### 4. **Monitor Memory Usage**
In production, monitor value type composition counts.

### 5. **Avoid Long-Lived Value Type Compositions**
Don't create compositions for temporary value type data.

## Common Pitfalls

### ❌ **Pitfall 1: Forgetting Value Type Cleanup**
```csharp
// This creates a memory leak
for (int i = 0; i < 1000000; i++)
{
    var composition = Composer.For(i).Add(capability).Build(); // Never cleaned up
}
```

### ✅ **Correct Approach**
```csharp
void ProcessUserIds(int[] userIds)
{
    var createdSubjects = new List<int>();
    
    try
    {
        foreach (var id in userIds)
        {
            var composition = Composer.For(id).Add(capability).Build();
            createdSubjects.Add(id);
            // Process composition
        }
    }
    finally
    {
        // Cleanup all created compositions
        foreach (var id in createdSubjects)
        {
            Composition.Remove(id);
        }
    }
}
```

## Integration with DI Containers

### Scoped Lifetime Integration
```csharp
// Register cleanup service with DI container
services.AddScoped<ICapabilityCleanupService, CapabilityCleanupService>();

public class CapabilityCleanupService : ICapabilityCleanupService, IDisposable
{
    private readonly List<object> _trackedValueTypes = new();
    
    public IComposition<T> CreateTrackedComposition<T>(T subject, params ICapability<T>[] capabilities) where T : notnull
    {
        var composition = Composer.For(subject).Add(capabilities).Build();
        
        if (typeof(T).IsValueType)
        {
            _trackedValueTypes.Add(subject);
        }
        
        return composition;
    }
    
    public void Dispose()
    {
        foreach (var subject in _trackedValueTypes)
        {
            Composition.Remove(subject);
        }
    }
}
```

---

*The dual storage strategy provides optimal memory management by automatically handling the different lifecycle requirements of value types and reference types.*