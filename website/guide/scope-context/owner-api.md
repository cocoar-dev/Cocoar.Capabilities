# Owner API

When you pass a scope between components — to extension methods, helper classes, or across layers — receiving code often needs to know *who* created or manages the scope. The Owner API solves this by associating a single distinguished object with the scope.

This is the object that "owns" or manages the scope — for example, a service host, pipeline runner, or application root. It also serves as a convenient subject for composing capabilities onto the owner itself.

## Setting an Owner

```csharp
var host = new ServiceHost("MyApp");
var scope = new CapabilityScope();

scope.Owner.Set(host);
```

`Set()` throws if an owner is already set. Use `Replace()` for explicit replacement:

```csharp
scope.Owner.Replace(newHost);
```

## Fluent Chaining

Owner methods return `ScopeOwnerApi`, allowing chaining. Use `.Scope` to return to the scope:

```csharp
var scope = new CapabilityScope();
scope.Owner.Set(host).Scope.Compose("something").Add(cap).Build();
```

## Getting the Owner

```csharp
// Get — returns null if not set, collected, or wrong type
var host = scope.Owner.Get<ServiceHost>();

// GetOrThrow — throws if not set, collected, or wrong type
var host = scope.Owner.GetOrThrow<ServiceHost>();

// TryGet — out-parameter pattern
if (scope.Owner.TryGet<ServiceHost>(out var host))
{
    Console.WriteLine(host.Name);
}
```

## Composing via Owner

Compose capabilities directly on the owner:

```csharp
scope.Owner.Set(host);
scope.Owner.Compose()
    .Add(new HostCapability())
    .Build();
```

For typed composition (uses the owner's type as the subject key):

```csharp
scope.Owner.ComposeFor<ServiceHost>()
    .Add(new HostCapability())
    .Build();
```

## Retrieving Compositions via Owner

```csharp
// Direct
Composition? comp = scope.Owner.GetComposition();
Composition required = scope.Owner.GetRequiredComposition();

// Try-pattern
if (scope.Owner.TryGetComposition(out var composition))
{
    var caps = composition.GetAll<ICapability>();
}

// Typed
Composition? comp = scope.Owner.GetCompositionFor<ServiceHost>();
Composition required = scope.Owner.GetRequiredCompositionFor<ServiceHost>();
```

## Retrieving Composers via Owner

```csharp
Composer? composer = scope.Owner.GetComposer();
Composer required = scope.Owner.GetRequiredComposer();

if (scope.Owner.TryGetComposer(out var c))
{
    // Composer is still in registry
}

// Typed
Composer? composer = scope.Owner.GetComposerFor<ServiceHost>();
Composer required = scope.Owner.GetRequiredComposerFor<ServiceHost>();
```

## Weak References

The owner is stored as a weak reference. If the owner object is garbage collected, `Get<T>()` returns `null` and `GetOrThrow<T>()` throws. This prevents the scope from keeping objects alive unnecessarily.
