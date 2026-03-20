# API Overview

Complete method reference for all public types in `Cocoar.Capabilities`.

## CapabilityScope

```csharp
public class CapabilityScope : IDisposable
```

| Member | Type | Description |
|--------|------|-------------|
| `CapabilityScope(options?)` | Constructor | Create scope with optional configuration |
| `Compose(subject, useRegistry?)` | Method | Create a `Composer` for the subject |
| `Recompose(composition, useRegistry?)` | Method | Create a `Composer` from an existing composition |
| `Composers` | Property | `ComposerRegistryApi` — access the composer registry |
| `Compositions` | Property | `CompositionRegistryApi` — access the composition registry |
| `Owner` | Property | `ScopeOwnerApi` — owner context management |
| `Anchors` | Property | `ScopeAnchorsApi` — anchors context management |
| `Dispose()` | Method | Clean up registry resources |

## CapabilityScope&lt;TOwner&gt;

```csharp
public class CapabilityScope<TOwner> : CapabilityScope where TOwner : class
```

| Member | Type | Description |
|--------|------|-------------|
| `CapabilityScope(owner)` | Constructor | Create scope with typed owner |
| `CapabilityScope(owner, options)` | Constructor | Create scope with typed owner and options |
| `Owner` | Property | `ScopeOwnerApi<TOwner>` — typed owner API |

## Composer

```csharp
public sealed class Composer
```

| Member | Signature | Description |
|--------|-----------|-------------|
| `Subject` | `object` | The subject being composed |
| `Add` | `Add(object, int?)` | Add capability with optional order |
| `Add` | `Add(object, Func<object, int>)` | Add with order selector |
| `AddAs<TContract>` | `AddAs<T>(object, int?)` | Add under contract type |
| `AddAs<TContract>` | `AddAs<T>(object, Func<object, int>)` | Add under contract with order selector |
| `TryAdd<T>` | `TryAdd<T>(T, int?)` | Add if type not already present |
| `TryAdd<T>` | `TryAdd<T>(T, Func<object, int>)` | Try-add with order selector |
| `TryAddAs<T>` | `TryAddAs<T>(object, int?)` | Try-add under contract |
| `TryAddAs<T>` | `TryAddAs<T>(object, Func<object, int>)` | Try-add under contract with order selector |
| `WithPrimary` | `WithPrimary(IPrimaryCapability?)` | Set or clear primary capability |
| `Has<T>` | `bool` | Check if capability type exists |
| `HasPrimary` | `bool` | Check if primary is set |
| `RemoveWhere` | `RemoveWhere(Func<object, bool>)` | Remove matching capabilities |
| `Build` | `Build(bool?)` | Create immutable composition |

All mutation methods return `Composer` for fluent chaining.

## IComposition

```csharp
public interface IComposition
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Subject` | `object` | The subject this composition belongs to |
| `TotalCapabilityCount` | `int` | Total capabilities across all types |

### Query Methods

| Method | Returns | Throws | Description |
|--------|---------|--------|-------------|
| `GetAll<T>()` | `IReadOnlyList<T>` | — | All capabilities of type |
| `GetAll()` | `IReadOnlyList<object>` | — | All capabilities |
| `GetFirstOrDefault<T>()` | `T?` | — | First or null |
| `GetLastOrDefault<T>()` | `T?` | — | Last or null |
| `GetRequiredFirst<T>()` | `T` | `InvalidOperationException` | First or throw |
| `GetRequiredLast<T>()` | `T` | `InvalidOperationException` | Last or throw |
| `TryGetFirst<T>(out T)` | `bool` | — | Try-pattern first |
| `TryGetLast<T>(out T)` | `bool` | — | Try-pattern last |
| `Has<T>()` | `bool` | — | Has any of type |
| `Count<T>()` | `int` | — | Count of type |

### Primary Capability Methods

