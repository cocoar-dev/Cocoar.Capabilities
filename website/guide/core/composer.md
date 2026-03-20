# Composer

The `Composer` is the fluent builder that attaches capabilities to a subject. You get one from `scope.Compose(subject)`.

## Adding Capabilities

```csharp
scope.Compose(myService)
    .Add(new LoggingCapability())
    .Add(new MetricsCapability())
    .Build();
```

Any object can be a capability — no base class or interface required.

## Adding with Order

```csharp
scope.Compose(myService)
    .Add(new ValidationStep(), order: 100)
    .Add(new AuthStep(), order: 200)
    .Add(new LoggingStep(), order: 300)
    .Build();
```

## Adding with Order Selector

```csharp
scope.Compose(pipeline)
    .Add(new Step { Priority = 10 }, c => ((Step)c).Priority)
    .Build();
```

## Primary Capability

Set the "identity" of a composition. Only one primary is allowed:

```csharp
scope.Compose(myService)
    .WithPrimary(new ServiceIdentity("Auth", "1.0"))
    .Add(new LoggingCapability())
    .Build();
```

The primary must implement `IPrimaryCapability`. Call `WithPrimary(null)` to clear.

## AddAs — Contract Registration

Register a capability under a specific contract type:

```csharp
// Single contract
scope.Compose(host)
    .AddAs<IPlugin>(new EmailPlugin())
    .Build();

// Multiple contracts with tuple syntax
scope.Compose(host)
    .AddAs<(IPlugin, IHealthCheck)>(new EmailPlugin())
    .Build();
```

## TryAdd — Conditional Registration

Only adds if no capability of that type exists yet:

```csharp
scope.Compose(myService)
    .TryAdd(new DefaultLogger())    // Added
    .TryAdd(new CustomLogger())     // Skipped — DefaultLogger already present
    .Build();
```

`TryAddAs<TContract>` checks against the contract type instead.

## Pre-Build Checks

```csharp
var composer = scope.Compose(myService)
    .Add(new LoggingCapability());

if (composer.Has<LoggingCapability>())  // true
    Console.WriteLine("Logging configured");

if (composer.HasPrimary())  // false
    Console.WriteLine("Has identity");
```

## RemoveWhere

Selectively remove capabilities before building:

```csharp
scope.Compose(myService)
    .Add(new CapA())
    .Add(new CapB())
    .RemoveWhere(c => c is CapB)
    .Build();
// Only CapA in the result
```

## Build

`Build()` creates an immutable `IComposition` and optionally stores it in the registry:

```csharp
var composition = scope.Compose(subject)
    .Add(new SomeCapability())
    .Build();              // uses scope default registry setting

var composition = scope.Compose(subject)
    .Add(new SomeCapability())
    .Build(useRegistry: false);  // skip registry storage
```
