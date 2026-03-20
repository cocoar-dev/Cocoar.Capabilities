# Patterns & Inspiration

This page explores architectural possibilities that capability composition enables. These are **thought experiments and explorations**, not prescriptive best practices.

::: info Production vs Exploration
The production use case that drove the creation of this library is [Cocoar.Configuration](https://github.com/cocoar-dev/Cocoar.Configuration) — attaching DI metadata to configuration builders across assembly boundaries (see [Why Capabilities?](/guide/why-capabilities) and the [Configuration Builder example](/reference/examples#configuration-builder-pattern)).

Everything below explores what *else* becomes possible with the same mechanism. Some patterns might be perfect for your scenario. Others might be overkill. Many have simpler alternatives depending on your constraints.

**Don't cargo-cult these patterns.** Understand them, then use what makes sense for your context.
:::

## Functions as Capabilities

The most non-obvious insight: **functions and delegates are objects**, so they can be capabilities too. Combined with ordering, you get framework-free pipeline orchestration.

```csharp
public class RequestPipeline
{
    private readonly CapabilityScope _scope = new();
    private readonly object _definition = new();

    public void Initialize()
    {
        _scope.Compose(_definition)
            .Add<Func<RequestContext, Task>>(
                async ctx => await AuthenticateUser(ctx), order: 100)
            .Add<Func<RequestContext, Task>>(
                async ctx => await ValidateInput(ctx), order: 200)
            .Add<Func<RequestContext, Task>>(
                async ctx => await ExecuteBusinessLogic(ctx), order: 300)
            .Add<Func<RequestContext, Task>>(
                async ctx => await AuditAction(ctx), order: 400)
            .Build();
    }

    public async Task ProcessRequest(RequestContext context)
    {
        var steps = _scope.Compositions.GetRequired(_definition)
            .GetAll<Func<RequestContext, Task>>();

        foreach (var step in steps) // guaranteed order
        {
            await step(context);
        }
    }
}
```

With [Using\* Extensions](/guide/composition/using-extensions), the execution becomes even cleaner:

```csharp
public async Task ProcessRequest(RequestContext context)
{
    _scope.Compositions.GetRequired(_definition)
        .UsingEach<Func<RequestContext, Task>>(async step => await step(context));
}
```

Or when the pipeline has different step types at different stages:

```csharp
_scope.Compositions.GetRequired(_definition)
    .UsingFirst<Func<RequestContext, bool>>(validate => validate(context))   // gate
    .UsingEach<Func<RequestContext, Task>>(async step => await step(context)) // pipeline
    .UsingFirstOrDefault<Action<RequestContext>>(cleanup => cleanup(context)); // optional
```

**Why this works:** The pipeline definition object is created once and reused. It acts as the stable key for the registry. Since it's a reference type, the composition is automatically cleaned up when the pipeline object is garbage collected.

**When to consider:** You want middleware-style pipelines in a class library or background service without taking a dependency on ASP.NET Core or any other framework.

**Why not just a `List<Func<...>>`?** You could. But capabilities give you ordering guarantees, type-safe querying across multiple delegate types, recomposition (adding steps later from other assemblies), and registry-based lookup by subject.

## Self-Registering Plugins

Instead of a host that registers plugins (see [Plugin example](/reference/examples#plugin-architecture)), let plugins describe *themselves*:

```csharp
public interface IPlugin
{
    void RegisterCapabilities(CapabilityScope scope);
}

public class EmailPlugin : IPlugin
{
    public void RegisterCapabilities(CapabilityScope scope)
    {
        scope.Compose(this)
            .WithPrimary(new PluginMetadata("email", "Acme Corp", "1.2.0"))
            .AddAs<(IEmailSender, INotificationProvider)>(new EmailService())
            .Add(new FeatureCapability("Templates"))
            .Add(new FeatureCapability("Attachments"))
            .Add(new DependencyCapability("SMTP", "1.0+"))
            .Build();
    }
}
```

The host just loads plugins and queries their capabilities:

```csharp
public class PluginHost
{
    private readonly CapabilityScope _scope = new();
    private readonly List<IPlugin> _plugins = new();

    public void Load(IPlugin plugin)
    {
        plugin.RegisterCapabilities(_scope);
        _plugins.Add(plugin);
    }

    public IEnumerable<IPlugin> WithFeature(string feature)
    {
        return _plugins.Where(p =>
        {
            var comp = _scope.Compositions.GetOrDefault(p);
            return comp?.GetAll<FeatureCapability>().Any(f => f.Name == feature) ?? false;
        });
    }

    public bool ValidateDependencies(IPlugin plugin)
    {
        var deps = _scope.Compositions.GetRequired(plugin)
            .GetAll<DependencyCapability>();
        return deps.All(d => IsSatisfied(d));
    }
}
```

**The difference from host-driven registration:** The plugin decides what capabilities it has. The host doesn't need to know about `FeatureCapability` or `DependencyCapability` at compile time — it just queries by type. A second assembly could define entirely new capability types that the original host never heard of.

**When to consider:** You're building an extensibility system where plugins come from different assemblies and need to self-describe their features, dependencies, or metadata.

## Multi-Tenant Context Isolation

Different `CapabilityScope` instances are completely independent worlds. This maps naturally to multi-tenant architectures:

```csharp
public class TenantService
{
    private readonly Dictionary<string, CapabilityScope> _tenantScopes = new();

    public void OnboardTenant(string tenantId, TenantConfig config)
    {
        var scope = new CapabilityScope();
        _tenantScopes[tenantId] = scope;

        // Same enum values, completely different metadata per tenant
        scope.Compose(ErrorCode.InvalidInput)
            .Add(new DisplayCapability(config.ErrorMessages[ErrorCode.InvalidInput]))
            .Add(new SeverityCapability(config.SeverityOverrides[ErrorCode.InvalidInput]))
            .Build();

        scope.Compose(ErrorCode.Unauthorized)
            .Add(new DisplayCapability(config.ErrorMessages[ErrorCode.Unauthorized]))
            .Add(new SeverityCapability(LogLevel.Error))
            .Build();
    }

    public void HandleError(string tenantId, ErrorCode error)
    {
        var scope = _tenantScopes[tenantId];
        var composition = scope.Compositions.GetOrDefault(error);

        if (composition != null)
        {
            var display = composition.GetFirstOrDefault<DisplayCapability>();
            Console.WriteLine(display?.Message); // tenant-specific message
        }
    }
}
```

**The key insight:** You don't need any multi-tenant abstractions in your business logic. The scope *is* the isolation boundary. Tenant A's capabilities can never leak into Tenant B's scope.

**When to consider:** You need per-tenant customization of metadata, error messages, feature flags, or configuration — and you want complete isolation without tenant-ID checks scattered through your code.

## Attaching Behavior to Values

The [Enum Enrichment example](/reference/examples#enum-enrichment) shows attaching *data* to enum values. But capabilities can also be *actions* — turning values into dispatch targets:

```csharp
scope.Compose(ErrorCode.Timeout)
    .Add(new DisplayCapability("Request timed out"))
    .Add<Action<HttpContext>>(ctx =>
    {
        ctx.Response.StatusCode = 504;
        ctx.Response.Headers["Retry-After"] = "30";
    })
    .Build();

scope.Compose(ErrorCode.NotFound)
    .Add(new DisplayCapability("Not found"))
    .Add<Action<HttpContext>>(ctx =>
    {
        ctx.Response.StatusCode = 404;
    })
    .Build();

// Dispatch — no switch statement
var composition = scope.Compositions.GetOrDefault(errorCode);
composition?.UsingFirstOrDefault<Action<HttpContext>>(handler => handler(httpContext));
```

This eliminates switch statements over enums entirely. Each value carries its own behavior.

**When to consider:** You have enum values or status codes that map to distinct behavior, and you want to avoid growing switch statements. Especially useful when the behavior comes from a different assembly than the enum definition.

## Choosing the Right Pattern

| Pattern | Core Mechanism | Best For |
|---------|---------------|----------|
| Functions as Capabilities | `Add<Func<...>>()` + ordering | Framework-free pipelines |
| Self-Registering Plugins | `AddAs<(I1, I2)>()` + primary | Extensibility systems |
| Multi-Tenant Isolation | Separate `CapabilityScope` per tenant | SaaS, per-context customization |
| Behavior on Values | `Add<Action<...>>()` on enums | Eliminating switch statements |

All of these build on the same three concepts: [Scope](/guide/core/capability-scope), [Composer](/guide/core/composer), and [Composition](/guide/core/composition). The flexibility comes from what you choose to compose — data, functions, or both — and how you structure your scopes.
