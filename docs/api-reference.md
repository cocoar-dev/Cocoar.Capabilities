# Cocoar.Capabilities - API Reference

Complete API reference for the Cocoar.Capabilities library.

## Table of Contents

- [CapabilityScope](#capabilityscope)
- [CapabilityScopeOptions](#capabilityscopeoptions)
- [Composer](#composer)
- [IComposition](#icomposition)
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

### Example

```csharp
using var scope = new CapabilityScope();
var composer = scope.For(myObject);
var composition = composer.Add(new MyCapability()).Build();
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
var composition = scope.For(subject)
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
var composition = scope.For(user)
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
| `FindOrDefault(object subject)` | `Composer?` | Finds the composer or returns null |
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
| `FindOrDefault(object subject)` | `IComposition?` | Finds the composition or returns null |
| `TryFind(object subject, out IComposition composition)` | `bool` | Tries to find the composition for the subject |

### Example

```csharp
var composition = scope.Compositions.FindOrDefault(document);
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
        _composer = scope.For(subject);
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
var composition = scope.For(processor)
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
var composition = scope.For(request)
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
var composition = scope.For(service)
    .Add(new LoggingDecorator(), order: 1)
    .Add(new CachingDecorator(), order: 2)
    .Add(new ValidationDecorator(), order: 3)
    .Build();
```
