# ADR-002: Introduce `Using*` Extension Methods for Fluent Capability Usage

**Status**: Accepted  
**Date**: 2025-10-25  
**Decision type**: API Enhancement  
**Scope**: Cocoar.Capabilities (framework-wide)

---

## Context

The `IComposition` interface provides explicit retrieval methods for capabilities:

- `GetRequiredFirst<T>()` / `GetFirstOrDefault<T>()` / `TryGetFirst<T>(out T?)`
- `GetRequiredLast<T>()` / `GetLastOrDefault<T>()` / `TryGetLast<T>(out T?)`
- `GetAll<T>()`

While functionally complete, common usage patterns like "retrieve and immediately use" require verbose code:

```csharp
var logger = composition.GetRequiredFirst<ILogger>();
logger.Info("started");

var handlers = composition.GetAll<IEventHandler>();
foreach (var handler in handlers)
    handler.Handle(evt);
```

For inline usage where the capability instance isn't needed afterward, this feels heavyweight. We want to provide **ergonomic convenience methods** that:

1. Support fluent, chainable usage for common patterns
2. Maintain 100% semantic consistency with existing `Get*` methods
3. Cover both single-instance and multi-instance scenarios
4. Preserve explicit intent (first/last/each/all)

---

## Decision

Introduce a comprehensive set of **`Using*` extension methods** on `IComposition` that mirror the existing `Get*` API surface, optimized for inline/fluent usage.

These methods are **pure convenience wrappers** with **zero behavioral changes**—they directly delegate to existing retrieval methods and execute user-provided delegates.

---

## API Surface

```csharp
namespace Cocoar.Capabilities;

public static class CompositionUsingExtensions
{
    // ============= FIRST =============
    
    /// <summary>
    /// Uses the first capability of type <typeparamref name="T"/>.
    /// Throws if no instances exist. Returns composition for chaining.
    /// </summary>
    public static IComposition UsingFirst<T>(this IComposition composition, Action<T> use)
        where T : class;

    /// <summary>
    /// Uses the first capability of type <typeparamref name="T"/> and returns a result.
    /// Throws if no instances exist.
    /// </summary>
    public static TResult UsingFirst<T, TResult>(this IComposition composition, Func<T, TResult> use)
        where T : class;

    /// <summary>
    /// Uses the first capability of type <typeparamref name="T"/> if it exists.
    /// Silently skips if no instances exist. Returns composition for chaining.
    /// </summary>
    public static IComposition UsingFirstOrDefault<T>(this IComposition composition, Action<T> use)
        where T : class;

    // ============= LAST =============
    
    /// <summary>
    /// Uses the last capability of type <typeparamref name="T"/>.
    /// Throws if no instances exist. Returns composition for chaining.
    /// </summary>
    public static IComposition UsingLast<T>(this IComposition composition, Action<T> use)
        where T : class;

    /// <summary>
    /// Uses the last capability of type <typeparamref name="T"/> and returns a result.
    /// Throws if no instances exist.
    /// </summary>
    public static TResult UsingLast<T, TResult>(this IComposition composition, Func<T, TResult> use)
        where T : class;

    /// <summary>
    /// Uses the last capability of type <typeparamref name="T"/> if it exists.
    /// Silently skips if no instances exist. Returns composition for chaining.
    /// </summary>
    public static IComposition UsingLastOrDefault<T>(this IComposition composition, Action<T> use)
        where T : class;

    // ============= EACH =============
    
    /// <summary>
    /// Executes <paramref name="use"/> for each capability of type <typeparamref name="T"/>.
    /// Silent if no instances exist (iterates empty collection). Returns composition for chaining.
    /// </summary>
    public static IComposition UsingEach<T>(this IComposition composition, Action<T> use)
        where T : class;

    /// <summary>
    /// Executes <paramref name="use"/> for each capability of type <typeparamref name="T"/> and collects results.
    /// Returns a list of results (empty if no instances exist).
    /// </summary>
    public static IReadOnlyList<TResult> UsingEach<T, TResult>(this IComposition composition, Func<T, TResult> use)
        where T : class;

    // ============= ALL =============
    
    /// <summary>
    /// Executes <paramref name="use"/> with the full collection of <typeparamref name="T"/> capabilities.
    /// Use for aggregations or collection-level operations. Returns composition for chaining.
    /// </summary>
    public static IComposition UsingAll<T>(this IComposition composition, Action<IReadOnlyList<T>> use)
        where T : class;

    /// <summary>
    /// Executes <paramref name="use"/> with the full collection of <typeparamref name="T"/> capabilities and returns a result.
    /// Use for aggregations or collection-level operations.
    /// </summary>
    public static TResult UsingAll<T, TResult>(this IComposition composition, Func<IReadOnlyList<T>, TResult> use)
        where T : class;
}
```

