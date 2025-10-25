# Owner and Anchor Quick Reference

## When to Use

Use **Owner** when:
- ✅ Your scope has one primary/canonical subject
- ✅ You want semantic clarity about what "owns" the scope
- ✅ Examples: Configuration manager, Pipeline host, Workflow engine

Use **Typed Anchors** when:
- ✅ You need to associate well-known types with the scope
- ✅ You have multiple distinct contexts (environment, tenant, etc.)
- ✅ You want compile-time type safety

Use **Named Anchors** when:
- ✅ You need flexible, runtime-defined keys
- ✅ You have contextual data like "tenant:acme" or "environment"
- ✅ You want string-based lookups

## API Quick Reference

### Setting (Fluent)
```csharp
// Set owner (throws if already set)
scope.Owner.Set(myHost)
     .Scope.Anchors.Set<EnvironmentContext>(envCtx)
     .Scope.Anchors.Set("tenant", tenantCtx);

// Replace owner (explicitly overwrite)
scope.Owner.Replace(newHost);

// Alternative: all in one chain
scope.Owner.Set(myHost).Scope
     .Anchors.Set<EnvironmentContext>(envCtx).Scope
     .Anchors.Set("tenant", tenantCtx);
```

### Retrieving (Throwing)
```csharp
var owner = scope.Owner.Get<MyHost>();
var env = scope.Anchors.Get<EnvironmentContext>();
var tenant = scope.Anchors.Get("tenant");

// Alias methods (same behavior)
var owner = scope.Owner.GetOrThrow<MyHost>();
var env = scope.Anchors.GetOrThrow<EnvironmentContext>();
var tenant = scope.Anchors.GetOrThrow("tenant");
```

### Retrieving (Safe)
```csharp
if (scope.Owner.TryGet<MyHost>(out var owner)) { ... }
if (scope.Anchors.TryGet<EnvironmentContext>(out var env)) { ... }
if (scope.Anchors.TryGet("tenant", out var tenant)) { ... }
```

### Composing

There are three tiers of composition helpers:

**Tier 1: Direct subject composition**
```csharp
var subject = new MySubject();
scope.Compose(subject)
     .Add(new MyCapability())
     .Build();
```

**Tier 2: Owner composition**
```csharp
// Type-inferred from set owner
scope.Owner.Compose()
           .Add(new MyCapability())
           .Build();

// Explicit type check
scope.Owner.Compose<MyHost>()
           .Add(new MyCapability())
           .Build();
```

**Tier 3: Anchor composition**
```csharp
// For typed anchor
scope.Anchors.Compose<EnvironmentContext>()
             .Add(new EnvCapability())
             .Build();

// For named anchor
scope.Anchors.Compose("tenant")
             .Add(new TenantCapability())
             .Build();
```

## Common Patterns

### Pattern 1: Single Owner
```csharp
var manager = new ConfigurationManager();
using var scope = new CapabilityScope();
scope.Owner.Set(manager);

// Later, in extension code:
void EnrichConfig(CapabilityScope scope)
{
    var mgr = scope.Owner.Get<ConfigurationManager>();
    scope.Owner.Compose().Add(new ConfigCap()).Build();
}
```

### Pattern 2: Owner + Typed Anchors
```csharp
using var scope = new CapabilityScope();
scope.Owner.Set(pipeline).Scope
     .Anchors.Set(environment).Scope
     .Anchors.Set(tenant);

scope.Owner.Compose<Pipeline>().Add(...).Build();
scope.Anchors.Compose<Environment>().Add(...).Build();
scope.Anchors.Compose<Tenant>().Add(...).Build();
```

### Pattern 3: Named Anchors for Flexibility
```csharp
scope.Anchors.Set("primary-db", dbContext).Scope
     .Anchors.Set("cache", cacheContext).Scope
     .Anchors.Set("tenant:acme", tenantA).Scope
     .Anchors.Set("tenant:globex", tenantB);

scope.Anchors.Compose("primary-db").Add(...).Build();
scope.Anchors.Compose("tenant:acme").Add(...).Build();
```

### Pattern 4: Owner Replacement
```csharp
// Initial setup
var oldPipeline = new Pipeline("v1");
using var scope = new CapabilityScope();
scope.Owner.Set(oldPipeline); // Safe initialization

// Later, replace with new pipeline
var newPipeline = new Pipeline("v2");
scope.Owner.Replace(newPipeline); // Explicit replacement

// Set would throw here:
// scope.Owner.Set(anotherPipeline); // ❌ InvalidOperationException
```

