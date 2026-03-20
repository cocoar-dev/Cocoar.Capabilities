# Anchors API

While the [Owner API](/guide/scope-context/owner-api) gives a scope a single identity, you often need to associate *additional* context objects — a database connection, a cache, an environment context. Anchors let you attach multiple named or typed objects to the scope and compose capabilities onto them.

Two kinds exist: **typed anchors** (keyed by type, one per type) and **named anchors** (keyed by string, for multiple instances of the same type).

## Typed Anchors

### Setting

```csharp
var db = new DatabaseConnection("connstr");
scope.Anchors.Set<DatabaseConnection>(db);
```

### Getting

```csharp
// Get — returns null if not set or collected
var db = scope.Anchors.Get<DatabaseConnection>();

// GetOrThrow — throws if not set or collected
var db = scope.Anchors.GetOrThrow<DatabaseConnection>();

// TryGet — out-parameter pattern
if (scope.Anchors.TryGet<DatabaseConnection>(out var db))
{
    db.Query("SELECT 1");
}
```

## Named Anchors

### Setting

```csharp
scope.Anchors.Set("primary-db", primaryDb);
scope.Anchors.Set("replica-db", replicaDb);
```

### Getting

```csharp
// Get — returns null if not set or collected
var db = scope.Anchors.Get("primary-db");

// GetOrThrow — throws if not set or collected
var db = scope.Anchors.GetOrThrow("primary-db");

// TryGet — out-parameter pattern
if (scope.Anchors.TryGet("primary-db", out var db))
{
    // use db
}
```

## Composing via Anchors

### Typed

```csharp
scope.Anchors.Set<DatabaseConnection>(db);
scope.Anchors.ComposeFor<DatabaseConnection>()
    .Add(new PoolingCapability { MaxSize = 10 })
    .Build();
```

### Named

```csharp
scope.Anchors.Set("cache", cache);
scope.Anchors.Compose("cache")
    .Add(new ExpirationCapability { Ttl = TimeSpan.FromMinutes(5) })
    .Build();
```

## Retrieving Compositions

### Typed

```csharp
Composition? comp = scope.Anchors.GetCompositionFor<DatabaseConnection>();
Composition required = scope.Anchors.GetRequiredCompositionFor<DatabaseConnection>();

if (scope.Anchors.TryGetCompositionFor<DatabaseConnection>(out var comp))
{
    // use composition
}
```

### Named

```csharp
Composition? comp = scope.Anchors.GetComposition("cache");
Composition required = scope.Anchors.GetRequiredComposition("cache");

if (scope.Anchors.TryGetComposition("cache", out var comp))
{
    // use composition
}
```

## Retrieving Composers

### Typed

```csharp
Composer? c = scope.Anchors.GetComposerFor<DatabaseConnection>();
Composer required = scope.Anchors.GetRequiredComposerFor<DatabaseConnection>();

if (scope.Anchors.TryGetComposerFor<DatabaseConnection>(out var composer))
{
    // composer still in registry
}
```

### Named

```csharp
Composer? c = scope.Anchors.GetComposer("cache");
Composer required = scope.Anchors.GetRequiredComposer("cache");

if (scope.Anchors.TryGetComposer("cache", out var composer))
{
    // use composer
}
```

## Fluent Chaining

Anchors methods return `ScopeAnchorsApi`:

```csharp
scope.Anchors
    .Set<DatabaseConnection>(db)
    .Set("cache", cache)
    .Scope  // return to scope
    .Compose("something")
    .Build();
```

## Weak References

Like the Owner API, anchors are stored as weak references. Anchored objects can be garbage collected without the scope preventing it.

## Typed vs Named — When to Use

| Use Case | Typed | Named |
|----------|-------|-------|
| One instance per type | Yes | — |
| Multiple instances of same type | — | Yes |
| Compile-time type safety | Yes | — |
| Dynamic/runtime keys | — | Yes |