| Method | Returns | Throws | Description |
|--------|---------|--------|-------------|
| `HasPrimary()` | `bool` | — | Has any primary |
| `HasPrimary<T>()` | `bool` | — | Has primary of type |
| `GetPrimary()` | `IPrimaryCapability` | `InvalidOperationException` | Get primary or throw |
| `GetPrimaryOrDefault()` | `IPrimaryCapability?` | — | Get primary or null |
| `TryGetPrimary(out)` | `bool` | — | Try-pattern |
| `GetRequiredPrimaryAs<T>()` | `T` | `InvalidOperationException` | Typed primary or throw |
| `GetPrimaryOrDefaultAs<T>()` | `T?` | — | Typed primary or null |
| `TryGetPrimaryAs<T>(out T)` | `bool` | — | Typed try-pattern |

## Using* Extensions

Extension methods on `IComposition` (namespace: `Cocoar.Capabilities`).

| Method | Returns | Description |
|--------|---------|-------------|
| `UsingFirst<T>(Action<T>)` | `IComposition` | Use first capability (throws if missing) |
| `UsingFirst<T, R>(Func<T, R>)` | `R` | Transform first capability |
| `UsingFirstOrDefault<T>(Action<T>)` | `IComposition` | Use first if exists |
| `UsingLast<T>(Action<T>)` | `IComposition` | Use last capability (throws if missing) |
| `UsingLast<T, R>(Func<T, R>)` | `R` | Transform last capability |
| `UsingLastOrDefault<T>(Action<T>)` | `IComposition` | Use last if exists |
| `UsingEach<T>(Action<T>)` | `IComposition` | Apply to each capability |
| `UsingEach<T, R>(Func<T, R>)` | `IReadOnlyList<R>` | Transform each capability |
| `UsingAll<T>(Action<IReadOnlyList<T>>)` | `IComposition` | Use full collection |
| `UsingAll<T, R>(Func<IReadOnlyList<T>, R>)` | `R` | Transform full collection |

## ScopeOwnerApi

| Method | Returns | Description |
|--------|---------|-------------|
| `Set(object)` | `ScopeOwnerApi` | Set owner (throws if already set) |
| `Replace(object)` | `ScopeOwnerApi` | Replace owner |
| `Get<T>()` | `T?` | Get owner or null if not set, collected, or wrong type |
| `GetOrThrow<T>()` | `T` | Get owner or throw (`InvalidOperationException` / `InvalidCastException`) |
| `TryGet<T>(out T?)` | `bool` | Try-pattern get |
| `Compose(useRegistry?)` | `Composer` | Compose on owner |
| `ComposeFor<T>(useRegistry?)` | `Composer` | Typed compose on owner |
| `GetComposition()` | `Composition?` | Get owner's composition |
| `GetRequiredComposition()` | `Composition` | Get required or throw |
| `TryGetComposition(out)` | `bool` | Try-pattern composition |
| `GetCompositionFor<T>()` | `Composition?` | Typed composition |
| `GetRequiredCompositionFor<T>()` | `Composition` | Typed required or throw |
| `GetComposer()` | `Composer?` | Get owner's composer |
| `GetRequiredComposer()` | `Composer` | Get required or throw |
| `TryGetComposer(out)` | `bool` | Try-pattern composer |
| `GetComposerFor<T>()` | `Composer?` | Typed composer |
| `GetRequiredComposerFor<T>()` | `Composer` | Typed required or throw |
| `Scope` | `CapabilityScope` | Return to scope |

## ScopeOwnerApi&lt;TOwner&gt;

| Method | Returns | Description |
|--------|---------|-------------|
| `Get()` | `TOwner` | Get typed owner |
| `TryGet(out TOwner?)` | `bool` | Try-pattern get |
| `Compose(useRegistry?)` | `Composer` | Compose on owner |
| `GetComposition()` | `Composition?` | Get composition |
| `GetRequiredComposition()` | `Composition` | Required or throw |
| `TryGetComposition(out)` | `bool` | Try-pattern composition |
| `GetComposer()` | `Composer?` | Get composer |
| `GetRequiredComposer()` | `Composer` | Required or throw |
| `TryGetComposer(out)` | `bool` | Try-pattern composer |
| `Scope` | `CapabilityScope` | Return to scope |

## ScopeAnchorsApi

### Anchor Management