---

## Usage Examples

### Single Instance Usage

```csharp
// Required first (throws if missing)
composition.UsingFirst<ILogger>(log => log.Info("started"));

// Required first with result
var count = composition.UsingFirst<IRepository, int>(repo => repo.Count());

// Optional first (silent if missing, chainable)
composition
    .UsingFirstOrDefault<ILogger>(log => log.Info("started"))
    .UsingFirst<IMetrics>(m => m.Inc());

// Last instance (when order matters)
composition.UsingLast<IMiddleware>(m => m.Execute());
```

### Multiple Instance Usage

```csharp
// Process each capability individually
composition.UsingEach<IEventHandler>(handler => handler.Handle(evt));

// Map each to a result
var counts = composition.UsingEach<IRepository, int>(repo => repo.Count());
// Returns: [5, 12, 8]

// Aggregate operation on collection
var total = composition.UsingAll<IRepository, int>(repos => repos.Sum(r => r.Count()));
// Returns: 25

// Collection operation (chainable)
composition.UsingAll<IPlugin>(plugins => 
{
    foreach (var p in plugins)
        p.Initialize();
});
```

### Fluent Chaining

```csharp
composition
    .UsingFirst<ILogger>(log => log.Info("starting"))
    .UsingEach<IPlugin>(p => p.Initialize())
    .UsingFirstOrDefault<IOptionalFeature>(f => f.Configure())
    .UsingAll<IEventHandler>(handlers => handlers.ForEach(h => h.Register()))
    .UsingLast<IMetrics>(m => m.Inc("startup"));
```

---

## Semantics & Guarantees

### Delegation to Existing Methods

All `Using*` methods **directly delegate** to existing `IComposition` methods:

| `Using*` Method | Delegates To |
|----------------|--------------|
| `UsingFirst<T>()` | `GetRequiredFirst<T>()` |
| `UsingFirstOrDefault<T>()` | `GetFirstOrDefault<T>()` |
| `UsingLast<T>()` | `GetRequiredLast<T>()` |
| `UsingLastOrDefault<T>()` | `GetLastOrDefault<T>()` |
| `UsingEach<T>()` | `GetAll<T>()` |
| `UsingAll<T>()` | `GetAll<T>()` |

### Exception Behavior

- **`UsingFirst<T>()` / `UsingLast<T>()`**: Throw if no instances exist (same as `GetRequired*`)
- **`UsingFirstOrDefault<T>()` / `UsingLastOrDefault<T>()`**: Silent if missing
- **`UsingEach<T>()` / `UsingAll<T>()`**: Silent if collection is empty

### Chainability

- All `Action<T>`-based overloads return `IComposition` for fluent chaining
- All `Func<T, TResult>`-based overloads return `TResult` (terminate the chain)

### Thread Safety & Lifetime

- **No additional isolation or scoping**: `Using*` methods provide **zero** lifetime management or thread safety beyond what the underlying composition provides
- They are **pure convenience wrappers** for inline execution

---

## Consequences

### Positive

