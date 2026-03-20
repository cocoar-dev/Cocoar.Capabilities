# Examples

Real-world patterns demonstrating capability composition.

## Plugin Architecture

A plugin host that discovers and manages plugins through capabilities:

```csharp
public interface IPlugin
{
    string Id { get; }
    void Initialize();
}

public record PluginMetadata(string Id, string Author, string Version) : IPrimaryCapability;

public class PluginHost
{
    private readonly CapabilityScope _scope = new();

    public void RegisterPlugin(IPlugin plugin)
    {
        _scope.Compose(plugin)
            .WithPrimary(new PluginMetadata(plugin.Id, "unknown", "1.0"))
            .AddAs<IPlugin>(plugin)
            .Build();
    }

    public void RegisterPlugin(IPlugin plugin, PluginMetadata metadata)
    {
        _scope.Compose(plugin)
            .WithPrimary(metadata)
            .AddAs<IPlugin>(plugin)
            .Build();
    }

    public PluginMetadata GetMetadata(IPlugin plugin)
    {
        return _scope.Compositions.GetRequired(plugin)
            .GetRequiredPrimaryAs<PluginMetadata>();
    }
}
```

## Role-Based Access Control (RBAC)

```csharp
public record Permission(string Resource, string Action);
public record RoleIdentity(string Name) : IPrimaryCapability;

var scope = new CapabilityScope();

// Define roles
scope.Compose("admin")
    .WithPrimary(new RoleIdentity("Administrator"))
    .Add(new Permission("users", "read"))
    .Add(new Permission("users", "write"))
    .Add(new Permission("settings", "read"))
    .Add(new Permission("settings", "write"))
    .Build();

scope.Compose("viewer")
    .WithPrimary(new RoleIdentity("Viewer"))
    .Add(new Permission("users", "read"))
    .Add(new Permission("settings", "read"))
    .Build();

// Check permissions
bool canWrite = scope.Compositions.GetRequired<string>("admin")
    .GetAll<Permission>()
    .Any(p => p.Resource == "users" && p.Action == "write"); // true
```

## Event Processing Pipeline

```csharp
public record PipelineIdentity(string Name) : IPrimaryCapability;

public class PipelineStep
{
    public string Name { get; init; }
    public int Priority { get; init; }
    public Func<Event, Task> Handler { get; init; }
}

var scope = new CapabilityScope();

scope.Compose("event-pipeline")
    .WithPrimary(new PipelineIdentity("EventProcessor"))
    .Add(new PipelineStep
    {
        Name = "Validate",
        Priority = 100,
        Handler = e => Task.FromResult(e.IsValid)
    }, order: 100)
    .Add(new PipelineStep
    {
        Name = "Enrich",
        Priority = 200,
        Handler = e => Task.CompletedTask
    }, order: 200)
    .Add(new PipelineStep
    {
        Name = "Store",
        Priority = 300,
        Handler = e => Task.CompletedTask
    }, order: 300)
    .Build();

// Execute pipeline
var composition = scope.Compositions.GetRequired<string>("event-pipeline");
foreach (var step in composition.GetAll<PipelineStep>())
{
    await step.Handler(myEvent);
}
```

## Enum Enrichment

Attach display metadata to enum values:

```csharp
public enum ErrorCode { NotFound, Unauthorized, RateLimit, Internal }

public record DisplayCapability(string Label, string Description);
public record SeverityCapability(string Level);

var scope = new CapabilityScope();

scope.Compose(ErrorCode.NotFound)
    .Add(new DisplayCapability("Not Found", "The requested resource was not found"))
    .Add(new SeverityCapability("Warning"))
    .Build();

scope.Compose(ErrorCode.Unauthorized)
    .Add(new DisplayCapability("Unauthorized", "Authentication required"))
    .Add(new SeverityCapability("Error"))
    .Build();

scope.Compose(ErrorCode.RateLimit)
    .Add(new DisplayCapability("Rate Limited", "Too many requests"))
    .Add(new SeverityCapability("Warning"))
    .Build();

// Usage
var display = scope.Compositions.GetRequired(ErrorCode.NotFound)
    .GetRequiredFirst<DisplayCapability>();
Console.WriteLine(display.Label); // "Not Found"
```

::: warning
Enum values are value types — they persist in the registry for the scope's lifetime. See [Registries](/guide/advanced/registries) for details.
:::

## Configuration Builder Pattern

How [Cocoar.Configuration](https://github.com/cocoar-dev/Cocoar.Configuration) uses capabilities in production:

```csharp
// Core assembly
public class ConfigBuilder<T>
{
    public CapabilityScope Scope { get; }

    public ConfigBuilder(CapabilityScope scope)
    {
        Scope = scope;
        scope.Compose(this).Build();
    }
}

// DI extension assembly
public static class DiExtensions
{
    public static ConfigBuilder<T> AsSingleton<T>(this ConfigBuilder<T> builder)
    {
        var existing = builder.Scope.Compositions.GetRequired(builder);
        builder.Scope.Recompose(existing)
            .Add(new ServiceLifetimeCapability(ServiceLifetime.Singleton))
            .Build();
        return builder;
    }
}

// Host assembly
foreach (var builder in builders)
{
    var composition = scope.Compositions.GetRequired(builder);
    if (composition.TryGetFirst<ServiceLifetimeCapability>(out var lifetime))
    {
        services.Add(new ServiceDescriptor(typeof(T), sp => /* ... */, lifetime.Lifetime));
    }
}
```

## Domain-Specific Typed Scope

```csharp
public class PipelineHost
{
    public string Name { get; }
    public PipelineHost(string name) => Name = name;
}

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

    public IReadOnlyList<IPipelineStep> GetOrderedSteps()
        => Owner.GetRequiredComposition().GetAll<IPipelineStep>();
}

// Usage
var scope = new PipelineScope(new PipelineHost("data-ingestion"));
scope.AddStep(new ValidateStep(), 100);
scope.AddStep(new TransformStep(), 200);
scope.AddStep(new PersistStep(), 300);

var steps = scope.GetOrderedSteps();
Console.WriteLine(scope.Owner.Get().Name); // "data-ingestion"
```