## Error Handling

### Exception Matrix

| Method | Condition | Exception |
|--------|-----------|-----------|
| `Owner.Set(owner)` | Owner already set and alive | `InvalidOperationException` |
| `Owner.Set(owner)` | Null owner | `ArgumentNullException` |
| `Owner.Set(owner)` | Scope disposed | `ObjectDisposedException` |
| `Owner.Replace(owner)` | Null owner | `ArgumentNullException` |
| `Owner.Replace(owner)` | Scope disposed | `ObjectDisposedException` |
| `Owner.Get<T>()` | Owner not set | `InvalidOperationException` |
| `Owner.Get<T>()` | Owner garbage collected | `InvalidOperationException` |
| `Owner.Get<T>()` | Type mismatch | `InvalidCastException` |
| `Owner.Get<T>()` | Scope disposed | `ObjectDisposedException` |
| `Owner.TryGet<T>(out owner)` | Scope disposed | `ObjectDisposedException` |
| `Anchors.Set<T>(anchor)` | Null anchor | `ArgumentNullException` |
| `Anchors.Set<T>(anchor)` | Scope disposed | `ObjectDisposedException` |
| `Anchors.Set(key, anchor)` | Null key or anchor | `ArgumentNullException` |
| `Anchors.Set(key, anchor)` | Scope disposed | `ObjectDisposedException` |
| `Anchors.Get<T>()` | Anchor not set | `InvalidOperationException` |
| `Anchors.Get<T>()` | Anchor garbage collected | `InvalidOperationException` |
| `Anchors.Get<T>()` | Scope disposed | `ObjectDisposedException` |
| `Anchors.Get(key)` | Anchor not set | `InvalidOperationException` |
| `Anchors.Get(key)` | Anchor garbage collected | `InvalidOperationException` |
| `Anchors.Get(key)` | Null key | `ArgumentNullException` |
| `Anchors.Get(key)` | Scope disposed | `ObjectDisposedException` |
| `Anchors.TryGet<T>(out anchor)` | Scope disposed | `ObjectDisposedException` |
| `Anchors.TryGet(key, out anchor)` | Null key | `ArgumentNullException` |
| `Anchors.TryGet(key, out anchor)` | Scope disposed | `ObjectDisposedException` |

**Note:** `Try*` methods return `false` (instead of throwing) when owner/anchor is not set, garbage collected, or wrong type.
**Note:** `GetOrThrow` methods are aliases for `Get` methods (same behavior).

## Memory Management

All owner and anchors are stored as **WeakReference<object>**:
- ✅ Won't prevent garbage collection
- ✅ Safe for long-lived scopes
- ⚠️ Must keep subjects alive if needed
- ⚠️ Check for collection in long-running scenarios

**Named Anchors:** Keys use **Ordinal** string comparison (case-sensitive).

## Best Practices

1. **Set early**: Configure owner/anchors during scope initialization
2. **Use Owner.Set for initialization**: First-time owner setting (throws if duplicate)
3. **Use Owner.Replace for replacement**: Explicitly replace owner when needed
4. **Use Try* variants**: When subject lifetime is uncertain
5. **Type-safe when possible**: Prefer typed anchors over named
6. **Named for runtime keys**: Use when keys aren't known at compile time
7. **One owner max**: Semantic clarity - use anchors for additional subjects
8. **Weak refs are weak**: Keep subjects alive in calling code if needed
9. **Document keys**: If using named anchors, document the key strings
10. **Fluent chaining**: Use `.Scope` to return from API to scope for chaining

## Anti-Patterns

❌ Don't use owner for multiple subjects (use anchors instead)  
❌ Don't assume owner/anchors are always alive (check or use Try* variants)  
❌ Don't set new owner/anchors after scope is widely used (set early)  
❌ Don't use scope itself as a subject (use owner instead)  
❌ Don't use Owner.Set to replace an owner (use Owner.Replace for explicit intent)  

## See Also

- ADR: [Scope Owner and Anchors](../adr/2025-10-25-scope-owner-and-anchors.md)
- Tests: `src/Cocoar.Capabilities.Tests/OwnerAndAnchor*.cs`