✅ **Improved ergonomics** for common inline usage patterns  
✅ **Fluent, chainable API** for sequential capability usage  
✅ **100% consistency** with existing `Get*` semantics  
✅ **Zero behavioral changes** — pure convenience layer  
✅ **Self-documenting intent** via explicit method names (`First`, `Last`, `Each`, `All`)  
✅ **Complete coverage** of single/multi-instance scenarios  

### Neutral

⚠️ **Expanded API surface** — 10 new methods, but all follow a consistent, predictable pattern  
⚠️ **No "simple" `Using<T>()`** — users must choose `First`, `Last`, `Each`, or `All` (this is intentional to avoid ambiguity)  

### Negative

None identified. The extension methods are opt-in and don't affect existing code.

---

## Alternatives Considered

### 1. Generic `Using<T>()` without position specifiers

**Rejected**: Ambiguous when multiple instances exist. Which one gets used? Forces users to guess or read implementation.

### 2. Different naming: `ActAs<T>()`, `As<T>()`, `Use<T>()`

**Rejected**: 
- `ActAs` implies the composition "becomes" the capability (wrong mental model)
- `As` is too generic and doesn't convey position (first/last)
- `Use` without position is ambiguous

`Using*` with explicit position (`First`, `Last`, `Each`, `All`) is clearer and mirrors the existing API.

### 3. Only add `UsingFirst` and `UsingEach` (minimal set)

**Rejected**: Incomplete. Users need `Last` for ordered scenarios, `Try*` for optional capabilities, and `*OrDefault` for silent fallback in chains.

### 4. Return `bool` from `TryUsing*` methods

**Rejected**: Breaks chainability. Returning `IComposition` allows fluent chaining while still being silent on missing capabilities. Users who need explicit success/failure checks can use `TryGet*` directly.

---

## Backward Compatibility & Migration

- **100% backward compatible**: New extension methods, no changes to existing API
- **Opt-in**: Teams can adopt incrementally or not at all
- **No performance impact**: Direct delegation with zero overhead

---

## Testing Strategy

### Unit Tests

- Verify each `Using*` method delegates correctly to its `Get*` counterpart
- Verify exception behavior (throwing vs. silent)
- Verify return types (chainable vs. result)
- Verify `*OrDefault` silent behavior
- Verify `UsingEach` iteration over all instances
- Verify `UsingAll` receives full collection

### Integration Tests

- Fluent chaining scenarios (multiple `Using*` calls in sequence)
- Mixed required/optional capability usage
- Empty collection handling (`UsingEach`, `UsingAll`)
- Exception propagation from user delegates

---

## Documentation Notes

- Position `Using*` as **convenience methods for inline usage**
- Recommend using `Get*` when you need to store/reuse the capability instance
- Recommend using `Using*` when you immediately invoke and don't need the instance afterward
- Document chainability patterns and when to break chains (when you need a result)
- Add examples showing `First` vs. `Each` vs. `All` distinctions

---

## Future Work (non-breaking extensions)

- **Diagnostics**: Enhanced exceptions that show available capabilities, positions, or composition context
- **Analyzers**: Suggest `Using*` over `Get*` + immediate invocation patterns
- **Additional overloads**: If new retrieval patterns are added to `IComposition`, mirror them with corresponding `Using*` methods

---

## Decision Rationale

The `Using*` extensions solve a real ergonomic pain point (verbose retrieve-and-use patterns) without introducing semantic complexity or behavioral changes. By mirroring the existing `Get*` API structure, they feel like a natural extension of the framework rather than a separate concept.

The explicit position/collection specifiers (`First`, `Last`, `Each`, `All`) prevent ambiguity and make call sites self-documenting, aligning with the framework's design philosophy of clarity over brevity.

---

## Summary

Introduce 10 `Using*` extension methods that provide fluent, chainable, inline capability usage while maintaining perfect semantic consistency with the existing `IComposition` API. These are pure convenience wrappers with zero behavioral changes.
