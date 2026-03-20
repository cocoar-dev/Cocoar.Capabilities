# Ordering

When a composition contains multiple capabilities of the same type, their order can matter — for example, in pipelines where steps must execute in a specific sequence.

## Explicit Order

Pass an `order` parameter to `Add()`:

```csharp
scope.Compose(pipeline)
    .Add(new AuthStep(),       order: 100)
    .Add(new ValidationStep(), order: 200)
    .Add(new LoggingStep(),    order: 300)
    .Build();

var steps = composition.GetAll<IPipelineStep>();
// AuthStep → ValidationStep → LoggingStep
```

Lower values execute first.

## Order Selector

Use a lambda to extract the order from the capability itself:

```csharp
public class PipelineStep : IPipelineStep
{
    public string Name { get; init; }
    public int Priority { get; init; }
}

scope.Compose(pipeline)
    .Add(new PipelineStep { Name = "Log", Priority = 30 }, c => ((PipelineStep)c).Priority)
    .Add(new PipelineStep { Name = "Auth", Priority = 10 }, c => ((PipelineStep)c).Priority)
    .Add(new PipelineStep { Name = "Validate", Priority = 20 }, c => ((PipelineStep)c).Priority)
    .Build();

// Result: Auth(10) → Validate(20) → Log(30)
```

## Default Behavior

Capabilities without an explicit order are sorted after all ordered capabilities, in insertion order. The sort is stable — capabilities with the same order retain their insertion sequence.

## Practical Example

```csharp
// Priority tiers for a request pipeline
scope.Compose("api-pipeline")
    .Add(new CorsMiddleware(),         order: 100)   // Infrastructure
    .Add(new AuthMiddleware(),         order: 200)   // Security
    .Add(new RateLimitMiddleware(),    order: 300)   // Protection
    .Add(new ValidationMiddleware(),   order: 400)   // Input
    .Add(new BusinessLogicHandler(),   order: 500)   // Core
    .Add(new AuditMiddleware(),        order: 900)   // Cross-cutting
    .Build();
```
