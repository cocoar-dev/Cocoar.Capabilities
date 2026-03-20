# Strongly-Typed Scopes

`CapabilityScope<TOwner>` is a generic scope that sets the owner at construction time. The owner is immutable — it can't be replaced.

## Creating a Typed Scope

```csharp
var host = new PipelineHost("ingestion");
var scope = new CapabilityScope<PipelineHost>(host);

// With options
var scope = new CapabilityScope<PipelineHost>(host, new CapabilityScopeOptions
{
    UseComposerRegistry = false
});
```

## Typed Owner API

The scope exposes `ScopeOwnerApi<TOwner>` — no generic parameters needed on queries:

```csharp
// No generic parameter needed — TOwner is already known
PipelineHost owner = scope.Owner.Get();

if (scope.Owner.TryGet(out var host))
{
    Console.WriteLine(host.Name);
}
```

Compare with the non-generic scope:

```csharp
// Non-generic — must specify type each time
var host = scope.Owner.Get<PipelineHost>();           // returns null if wrong type
var host = scope.Owner.GetOrThrow<PipelineHost>();    // throws if wrong type
```

## Composition via Typed Owner

```csharp
scope.Owner.Compose()
    .Add(new PipelineMetrics())
    .Build();

Composition? comp = scope.Owner.GetComposition();
Composition required = scope.Owner.GetRequiredComposition();

if (scope.Owner.TryGetComposition(out var composition))
{
    // use composition
}
```

## Composer via Typed Owner

```csharp
Composer? c = scope.Owner.GetComposer();
Composer required = scope.Owner.GetRequiredComposer();

if (scope.Owner.TryGetComposer(out var composer))
{
    // use composer
}
```

## Inheritance

`CapabilityScope<TOwner>` inherits from `CapabilityScope`, so all non-generic methods (`Compose()`, `Recompose()`, `Anchors`, `Compositions`, `Composers`) work exactly the same.

## Domain-Specific Scopes

Create purpose-built scope classes by inheriting:

```csharp
public class PipelineScope : CapabilityScope<PipelineHost>
{
    public PipelineScope(PipelineHost host) : base(host) { }

    public void AddStep(IPipelineStep step, int order)
    {
        var existing = Owner.GetComposition();
        if (existing != null)
            Recompose(existing).Add(step, order).Build();
        else
            Owner.Compose().Add(step, order).Build();
    }

    public IReadOnlyList<IPipelineStep> GetSteps()
    {
        return Owner.GetRequiredComposition().GetAll<IPipelineStep>();
    }
}
```

Usage:

```csharp
var scope = new PipelineScope(new PipelineHost("ingestion"));
scope.AddStep(new ValidateStep(), order: 100);
scope.AddStep(new TransformStep(), order: 200);
scope.AddStep(new LoadStep(), order: 300);

var steps = scope.GetSteps(); // Ordered: Validate → Transform → Load
```
