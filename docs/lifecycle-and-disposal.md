# Lifecycle & Disposal

This library separates three concerns:

| Concern | Lifetime | Disposal Responsibility |
| ------- | -------- | ----------------------- |
| CapabilityScope | Explicit (Dispose or GC) | User (call Dispose when done) |
| Composer<T> | Ephemeral builder | Not disposable (transient) |
| Composition<T> | Immutable snapshot | User holds reference; not disposed by scope |
| Registry Entries | Internal index nodes | Disposed (owned resources) when scope disposed |

## Scope Disposal Semantics

Disposing a `CapabilityScope`:
- Prevents creation of new composers (`scope.For(...)` throws `ObjectDisposedException`).
- Stops registry usage (lookups / new registrations throw if they touch the disposed scope indirectly).
- Disposes any disposable objects referenced by value-type subject entries (and, if added later, any tracked disposables) via `CapabilityEntry.DisposeOwnedResources()`.
- Releases references to reference-type subject entries (stored in a `ConditionalWeakTable`), allowing GC to reclaim them naturally.

Already-built compositions remain fully usable because they are immutable and do not depend on scope internals once created.

## Why Compositions Aren't Disposed
`Composition<T>` is a pure data container (arrays + dictionaries). It does not own external resources. Disposing would add ceremony without benefit. If, in the future, compositions wrap disposables, an explicit `IDisposable` implementation can be introduced without breaking existing semantics.

## Holding References After Disposal
It is safe—and expected—to hold a composition reference after disposing the scope:
```csharp
var scope = new CapabilityScope(new CapabilityScopeOptions { UseCompositionRegistry = true });
var comp = scope.For("svc", useRegistry: true)
    .Add(new LoggingCapability<string>(LogLevel.Info, "cat"))
    .Build(useRegistry: true);

scope.Dispose();

// Still valid: immutable snapshot
var loggers = comp.GetAll<LoggingCapability<string>>();
```

Registry lookups after disposal will fail (or return false) because the registry has been torn down.

## Recommended Practices
- Dispose the scope when you are done registering or discovering compositions globally.
- Store compositions where you need them (DI container, cache, etc.).
- Do not assume registry disposal invalidates existing compositions.
- If you introduce capabilities that implement `IDisposable`, manage their disposal explicitly or extend the registry tracking to own them.

## Future Extension Hooks
If later you need explicit disposal for reference-type entries: add an internal tracking list during registration and enumerate it in `DefaultCapabilityRegistry.Dispose()` similar to value-type entries.

