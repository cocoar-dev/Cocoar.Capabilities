# Cocoar.Capabilities - API Reference

Complete API reference for the Cocoar.Capabilities library.

## Table of Contents

- [CapabilityScope](#capabilityscope)
- [CapabilityScope&lt;TOwner&gt;](#capabilityscopetowner)
- [CapabilityScopeOptions](#capabilityscopeoptions)
- [ScopeOwnerApi](#scopeownerapi)
- [ScopeOwnerApi&lt;TOwner&gt;](#scopeownerapitowner)
- [ScopeAnchorsApi](#scopeanchorsapi)
- [Composer](#composer)
- [IComposition](#icomposition)
- [Using Extensions](#using-extensions)
- [IPrimaryCapability](#iprimarycapability)
- [ComposerRegistryApi](#composerregistryapi)
- [CompositionRegistryApi](#compositionregistryapi)

---

## CapabilityScope

Entry point for all capability operations. Manages the lifecycle of composers and compositions.

### Constructors

| Constructor | Description |
|------------|-------------|
| `CapabilityScope(CapabilityScopeOptions? options = null)` | Creates a new capability scope with the specified options |

### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `For(object subject, bool? useRegistry = null)` | `Composer` | Creates a new composer for the specified subject |
| `Recompose(IComposition composition, bool? useRegistry = null)` | `Composer` | Creates a new composer based on an existing composition |
| `Dispose()` | `void` | Releases all resources used by the scope |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Composers` | `ComposerRegistryApi` | Provides access to the composer registry |
| `Compositions` | `CompositionRegistryApi` | Provides access to the composition registry |
| `Owner` | `ScopeOwnerApi` | Provides access to owner management operations |
| `Anchors` | `ScopeAnchorsApi` | Provides access to anchor management operations |

### Example

```csharp
using var scope = new CapabilityScope();
var composer = scope.Compose(myObject);
var composition = composer.Add(new MyCapability()).Build();
```

---

## CapabilityScope&lt;TOwner&gt;

A capability scope with a strongly-typed owner. The owner is immutable and set at construction time, providing compile-time type safety for all owner operations.

### Constructors

| Constructor | Description |
|------------|-------------|
| `CapabilityScope(TOwner owner)` | Creates a new typed scope with the specified owner |
| `CapabilityScope(TOwner owner, CapabilityScopeOptions? options)` | Creates a new typed scope with the specified owner and options |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Owner` | `ScopeOwnerApi<TOwner>` | Provides access to strongly-typed owner operations |
| `Composers` | `ComposerRegistryApi` | Provides access to the composer registry (inherited) |
| `Compositions` | `CompositionRegistryApi` | Provides access to the composition registry (inherited) |
| `Anchors` | `ScopeAnchorsApi` | Provides access to anchor management operations (inherited) |

### Example

```csharp
// Create a typed scope with owner
var configManager = new ConfigurationManager();
using var scope = new CapabilityScope<ConfigurationManager>(configManager);

// Owner methods are strongly typed - no generic parameter needed!
var owner = scope.Owner.Get();  // Returns ConfigurationManager directly
var composer = scope.Owner.Compose();  // Composes for the owner

// With options
var options = new CapabilityScopeOptions { UseComposerRegistry = true };
using var typedScope = new CapabilityScope<ConfigManager>(config, options);

// Can be used as base CapabilityScope
CapabilityScope baseScope = scope;
```

### Inheritance

`CapabilityScope<TOwner>` inherits from `CapabilityScope`, so it can be used anywhere a base `CapabilityScope` is expected. This enables patterns like:

```csharp
public class ConfigManagerScope : CapabilityScope<ConfigManager>
{
    public ConfigManagerScope(ConfigManager manager) : base(manager) { }
}
```

---

## CapabilityScopeOptions

Configuration options for a capability scope.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `UseComposerRegistry` | `bool` | `false` | Controls whether composers are automatically registered when created |
| `UseCompositionRegistry` | `bool` | `false` | Controls whether compositions are automatically registered when built |
| `SubjectKeyMappers` | `List<Func<object, object>>?` | `null` | Optional list of functions to canonicalize subject keys for registry lookups |

### Example

```csharp
var options = new CapabilityScopeOptions
{
    UseComposerRegistry = true,
    UseCompositionRegistry = true,
    SubjectKeyMappers = new List<Func<object, object>>
    {
        obj => obj is string s ? s.ToLowerInvariant() : obj
    }
};
```

---

## ScopeOwnerApi

Provides scope ownership management operations. Owners are single-valued context objects associated with a scope (e.g., user, request, pipeline). Each scope can have at most one owner.

Access via `scope.Owner.*`

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Scope` | `CapabilityScope` | Returns the associated scope (for fluent chaining back to scope) |

### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Set(object owner)` | `ScopeOwnerApi` | Sets the owner for the scope (throws if already set) |
| `Replace(object owner)` | `ScopeOwnerApi` | Replaces the existing owner with a new one |
| `Get<T>()` | `T` | Gets the owner cast to type T, throws if not set or wrong type |
| `GetOrThrow<T>()` | `T` | Alias for `Get<T>()` |
| `TryGet<T>(out T? owner)` | `bool` | Tries to get the owner as type T, returns true if successful |
| `Compose(bool? useRegistry = null)` | `Composer` | Creates a composer for the owner |
| `ComposeFor<T>(bool? useRegistry = null)` | `Composer` | Creates a composer for the owner if it's of type T |
| `GetComposition()` | `Composition?` | Gets the existing composition for the owner, or null |
| `TryGetComposition(out Composition? composition)` | `bool` | Tries to get the composition, returns true if found |
| `GetCompositionFor<T>()` | `Composition?` | Gets the composition if owner is of type T, or null |
| `GetRequiredComposition()` | `Composition` | Gets the composition, throws if not found |
| `GetRequiredCompositionFor<T>()` | `Composition` | Gets the composition if owner is type T, throws if not found |
| `GetComposer()` | `Composer?` | Gets the composer from registry, or null |
| `TryGetComposer(out Composer? composer)` | `bool` | Tries to get the composer from registry, returns true if found |
| `GetComposerFor<T>()` | `Composer?` | Gets the composer if owner is of type T, or null |
| `GetRequiredComposer()` | `Composer` | Gets the composer from registry, throws if not found |
| `GetRequiredComposerFor<T>()` | `Composer` | Gets the composer if owner is type T, throws if not found |

### Exceptions

| Method | Exception | Condition |
|--------|-----------|-----------|
| All methods | `ObjectDisposedException` | If scope is disposed |
| `Set` | `InvalidOperationException` | If owner is already set |
| `Replace` | `InvalidOperationException` | If owner is not set |
| `GetOrThrow` | `InvalidOperationException` | If owner is not set |
| `Compose` | `InvalidOperationException` | If owner is not set |

### Example

```csharp
using var scope = new CapabilityScope();

// Set owner once
scope.Owner.Set(currentUser);

// Get owner (various patterns)
var owner = scope.Owner.Get();
var requiredOwner = scope.Owner.GetOrThrow();
if (scope.Owner.TryGet(out var o)) { /* use o */ }

// Replace owner (runtime replacement scenario)
scope.Owner.Replace(newUser);

// Compose capabilities for owner
var composition = scope.Owner.Compose(c => c
    .Add(new UserPermissions())
    .Add(new AuditLog()));

// Fluent chaining back to scope
scope.Owner.Set(currentUser).Scope.Anchors.Set<ILogger>(logger);
```

---

## ScopeOwnerApi&lt;TOwner&gt;

Provides strongly-typed owner operations for `CapabilityScope<TOwner>`. All methods work with the concrete owner type without needing generic parameters.

Access via `scope.Owner` on a `CapabilityScope<TOwner>` instance.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Scope` | `CapabilityScope` | Returns the associated scope |

### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Get()` | `TOwner` | Gets the owner (always succeeds since owner is immutable) |
| `TryGet(out TOwner? owner)` | `bool` | Gets the owner (always returns true) |
| `Compose(bool? useRegistry = null)` | `Composer` | Creates a composer for the owner |
| `GetComposition()` | `Composition?` | Gets the existing composition for the owner, or null |
| `TryGetComposition(out Composition? composition)` | `bool` | Tries to get the composition, returns true if found |
| `GetRequiredComposition()` | `Composition` | Gets the composition, throws if not found |
| `GetComposer()` | `Composer?` | Gets the composer from registry, or null |
| `TryGetComposer(out Composer? composer)` | `bool` | Tries to get the composer from registry, returns true if found |
| `GetRequiredComposer()` | `Composer` | Gets the composer from registry, throws if not found |

### Exceptions

| Method | Exception | Condition |
|--------|-----------|-----------|
| All methods | `ObjectDisposedException` | If scope is disposed |
| `GetRequiredComposition()` | `InvalidOperationException` | If no composition exists |
| `GetRequiredComposer()` | `InvalidOperationException` | If no composer in registry |

### Example

```csharp
var config = new ConfigurationManager();
using var scope = new CapabilityScope<ConfigurationManager>(config);

// No generic parameter needed!
var owner = scope.Owner.Get();  // Returns ConfigurationManager
var composer = scope.Owner.Compose();

// Try-pattern
if (scope.Owner.TryGetComposition(out var composition))
{
    // Use composition
}

// Build and retrieve
scope.Owner.Compose()
    .Add(new ConfigCapability("key", "value"))
    .Build();

var comp = scope.Owner.GetRequiredComposition();
```

---

## ScopeAnchorsApi

Provides scope anchor management operations. Anchors are keyed context objects associated with a scope (e.g., logger, configuration, pipeline). Use typed anchors for compile-time safety or named anchors for string keys.

Access via `scope.Anchors.*`

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Scope` | `CapabilityScope` | Returns the associated scope (for fluent chaining back to scope) |

### Methods - Typed Anchors

| Method | Returns | Description |
|--------|---------|-------------|
| `Set<T>(T anchor)` | `ScopeAnchorsApi` | Sets a typed anchor (throws if already set) |
| `Get<T>()` | `T?` | Gets a typed anchor, or null/default if not set |
| `GetOrThrow<T>()` | `T` | Gets a typed anchor, throws if not set (alias: `Require<T>()`) |
| `TryGet<T>(out T anchor)` | `bool` | Tries to get a typed anchor, returns true if successful |
| `ComposeFor<T>(bool? useRegistry = null)` | `Composer` | Creates a composer for a typed anchor (throws if not set) |
| `GetCompositionFor<T>()` | `Composition?` | Gets the existing composition for a typed anchor, or null |
| `TryGetCompositionFor<T>(out Composition? composition)` | `bool` | Tries to get composition for typed anchor, returns true if exists |
| `GetRequiredCompositionFor<T>()` | `Composition` | Gets composition for typed anchor, throws if not found |
| `GetComposerFor<T>()` | `Composer?` | Gets composer from registry for typed anchor, or null |
| `TryGetComposerFor<T>(out Composer? composer)` | `bool` | Tries to get composer from registry, returns true if found |
| `GetRequiredComposerFor<T>()` | `Composer` | Gets composer from registry, throws if not found |
| `Compose<T>(bool? useRegistry = null)` | `Composer` | **[Obsolete]** Use `ComposeFor<T>()` instead |
| `GetComposition<T>()` | `Composition?` | **[Obsolete]** Use `GetCompositionFor<T>()` instead |
| `TryGetComposition<T>(out Composition?)` | `bool` | **[Obsolete]** Use `TryGetCompositionFor<T>()` instead |
| `GetRequiredComposition<T>()` | `Composition` | **[Obsolete]** Use `GetRequiredCompositionFor<T>()` instead |
| `GetComposer<T>()` | `Composer?` | **[Obsolete]** Use `GetComposerFor<T>()` instead |
| `TryGetComposer<T>(out Composer?)` | `bool` | **[Obsolete]** Use `TryGetComposerFor<T>()` instead |
| `GetRequiredComposer<T>()` | `Composer` | **[Obsolete]** Use `GetRequiredComposerFor<T>()` instead |

### Methods - Named Anchors

| Method | Returns | Description |
|--------|---------|-------------|
| `Set(string name, object anchor)` | `ScopeAnchorsApi` | Sets a named anchor (throws if already set) |
| `Get(string name)` | `object?` | Gets a named anchor, or null if not set |
| `GetOrThrow(string name)` | `object` | Gets a named anchor, throws if not set (alias: `Require(string)`) |
| `TryGet(string name, out object anchor)` | `bool` | Tries to get a named anchor, returns true if successful |
| `Compose(string name, Action<Composer> composerSetup)` | `IComposition` | Creates a composition for a named anchor (throws if not set) |
| `GetComposition(string name)` | `IComposition?` | Gets the existing composition for a named anchor, or null |
| `TryGetComposition(string name, out Composition? composition)` | `bool` | Tries to get composition for named anchor, returns true if exists |
| `GetRequiredComposition(string name)` | `Composition` | Gets composition for named anchor, throws if not found |
| `GetComposer(string name)` | `Composer?` | Gets composer from registry for named anchor, or null |
| `TryGetComposer(string name, out Composer? composer)` | `bool` | Tries to get composer from registry, returns true if found |
| `GetRequiredComposer(string name)` | `Composer` | Gets composer from registry, throws if not found |

### Exceptions

| Method | Exception | Condition |
|--------|-----------|-----------|
| All methods | `ObjectDisposedException` | If scope is disposed |
| `Set<T>` / `Set(name, ...)` | `InvalidOperationException` | If anchor is already set |
| `GetOrThrow<T>` / `GetOrThrow(name)` | `InvalidOperationException` | If anchor is not set or has been garbage collected |
| `Compose<T>` / `Compose(name, ...)` | `InvalidOperationException` | If anchor is not set or has been garbage collected |
| `GetRequiredComposition<T>` / `GetRequiredComposition(name)` | `InvalidOperationException` | If anchor not set, has been collected, or no composition exists |
| `GetRequiredComposer<T>` / `GetRequiredComposer(name)` | `InvalidOperationException` | If anchor not set, has been collected, or composer not in registry |

### Example

```csharp
using var scope = new CapabilityScope();

// Typed anchors (compile-time safety)
scope.Anchors.Set<ILogger>(logger);
scope.Anchors.Set<IConfiguration>(config);

var log = scope.Anchors.Get<ILogger>();
var requiredConfig = scope.Anchors.GetOrThrow<IConfiguration>();

// Named anchors (runtime flexibility)
scope.Anchors.Set("current-pipeline", pipeline1);
scope.Anchors.Set("previous-pipeline", pipeline0);

if (scope.Anchors.TryGet("current-pipeline", out var p))
{
    // use p
}

// Compose capabilities for anchors
var composition = scope.Anchors.Compose<ILogger>(c => c
    .Add(new LogFormatter())
    .Add(new LogFilter()));

// Fluent chaining
scope.Anchors
    .Set<ILogger>(logger)
    .Set<IConfiguration>(config)
    .Scope.Owner.Set(currentUser);
```

---

## Composer

Fluent builder for creating capability compositions.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Subject` | `object` | Gets the subject this composer is building capabilities for |

### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Add(object capability, int? order = null)` | `Composer` | Adds a capability to the composition |
| `Add(object capability, Func<object, int> orderSelector)` | `Composer` | Adds a capability with order determined by a selector function |
| `AddAs<TContract>(object capability, int? order = null)` | `Composer` | Adds a capability and registers it under specific contract type(s) |
| `AddAs<TContract>(object capability, Func<object, int> orderSelector)` | `Composer` | Adds a capability under specific contract(s) with order selector |
| `TryAdd<TCapability>(TCapability capability, int? order = null)` | `Composer` | Adds a capability only if it doesn't already exist |
| `TryAdd<TCapability>(TCapability capability, Func<object, int> orderSelector)` | `Composer` | Try-add variant with order selector |
| `Has<TCapability>()` | `bool` | Checks if a capability of the specified type has been added |
| `HasPrimary()` | `bool` | Checks if a primary capability has been added |
| `Build(bool? useRegistry = null)` | `IComposition` | Builds the final immutable composition |

### Exceptions

| Method | Exception | Condition |
|--------|-----------|-----------|
| `Add` | `ArgumentNullException` | If capability is null |
| `Add` | `InvalidOperationException` | If adding a second primary capability or if already built |
| `Build` | `InvalidOperationException` | If already built |

### Example

```csharp
var composition = scope.Compose(subject)
    .Add(new Capability1(), order: 10)
    .Add(new Capability2(), order: 5)
    .AddAs<(IContract1, IContract2)>(new MultiContract())
    .TryAdd(new OptionalCapability())
    .Build();
```

---

## IComposition

Immutable collection of capabilities attached to a subject. Thread-safe.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Subject` | `object` | Gets the subject this composition is attached to |
| `TotalCapabilityCount` | `int` | Gets the total number of capabilities in the composition |

### Capability Query Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetAll<TCapability>()` | `IReadOnlyList<TCapability>` | Retrieves all capabilities of the specified type in order |
| `GetFirstOrDefault<TCapability>()` | `TCapability?` | Gets the first capability of the specified type, or null if none exists |
| `GetRequiredFirst<TCapability>()` | `TCapability` | Gets the first capability of the specified type (throws if not found) |
| `TryGetFirst<TCapability>(out TCapability capability)` | `bool` | Tries to get the first capability of the specified type |
| `GetLastOrDefault<TCapability>()` | `TCapability?` | Gets the last capability of the specified type, or null if none exists |
| `GetRequiredLast<TCapability>()` | `TCapability` | Gets the last capability of the specified type (throws if not found) |
| `TryGetLast<TCapability>(out TCapability capability)` | `bool` | Tries to get the last capability of the specified type |
| `Has<TCapability>()` | `bool` | Checks if any capability of the specified type exists |
| `Count<TCapability>()` | `int` | Gets the count of capabilities of the specified type |

### Primary Capability Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `HasPrimary()` | `bool` | Checks if a primary capability exists |
| `HasPrimary<TPrimaryCapability>()` | `bool` | Checks if a primary capability of specific type exists |
| `GetPrimary()` | `IPrimaryCapability` | Gets the primary capability (throws if not found) |
| `GetPrimaryOrDefault()` | `IPrimaryCapability?` | Gets the primary capability or null |
| `TryGetPrimary(out IPrimaryCapability primary)` | `bool` | Tries to get the primary capability |
| `GetPrimaryOrDefaultAs<TPrimaryCapability>()` | `TPrimaryCapability?` | Gets the primary capability cast to a specific type, or null |
| `GetRequiredPrimaryAs<TPrimaryCapability>()` | `TPrimaryCapability` | Gets the primary capability cast to a specific type (throws if not found) |
| `TryGetPrimaryAs<TPrimaryCapability>(out TPrimaryCapability primary)` | `bool` | Tries to get the primary capability as a specific type |

### Exceptions

| Method | Exception | Condition |
|--------|-----------|-----------|
| `GetPrimary()` | `InvalidOperationException` | If no primary capability exists |
| `GetRequiredPrimaryAs<T>()` | `InvalidOperationException` | If primary capability doesn't exist or isn't of the specified type |
| `GetRequiredFirst<T>()` | `InvalidOperationException` | If no capability of the specified type exists |
| `GetRequiredLast<T>()` | `InvalidOperationException` | If no capability of the specified type exists |

### Example

```csharp
// Query capabilities
var validators = composition.GetAll<IValidator>();
var hasLogging = composition.Has<ILogger>();

// Get first capability (convenient when you expect only one)
var config = composition.GetFirstOrDefault<ConfigCapability>();
if (config != null)
{
    // Use config
}

// Or use Try pattern
if (composition.TryGetFirst<ConfigCapability>(out var cfg))
{
    // Use cfg
}

// Work with primary
if (composition.TryGetPrimary(out var primary))
{
    Console.WriteLine($"Primary: {primary}");
}

// Type-safe primary access
var userPrimary = composition.GetRequiredPrimaryAs<UserPrimaryCapability>();
```

---

## Using Extensions

Fluent extension methods for inline capability usage. These are convenience wrappers over the `Get*` methods that enable chainable, expressive usage patterns.

**Extension methods on `IComposition`**

### Single Instance Methods (First)

| Method | Returns | Description |
|--------|---------|-------------|
| `UsingFirst<T>(Action<T> use)` | `IComposition` | Uses the first T capability; throws if none exist. Chainable. |
| `UsingFirst<T, TResult>(Func<T, TResult> use)` | `TResult` | Uses the first T capability and returns a result; throws if none exist |
| `UsingFirstOrDefault<T>(Action<T> use)` | `IComposition` | Uses the first T capability if it exists; silent if missing. Chainable. |

### Single Instance Methods (Last)

| Method | Returns | Description |
|--------|---------|-------------|
| `UsingLast<T>(Action<T> use)` | `IComposition` | Uses the last T capability; throws if none exist. Chainable. |
| `UsingLast<T, TResult>(Func<T, TResult> use)` | `TResult` | Uses the last T capability and returns a result; throws if none exist |
| `UsingLastOrDefault<T>(Action<T> use)` | `IComposition` | Uses the last T capability if it exists; silent if missing. Chainable. |

### Multiple Instance Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `UsingEach<T>(Action<T> use)` | `IComposition` | Executes action for each T capability; silent if none exist. Chainable. |
| `UsingEach<T, TResult>(Func<T, TResult> use)` | `IReadOnlyList<TResult>` | Executes function for each T capability and collects results |
| `UsingAll<T>(Action<IReadOnlyList<T>> use)` | `IComposition` | Executes action with full collection of T capabilities. Chainable. |
| `UsingAll<T, TResult>(Func<IReadOnlyList<T>, TResult> use)` | `TResult` | Executes function with full collection and returns a result |

### Delegation Mapping

All `Using*` methods delegate directly to existing `IComposition` methods:

| `Using*` Method | Delegates To |
|----------------|--------------|
| `UsingFirst<T>()` | `GetRequiredFirst<T>()` |
| `UsingFirstOrDefault<T>()` | `GetFirstOrDefault<T>()` |
| `UsingLast<T>()` | `GetRequiredLast<T>()` |
| `UsingLastOrDefault<T>()` | `GetLastOrDefault<T>()` |
| `UsingEach<T>()` / `UsingAll<T>()` | `GetAll<T>()` |

### Examples

**Single instance usage:**

```csharp
// Required first (throws if missing)
composition.UsingFirst<ILogger>(log => log.Info("started"));

// Required first with result
var count = composition.UsingFirst<IRepository, int>(repo => repo.Count());

// Optional (silent if missing)
composition.UsingFirstOrDefault<ILogger>(log => log.Info("started"));

// Last instance (when order matters)
composition.UsingLast<IMiddleware>(m => m.Execute());
```

**Multiple instance usage:**

```csharp
// Process each capability individually
composition.UsingEach<IEventHandler>(handler => handler.Handle(evt));

// Map each to a result
var counts = composition.UsingEach<IRepository, int>(repo => repo.Count());
// Returns: [5, 12, 8]

// Aggregate operation on collection
var total = composition.UsingAll<IRepository, int>(
    repos => repos.Sum(r => r.Count())
);
// Returns: 25

// Collection operation (chainable)
composition.UsingAll<IPlugin>(plugins => 
{
    foreach (var p in plugins)
        p.Initialize();
});
```

**Fluent chaining:**

```csharp
composition
    .UsingFirst<ILogger>(log => log.Info("starting"))
    .UsingEach<IPlugin>(p => p.Initialize())
    .UsingFirstOrDefault<IOptionalFeature>(f => f.Configure())
    .UsingAll<IEventHandler>(handlers => 
    {
        foreach (var h in handlers)
            h.Register();
    })
    .UsingLast<IMetrics>(m => m.Inc("startup"));
```

**When to use `Using*` vs `Get*`:**

```csharp
// Use Get* when you need to store/reuse the instance
var logger = composition.GetRequiredFirst<ILogger>();
logger.Info("step 1");
logger.Info("step 2");

// Use Using* for inline/one-off usage
composition.UsingFirst<ILogger>(log => log.Info("one-off message"));

// Use Using* for fluent chaining
composition
    .UsingFirst<ILogger>(log => log.Info("starting"))
    .UsingEach<IValidator>(v => v.Validate());
```

### Exception Behavior

| Method | Behavior |
|--------|----------|
| `UsingFirst<T>()` / `UsingLast<T>()` | Throws if no instances exist |
| `UsingFirstOrDefault<T>()` / `UsingLastOrDefault<T>()` | Silent if missing (no-op) |
| `UsingEach<T>()` / `UsingAll<T>()` | Silent if collection is empty |

### Thread Safety

- ✅ All `Using*` methods are thread-safe (they operate on immutable `IComposition` instances)
- ✅ User-provided delegates may execute concurrently if the composition is shared across threads
- ⚠️ User code inside delegates is responsible for its own thread safety

### Performance

- **Zero allocation overhead** beyond the user delegate itself
- Direct delegation to existing `Get*` methods (no intermediate objects)
- Chainable methods return the same `IComposition` instance (no copying)

---

## IPrimaryCapability

Marker interface indicating a capability that should be the primary capability for an instance.

### Interface Definition

```csharp
public interface IPrimaryCapability { }
```

### Rules

| Rule | Description |
|------|-------------|
| **Single Primary** | Only one primary capability is allowed per composition |
| **Exception on Duplicate** | Attempting to add a second primary capability throws `InvalidOperationException` |
| **Specialized Retrieval** | Primary capabilities have dedicated retrieval methods on `IComposition` |

### Example

```csharp
public record UserPrimaryCapability(string UserId, string Name) : IPrimaryCapability;

public record DocumentPrimaryCapability(string Id, string Title) : IPrimaryCapability;

// Use in composition
var composition = scope.Compose(user)
    .Add(new UserPrimaryCapability("user123", "John Doe"))
    .Add(new AdminCapability()) // Non-primary, OK
    .Build();
```

---

## ComposerRegistryApi

Provides access to the composer registry for managing active composers.

### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Has(object subject)` | `bool` | Checks if a composer exists for the subject |
| `Find(object subject)` | `Composer` | Finds the composer for the subject (throws if not found) |
| `GetOrDefault(object subject)` | `Composer?` | Finds the composer or returns null |
| `TryGet(object subject, out Composer composer)` | `bool` | Tries to get the composer for the subject |

### Example

```csharp
if (scope.Composers.Has(document))
{
    var composer = scope.Composers.Find(document);
    // Composer is still being built
}
```

---

## CompositionRegistryApi

Provides access to the composition registry for managing built compositions.

### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Has(object subject)` | `bool` | Checks if a composition exists for the subject |
| `Find(object subject)` | `IComposition` | Finds the composition for the subject (throws if not found) |
| `GetOrDefault(object subject)` | `IComposition?` | Finds the composition or returns null |
| `TryGet(object subject, out IComposition composition)` | `bool` | Tries to find the composition for the subject |

### Example

```csharp
var composition = scope.Compositions.GetOrDefault(document);
if (composition != null)
{
    var capabilities = composition.GetAll<ICapability>();
}
```

---

## Performance Characteristics

| Operation | Complexity | Notes |
|-----------|-----------|-------|
| Capability Lookup | O(1) | Dictionary-based lookup |
| Add Capability | O(1) amortized | List growth |
| Build Composition | O(n log n) | Sorting by order value |
| Memory (Post-Build) | Zero allocation | For capability lookups |
| Registry Lookup | O(1) | With canonical key |

---

## Thread Safety

| Type | Thread Safety | Description |
|------|--------------|-------------|
| `CapabilityScope` | ❌ Not thread-safe | Each thread should use its own scope or external synchronization |
| `Composer` | ❌ Not thread-safe | Should not be shared across threads |
| `IComposition` | ✅ Thread-safe | Immutable and fully thread-safe, can be safely shared |
| Registries | ✅ Thread-safe | Internal locking for concurrent access |

---

## Best Practices

| Practice | Description |
|----------|-------------|
| **Always Dispose Scopes** | Use `using` statement to ensure proper cleanup |
| **Don't Reuse Composers** | Create new composers for each composition |
| **Enable Registries Sparingly** | Only enable when centralized management is needed |
| **Use Explicit Ordering** | Specify order values for predictable behavior |
| **Primary for Identity** | Use primary capabilities for the main "identity" of a subject |
| **Try-Add for Conditionals** | Use `TryAdd` when adding logic is complex |
| **Recompose for Modifications** | Never try to mutate compositions directly |

---

## Common Design Patterns

### Builder Pattern

```csharp
public class DocumentBuilder
{
    private readonly Composer _composer;

    public DocumentBuilder(CapabilityScope scope, object subject)
    {
        _composer = scope.Compose(subject);
    }

    public DocumentBuilder WithEditing() 
    {
        _composer.Add(new EditCapability());
        return this;
    }

    public IComposition Build() => _composer.Build();
}
```

### Strategy Pattern

```csharp
// Add strategies as capabilities, execute in order
var composition = scope.Compose(processor)
    .Add(new ValidationStrategy(), order: 1)
    .Add(new TransformationStrategy(), order: 2)
    .Add(new PersistenceStrategy(), order: 3)
    .Build();

foreach (var strategy in composition.GetAll<IStrategy>())
{
    strategy.Execute();
}
```

### Chain of Responsibility

```csharp
var composition = scope.Compose(request)
    .Add(new AuthenticationHandler(), order: 1)
    .Add(new AuthorizationHandler(), order: 2)
    .Add(new ValidationHandler(), order: 3)
    .Build();

foreach (var handler in composition.GetAll<IRequestHandler>())
{
    if (handler.CanHandle(request))
    {
        await handler.HandleAsync(request);
        break;
    }
}
```

### Decorator Pattern

```csharp
// Layer capabilities as decorators
var composition = scope.Compose(service)
    .Add(new LoggingDecorator(), order: 1)
    .Add(new CachingDecorator(), order: 2)
    .Add(new ValidationDecorator(), order: 3)
    .Build();
```

