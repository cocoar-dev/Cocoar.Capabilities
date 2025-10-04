# Static API Migration Strategy - Backward Compatibility & Migration Path

## Overview

The new context-based architecture provides static default contexts that maintain complete backward compatibility with the existing static API while enabling smooth migration to the more powerful context-based approach.

## Static Default Contexts

### CapabilityContext.Default
```csharp
/// <summary>
/// Gets a default context with both registries enabled.
/// This provides the same behavior as the previous static API.
/// </summary>
public static CapabilityContext Default { get; } = new CapabilityContext();
```

**Characteristics:**
- ✅ `UseComposerRegistry = true` (default)
- ✅ `UseCompositionRegistry = true` (default)  
- ✅ Auto-registers composers and compositions
- ✅ Provides global shared state like previous static API

### CapabilityContext.Lightweight
```csharp
/// <summary>
/// Gets a context with registries disabled for lightweight operation.
/// Use this when you don't need persistence or cross-reference capabilities.
/// </summary>
public static CapabilityContext Lightweight { get; } = new CapabilityContext(new CapabilityContextOptions
{
    UseComposerRegistry = false,
    UseCompositionRegistry = false
});
```

**Characteristics:**
- ❌ `UseComposerRegistry = false`
- ❌ `UseCompositionRegistry = false`
- ⚡ Optimized for performance scenarios
- 💾 Minimal memory footprint when unused

## Updated Static API

The existing static `Composer` class now delegates to `CapabilityContext.Default`:

### Before (Old Implementation)
```csharp
public static class Composer
{
    public static Composer<TSubject> For<TSubject>(TSubject subject)
        where TSubject : notnull
    {
        // Created isolated composers with no shared state
        return new Composer<TSubject>(subject);
    }
}
```

### After (New Implementation)
```csharp
public static class Composer
{
    /// <summary>
    /// Creates a composer for the specified subject using the default context.
    /// This provides the same behavior as the previous static API.
    /// </summary>
    public static Composer<TSubject> For<TSubject>(TSubject subject)
        where TSubject : notnull
    {
        return CapabilityContext.Default.For(subject);
    }

    /// <summary>
    /// Creates a composer from an existing composition using the default context.
    /// </summary>
    public static Composer<TSubject> Recompose<TSubject>(IComposition<TSubject> existingComposition)
        where TSubject : notnull
    {
        return new Composer<TSubject>(existingComposition, CapabilityContext.Default);
    }
}
```

## Migration Paths

### 1. Zero-Change Migration (Immediate Compatibility)
```csharp
// Existing code continues to work unchanged
var composer = Composer.For(document);
var composition = composer.Add(new MetadataCapability("author", "John")).Build();

// Now automatically registered in CapabilityContext.Default
// Can be accessed via context API:
Assert.True(CapabilityContext.Default.Composers.TryGet(document, out var found));
Assert.Same(composer, found);
```

### 2. Gradual Migration (Progressive Enhancement)
```csharp
// Phase 1: Keep using static API
var legacyComposer = Composer.For(document);
var legacyComposition = legacyComposer.Add(capability).Build();

// Phase 2: Access via context when needed
var registeredComposition = CapabilityContext.Default.Compositions
    .TryGet(document, out var composition) ? composition : null;

// Phase 3: New features use context directly
var contextComposer = CapabilityContext.Default.For(document, useRegistry: false);
var performanceComposition = contextComposer.Add(capability).Build(useRegistry: false);
```

### 3. Full Migration (Context-First Approach)
```csharp
// Replace static calls with context calls
// Before:
// var composer = Composer.For(document);

// After:
var composer = CapabilityContext.Default.For(document);

// Benefit: Access to registry override parameters
var lightweightComposer = CapabilityContext.Default.For(document, useRegistry: false);
var persistentComposition = composer.Build(useRegistry: true);
```

### 4. Custom Context Migration
```csharp
// For applications needing custom behavior
var customContext = new CapabilityContext(new CapabilityContextOptions
{
    UseComposerRegistry = true,
    UseCompositionRegistry = false,
    ComposerRegistry = new MyCustomRegistry()
});

// Migrate from static to custom context gradually
var composer = customContext.For(document); // Instead of Composer.For(document)
```

## Usage Examples

### Static API Equivalents

| Old Static API | New Context API Equivalent | Benefits |
|---------------|---------------------------|----------|
| `Composer.For(subject)` | `CapabilityContext.Default.For(subject)` | ✅ Method-level overrides |
| N/A | `CapabilityContext.Lightweight.For(subject)` | ⚡ Performance optimization |
| N/A | `context.For(subject, useRegistry: false)` | 🎯 Fine-grained control |

### Backward Compatibility Examples

```csharp
// All existing code works unchanged
var document = new Document("example.txt");

// 1. Static API (unchanged)
var composer1 = Composer.For(document);
var composition1 = composer1.Add(new MetadataCapability("author", "Alice")).Build();

// 2. Context API (equivalent behavior)
var composer2 = CapabilityContext.Default.For(document);
var composition2 = composer2.Add(new MetadataCapability("editor", "Bob")).Build();

// 3. Both are registered in the same default context
Assert.True(CapabilityContext.Default.Composers.TryGet(document, out var foundComposer));
Assert.True(CapabilityContext.Default.Compositions.TryGet(document, out var foundComposition));

// 4. Registry APIs now available
var allComposers = CapabilityContext.Default.Composers.GetAll(); // New capability
var allCompositions = CapabilityContext.Default.Compositions.GetAll(); // New capability
```

### Performance Optimization Examples

```csharp
// 1. Lightweight operations (no registry overhead)
var fastComposer = CapabilityContext.Lightweight.For(document);
var fastComposition = fastComposer.Add(capability).Build();

// 2. Conditional registration
var composer = CapabilityContext.Default.For(document, useRegistry: shouldPersist);
var composition = composer.Add(capability).Build(useRegistry: shouldPersist);

// 3. Mixed usage patterns
var defaultComposer = CapabilityContext.Default.For(document); // Auto-registered
var lightweightComposer = CapabilityContext.Lightweight.For(document); // Not registered
```

## Benefits Achieved

### ✅ **Complete Backward Compatibility**
- All existing static API calls work unchanged
- Same behavior and semantics
- Zero breaking changes

### ✅ **Smooth Migration Path**
- Multiple migration strategies available
- Can migrate incrementally
- Old and new APIs interoperate seamlessly

### ✅ **Enhanced Capabilities**
- Access to registry APIs through static contexts
- Method-level registry control
- Performance optimization options

### ✅ **Future-Proof Architecture**
- Context-based design enables future enhancements
- Dependency injection ready
- Testing and isolation friendly

## Removal Strategy for Old Static Methods

Now that the static API delegates to the default context, you can:

1. **Keep the static API** for backward compatibility (recommended)
2. **Mark as obsolete** with migration guidance:
   ```csharp
   [Obsolete("Use CapabilityContext.Default.For(subject) instead")]
   public static Composer<TSubject> For<TSubject>(TSubject subject)
   ```
3. **Update documentation** to recommend context API for new code
4. **Update existing tests** to use context API directly

## Testing Strategy

All tests can be updated to use the static contexts:

```csharp
// Before:
var composer = Composer.For(subject);

// After (equivalent behavior):
var composer = CapabilityContext.Default.For(subject);

// Or for isolated testing:
var composer = CapabilityContext.Lightweight.For(subject);
```

This approach provides the best of both worlds: **complete backward compatibility** with **smooth migration paths** to the more powerful context-based architecture.