| Method | Returns | Description |
|--------|---------|-------------|
| `Set<T>(T)` | `ScopeAnchorsApi` | Set typed anchor |
| `Set(string, object)` | `ScopeAnchorsApi` | Set named anchor |
| `Get<T>()` | `T?` | Get typed anchor or null |
| `GetOrThrow<T>()` | `T` | Get typed anchor or throw |
| `TryGet<T>(out T?)` | `bool` | Typed try-pattern |
| `Get(string)` | `object?` | Get named anchor or null |
| `GetOrThrow(string)` | `object` | Get named anchor or throw |
| `TryGet(string, out object?)` | `bool` | Named try-pattern |
| `Scope` | `CapabilityScope` | Return to scope |

### Composition/Composer Access (typed)

| Method | Returns | Description |
|--------|---------|-------------|
| `ComposeFor<T>(useRegistry?)` | `Composer` | Compose on typed anchor |
| `GetCompositionFor<T>()` | `Composition?` | Get composition |
| `GetRequiredCompositionFor<T>()` | `Composition` | Required or throw |
| `TryGetCompositionFor<T>(out)` | `bool` | Try-pattern |
| `GetComposerFor<T>()` | `Composer?` | Get composer |
| `GetRequiredComposerFor<T>()` | `Composer` | Required or throw |
| `TryGetComposerFor<T>(out)` | `bool` | Try-pattern |

### Composition/Composer Access (named)

| Method | Returns | Description |
|--------|---------|-------------|
| `Compose(string, useRegistry?)` | `Composer` | Compose on named anchor |
| `GetComposition(string)` | `Composition?` | Get composition |
| `GetRequiredComposition(string)` | `Composition` | Required or throw |
| `TryGetComposition(string, out)` | `bool` | Try-pattern |
| `GetComposer(string)` | `Composer?` | Get composer |
| `GetRequiredComposer(string)` | `Composer` | Required or throw |
| `TryGetComposer(string, out)` | `bool` | Try-pattern |

## CompositionRegistryApi

| Method | Returns | Description |
|--------|---------|-------------|
| `TryGet<T>(T, out IComposition)` | `bool` | Try-get by typed subject |
| `TryGet(object, out IComposition)` | `bool` | Try-get by subject |
| `GetOrDefault<T>(T)` | `IComposition?` | Get or null |
| `GetOrDefault(object)` | `IComposition?` | Get or null |
| `GetRequired<T>(T)` | `IComposition` | Get or throw |
| `GetRequired(object)` | `IComposition` | Get or throw |
| `Remove<T>(T)` | `bool` | Remove by typed subject |
| `Remove(object)` | `bool` | Remove by subject |

## ComposerRegistryApi

| Method | Returns | Description |
|--------|---------|-------------|
| `TryGet<T>(T, out Composer?)` | `bool` | Try-get composer |
| `GetOrDefault<T>(T)` | `Composer?` | Get or null |
| `GetRequired<T>(T)` | `Composer` | Get or throw |
| `Register<T>(T, Composer, bool)` | `void` | Register composer |
| `Remove<T>(T)` | `bool` | Remove composer |

## Other Types

### IPrimaryCapability

```csharp
public interface IPrimaryCapability { }
```

Marker interface for primary capabilities.

### ISubjectKeyMapper

```csharp
public interface ISubjectKeyMapper
{
    bool CanHandle(Type subjectType);
    object Map(object subject);
}
```

### CapabilityScopeOptions

```csharp
public record CapabilityScopeOptions
{
    public bool UseComposerRegistry { get; init; } = true;
    public bool UseCompositionRegistry { get; init; } = true;
    public IEnumerable<ISubjectKeyMapper>? SubjectKeyMappers { get; init; }
}
```

### ReadOnlyListExtensions

```csharp
public static void ForEach<T>(this IReadOnlyList<T> list, Action<T> action)
```

Zero-allocation iteration helper.

## Thread Safety

| Type | Thread-Safe? | Notes |
|------|-------------|-------|
| `CapabilityScope` | Yes | Registries use concurrent collections |
| `Composer` | No | Use within a single thread, then `Build()` |
| `IComposition` | Yes | Fully immutable after creation |
| `ScopeOwnerApi` | Yes | Weak reference access is thread-safe |
| `ScopeAnchorsApi` | Yes | Dictionary access is thread-safe |
