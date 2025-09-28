# Cocoar.Capabilities - Advanced Examples

This document contains detailed examples showing advanced patterns and real-world usage scenarios for Cocoar.Capabilities.

## Architecture Overview

**Cocoar.Capabilities implements Capability Composition**: a component/extension-object architectural style where subjects carry typed, immutable bags of capabilities (policies/behaviors). This is composition-over-inheritance with exact-type lookups and cross-project contributions.

### Pattern Mapping

| Concept | Role | Example |
|---------|------|---------|
| **Subject** | Host object | `DatabaseConfig`, `UserService`, `PaymentController` |
| **Capability** | Component/extension/role | `SingletonCapability`, `RouteCapability`, `CacheCapability` |
| **CapabilityBag** | Extension registry | Immutable container with type-safe lookup |
| **`AddAs<TContract>()`** | Interface registration | Register concrete capability under interface contract |
| **Primary marker** | Constraint enforcement | Ensure exactly one "primary behavior" per subject |

**Related Patterns**: Extension Object, Role Object, Component-Based Design, Strategy/Policy, Decorator (attach vs wrap)

## Table of Contents

- [Primary Capability Pattern](#primary-capability-pattern) - Extensible "exactly one" capabilities
- [Plugin Architecture](#plugin-architecture) - Cross-assembly capability discovery
- [Configuration System Integration](#configuration-system-integration) - Complete DI integration example
- [Web Framework Integration](#web-framework-integration) - Route and middleware capabilities
- [Event-Driven Architecture](#event-driven-architecture) - Handler registration and ordering

---

## Primary Capability Pattern

**Problem**: You need **exactly one** "primary capability" that decides the main behavior, while allowing unlimited additional capabilities. Third-party libraries should be able to add new primary kinds without modifying your core code.

**Solution**: Use a marker interface with specialized sub-contracts and pattern matching. This implements the **Extension Object pattern** with constraint validation.

**Architecture**: This is a **Role Object pattern** where the subject can play multiple roles simultaneously, but exactly one must be the "primary role".

### 1. Contracts (Marker + Specialized Primary Kinds)

```csharp
using Cocoar.Capabilities;
using Microsoft.Extensions.DependencyInjection;

public sealed class ConfigureSpec { }

// Marker: there must be exactly ONE of these per ConfigureSpec
public interface IPrimaryCapability<T> : ICapability<T> { }

// Example specialized primary kinds (others can add more)
public interface IPrimaryTypeCapability<T> : IPrimaryCapability<T>
{
    Type SelectedType { get; }
}

public interface IPrimaryFactoryCapability<T> : IPrimaryCapability<T>
{
    Type ResultType { get; }
    object CreateInstance(IServiceProvider services);
}
```

> Any external package can introduce another primary kind by implementing `IPrimaryCapability<T>` (and optionally a sub-contract like the two above).

### 2. Example Implementations (Could Live in Other Packages)

```csharp
public sealed record ConcreteTypePrimary<T>(Type Concrete)
    : IPrimaryTypeCapability<T>
{
    public Type SelectedType => Concrete;
}

public sealed record FactoryPrimary<T>(Type Result, Func<IServiceProvider, object> Factory)
    : IPrimaryFactoryCapability<T>
{
    public Type ResultType => Result;
    public object CreateInstance(IServiceProvider sp) => Factory(sp);
}
```

### 3. Building a Bag (Register the Primary Under the Marker)

> **Important:** The library uses **exact type matching**. To retrieve by the marker (`IPrimaryCapability<T>`), you must register the concrete primary **under the marker** via `AddAs<IPrimaryCapability<T>>()`.

```csharp
var spec = new ConfigureSpec();

var bag = Composer.For(spec)
    // exactly ONE primary capability, registered under the marker:
    .AddAs<IPrimaryCapability<ConfigureSpec>>(
        new ConcreteTypePrimary<ConfigureSpec>(typeof(MyConcreteType)))
    // optional additional capabilities:
    .Add(new DisableAutoRegistrationCapability<ConfigureSpec>())
    .Add(new ExposeAsCapability<ConfigureSpec>(typeof(IMyService)))
    .Build();

// Examples of additional capabilities:
public sealed record DisableAutoRegistrationCapability<T> : ICapability<T>;
public sealed record ExposeAsCapability<T>(Type ContractType) : ICapability<T>;
```

### 4. Enforcing "Exactly One" Primary (Small Helper)

```csharp
public static class Primary
{
    public static IPrimaryCapability<T> GetRequiredSingle<T>(ICapabilityBag<T> bag)
    {
        var all = bag.GetAll<IPrimaryCapability<T>>();
        return all.Count switch
        {
            1 => all[0],
            0 => throw new InvalidOperationException(
                    $"No primary capability set for '{typeof(T).Name}'."),
            _ => throw new InvalidOperationException(
                    $"Multiple primary capabilities for '{typeof(T).Name}': " +
                    string.Join(", ", all.Select(c => c.GetType().Name)))
        };
    }
}
```

### 5. Consuming Dynamically (You Don't Know the Concrete Type)

You only depend on the marker. Then you branch with pattern matching for the known sub-contracts.
If a third party adds a new primary kind, your default branch can throw (or delegate to a resolver).

```csharp
var primary = Primary.GetRequiredSingle(bag);

// Example: DI registration path varies by primary kind
switch (primary)
{
    case IPrimaryTypeCapability<ConfigureSpec> byType:
    {
        var implType = byType.SelectedType;

        if (!bag.Contains<DisableAutoRegistrationCapability<ConfigureSpec>>())
        {
            foreach (var expose in bag.GetAll<ExposeAsCapability<ConfigureSpec>>())
                services.AddSingleton(expose.ContractType, _ => Activator.CreateInstance(implType)!);
        }
        break;
    }

    case IPrimaryFactoryCapability<ConfigureSpec> byFactory:
    {
        foreach (var expose in bag.GetAll<ExposeAsCapability<ConfigureSpec>>())
            services.AddSingleton(expose.ContractType,
                sp => byFactory.CreateInstance(sp));
        break;
    }

    default:
        throw new NotSupportedException(
            $"Unknown primary capability: {primary.GetType().Name}. " +
            "Register a resolver or implement a known sub-contract.");
}
```

### Why This Scales

- **Open for extension:** New primary kinds can be added in other libraries by implementing `IPrimaryCapability<T>`
- **Closed for modification:** Your core logic only depends on the **marker**; it doesn't need to know every concrete type
- **Reliable:** A helper enforces **exactly one** primary capability per subject
- **Type-safe & fast:** Exact-type matching + array-backed storage; no reflection in hot paths

> **Tip for contributors:** When adding a new primary kind, always register it with
> `builder.AddAs<IPrimaryCapability<T>>(new YourPrimary<T>(...))`
> so consumers can reliably retrieve it by the marker.

---

## Plugin Architecture

**Problem**: You want plugins from different assemblies to automatically contribute capabilities to your types.

**Solution**: Use capability scanning and automatic registration patterns. This implements **Component-Based Design** with cross-assembly discovery.

**Architecture**: This is the **Extension Object pattern** where plugins extend subjects without the subjects knowing about the plugins.

### Plugin Interface

```csharp
public interface ICapabilityPlugin<T>
{
    void ContributeCapabilities(CapabilityBagBuilder<T> builder);
    bool CanContributeTo(Type subjectType);
}
```

### Plugin Implementation (In External Assembly)

```csharp
// SecurityPlugin.dll
public class SecurityCapabilityPlugin<T> : ICapabilityPlugin<T>
{
    public bool CanContributeTo(Type subjectType)
    {
        return subjectType.GetCustomAttribute<SecureAttribute>() != null;
    }

    public void ContributeCapabilities(CapabilityBagBuilder<T> builder)
    {
        builder.Add(new AuthenticationCapability<T>());
        builder.Add(new AuthorizationCapability<T>("DefaultPolicy"));
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class SecureAttribute : Attribute { }

public record AuthenticationCapability<T> : ICapability<T>;
public record AuthorizationCapability<T>(string Policy) : ICapability<T>;
```

### Host Application Usage

```csharp
[Secure] // Plugin will automatically contribute security capabilities
public class PaymentService { }

public static class CapabilityPluginSystem
{
    private static readonly List<object> _plugins = new();

    static CapabilityPluginSystem()
    {
        // Discover plugins from loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var pluginTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && 
                           t.GetInterfaces().Any(i => i.IsGenericType && 
                           i.GetGenericTypeDefinition() == typeof(ICapabilityPlugin<>)));

            foreach (var pluginType in pluginTypes)
            {
                if (Activator.CreateInstance(pluginType) is { } plugin)
                {
                    _plugins.Add(plugin);
                }
            }
        }
    }

    public static CapabilityBagBuilder<T> CreateWithPlugins<T>(T subject)
        where T : notnull
    {
        var builder = Composer.For(subject);
        
        foreach (var plugin in _plugins.OfType<ICapabilityPlugin<T>>())
        {
            if (plugin.CanContributeTo(typeof(T)))
            {
                plugin.ContributeCapabilities(builder);
            }
        }

        return builder;
    }
}

// Usage
var paymentService = new PaymentService();
var bag = CapabilityPluginSystem.CreateWithPlugins(paymentService)
    .Add(new CustomCapability<PaymentService>()) // Add your own capabilities too
    .Build();

// Plugins automatically added security capabilities
if (bag.TryGet<AuthenticationCapability<PaymentService>>(out var auth))
{
    Console.WriteLine("Security plugin contributed authentication capability");
}
```

---

## Configuration System Integration

**Problem**: You need a complete configuration system where different configuration types can have different DI registration behaviors, health checks, and interface exposures.

**Solution**: Define configuration-specific capabilities and process them during DI registration. This implements **Strategy/Policy patterns** where capabilities represent different policies for how configurations should behave.

**Architecture**: Each capability is a **Strategy object** that encapsulates a specific behavior (lifetime, exposure, validation). The system processes these strategies to configure the DI container.

### Configuration Capabilities

```csharp
public record ExposeAsCapability<T>(Type ContractType) : ICapability<T>;
public record SingletonLifetimeCapability<T> : ICapability<T>;
public record ScopedLifetimeCapability<T> : ICapability<T>;
public record TransientLifetimeCapability<T> : ICapability<T>;
public record HealthCheckCapability<T>(string Name, Func<T, bool> CheckFunc) : ICapability<T>;
public record ValidateOnStartCapability<T>(Action<T> Validator) : ICapability<T>;
public record SkipRegistrationCapability<T> : ICapability<T>;
```

### Configuration Types

```csharp
public interface IDbConfig
{
    string ConnectionString { get; }
}

public class DatabaseConfig : IDbConfig
{
    public string ConnectionString { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableRetry { get; set; } = true;
}

public class CacheConfig
{
    public string RedisConnectionString { get; set; } = "";
    public int DefaultTtlMinutes { get; set; } = 60;
}

public class ApiConfig
{
    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public int RateLimitPerMinute { get; set; } = 1000;
}
```

### Configuration with Capabilities

```csharp
public static class ConfigurationSetup
{
    public static IServiceCollection AddConfigurationWithCapabilities(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Database configuration
        var dbConfig = configuration.GetSection("Database").Get<DatabaseConfig>() 
                      ?? new DatabaseConfig();
        
        var dbBag = Composer.For(dbConfig)
            .Add(new ExposeAsCapability<DatabaseConfig>(typeof(IDbConfig)))
            .Add(new SingletonLifetimeCapability<DatabaseConfig>())
            .Add(new HealthCheckCapability<DatabaseConfig>("database", 
                db => !string.IsNullOrEmpty(db.ConnectionString)))
            .Add(new ValidateOnStartCapability<DatabaseConfig>(db =>
            {
                if (string.IsNullOrEmpty(db.ConnectionString))
                    throw new InvalidOperationException("Database connection string is required");
            }))
            .Build();

        // Cache configuration  
        var cacheConfig = configuration.GetSection("Cache").Get<CacheConfig>() 
                         ?? new CacheConfig();
        
        var cacheBag = Composer.For(cacheConfig)
            .Add(new ScopedLifetimeCapability<CacheConfig>())
            .Add(new HealthCheckCapability<CacheConfig>("cache",
                cache => !string.IsNullOrEmpty(cache.RedisConnectionString)))
            .Build();

        // API configuration (skip registration, just validate)
        var apiConfig = configuration.GetSection("Api").Get<ApiConfig>() 
                       ?? new ApiConfig();
        
        var apiBag = Composer.For(apiConfig)
            .Add(new SkipRegistrationCapability<ApiConfig>())
            .Add(new ValidateOnStartCapability<ApiConfig>(api =>
            {
                if (string.IsNullOrEmpty(api.ApiKey))
                    throw new InvalidOperationException("API key is required");
            }))
            .Build();

        // Process all configurations
        ProcessConfigurationBag(services, dbBag);
        ProcessConfigurationBag(services, cacheBag);
        ProcessConfigurationBag(services, apiBag);

        return services;
    }

    private static void ProcessConfigurationBag<T>(IServiceCollection services, ICapabilityBag<T> bag)
        where T : class
    {
        // Skip registration if requested
        if (bag.Contains<SkipRegistrationCapability<T>>())
        {
            Console.WriteLine($"Skipping DI registration for {typeof(T).Name}");
        }
        else
        {
            // Register with appropriate lifetime
            if (bag.Contains<SingletonLifetimeCapability<T>>())
            {
                services.AddSingleton(bag.Subject);
                Console.WriteLine($"Registered {typeof(T).Name} as Singleton");
            }
            else if (bag.Contains<ScopedLifetimeCapability<T>>())
            {
                services.AddScoped<T>(_ => bag.Subject);
                Console.WriteLine($"Registered {typeof(T).Name} as Scoped");
            }
            else if (bag.Contains<TransientLifetimeCapability<T>>())
            {
                services.AddTransient<T>(_ => bag.Subject);
                Console.WriteLine($"Registered {typeof(T).Name} as Transient");
            }

            // Register under contract interfaces
            foreach (var expose in bag.GetAll<ExposeAsCapability<T>>())
            {
                services.AddSingleton(expose.ContractType, provider => bag.Subject);
                Console.WriteLine($"Exposed {typeof(T).Name} as {expose.ContractType.Name}");
            }
        }

        // Setup health checks
        foreach (var healthCheck in bag.GetAll<HealthCheckCapability<T>>())
        {
            services.AddHealthChecks().AddCheck(healthCheck.Name, () =>
            {
                return healthCheck.CheckFunc(bag.Subject) 
                    ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
                    : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy();
            });
            Console.WriteLine($"Added health check '{healthCheck.Name}' for {typeof(T).Name}");
        }

        // Validate on startup
        foreach (var validation in bag.GetAll<ValidateOnStartCapability<T>>())
        {
            try
            {
                validation.Validator(bag.Subject);
                Console.WriteLine($"Validation passed for {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validation failed for {typeof(T).Name}: {ex.Message}");
                throw;
            }
        }
    }
}
```

### Usage in Startup

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add configuration with capabilities
        builder.Services.AddConfigurationWithCapabilities(builder.Configuration);

        var app = builder.Build();

        // Use the configured services
        var dbConfig = app.Services.GetRequiredService<IDbConfig>();
        var cacheConfig = app.Services.GetRequiredService<CacheConfig>();
        
        Console.WriteLine($"Database: {dbConfig.ConnectionString}");
        Console.WriteLine($"Cache: {cacheConfig.RedisConnectionString}");

        app.MapHealthChecks("/health");
        app.Run();
    }
}
```

---

## Web Framework Integration

**Problem**: You want to build a web framework where controllers can declaratively specify their routing, authorization, caching, and other behaviors through capabilities.

**Solution**: Create web-specific capabilities and process them during application startup. This combines **Decorator pattern** (capabilities modify behavior) with **Command pattern** (capabilities encapsulate actions to take).

**Architecture**: Each capability represents a **cross-cutting concern** that should be applied to the endpoint. The framework processes these in a pipeline, similar to **Chain of Responsibility**.

### Web Capabilities

```csharp
public record RouteCapability<T>(string Template, string Method = "GET") : ICapability<T>;
public record AuthorizeCapability<T>(string? Policy = null, string? Roles = null) : ICapability<T>;
public record CacheCapability<T>(int DurationSeconds, bool VaryByQuery = false) : ICapability<T>;
public record RateLimitCapability<T>(int RequestsPerMinute) : ICapability<T>;
public record CorsCapability<T>(string[] AllowedOrigins) : ICapability<T>;
public record ValidateModelCapability<T> : ICapability<T>;
public record LogRequestCapability<T>(LogLevel Level = LogLevel.Information) : ICapability<T>;
```

### Controller with Capabilities

```csharp
public class UsersController
{
    public async Task<IActionResult> GetUsers()
    {
        // Implementation
        return new OkResult();
    }

    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        // Implementation  
        return new CreatedResult("", null);
    }

    public async Task<IActionResult> DeleteUser(int id)
    {
        // Implementation
        return new NoContentResult();
    }
}

public class CreateUserRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}
```

### Capability-Based Registration

```csharp
public static class WebFrameworkExtensions
{
    public static IServiceCollection AddCapabilityControllers(this IServiceCollection services)
    {
        // Register GetUsers endpoint
        var getUsersBag = Composer.For(new UsersController())
            .Add(new RouteCapability<UsersController>("/api/users", "GET"))
            .Add(new AuthorizeCapability<UsersController>("ReadUsers"))
            .Add(new CacheCapability<UsersController>(300, varyByQuery: true))
            .Add(new RateLimitCapability<UsersController>(60))
            .Add(new LogRequestCapability<UsersController>(LogLevel.Information))
            .Build();

        // Register CreateUser endpoint
        var createUserBag = Composer.For(new UsersController())
            .Add(new RouteCapability<UsersController>("/api/users", "POST"))
            .Add(new AuthorizeCapability<UsersController>("WriteUsers"))
            .Add(new ValidateModelCapability<UsersController>())
            .Add(new RateLimitCapability<UsersController>(10)) // Stricter rate limit
            .Add(new LogRequestCapability<UsersController>(LogLevel.Warning))
            .Build();

        // Register DeleteUser endpoint
        var deleteUserBag = Composer.For(new UsersController())
            .Add(new RouteCapability<UsersController>("/api/users/{id:int}", "DELETE"))
            .Add(new AuthorizeCapability<UsersController>(roles: "Admin"))
            .Add(new LogRequestCapability<UsersController>(LogLevel.Warning))
            .Build();

        // Store endpoint configurations for processing
        services.AddSingleton(new EndpointCapabilityBags(getUsersBag, createUserBag, deleteUserBag));

        return services;
    }

    public static IApplicationBuilder UseCapabilityEndpoints(this IApplicationBuilder app)
    {
        var bags = app.ApplicationServices.GetRequiredService<EndpointCapabilityBags>();

        foreach (var bag in bags.AllBags)
        {
            ConfigureEndpoint(app, bag);
        }

        return app;
    }

    private static void ConfigureEndpoint<T>(IApplicationBuilder app, ICapabilityBag<T> bag)
    {
        var route = bag.GetRequired<RouteCapability<T>>();
        
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(route.Template) && 
                context.Request.Method == route.Method)
            {
                // Process capabilities in order
                await ProcessCapabilities(context, bag, next);
            }
            else
            {
                await next();
            }
        });
    }

    private static async Task ProcessCapabilities<T>(HttpContext context, ICapabilityBag<T> bag, RequestDelegate next)
    {
        try
        {
            // Authentication & Authorization
            if (bag.TryGet<AuthorizeCapability<T>>(out var auth))
            {
                if (!string.IsNullOrEmpty(auth.Policy))
                {
                    // Check policy
                }
                if (!string.IsNullOrEmpty(auth.Roles))
                {
                    // Check roles
                }
            }

            // Rate Limiting
            if (bag.TryGet<RateLimitCapability<T>>(out var rateLimit))
            {
                // Apply rate limiting logic
                var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                if (await IsRateLimited(clientId, rateLimit.RequestsPerMinute))
                {
                    context.Response.StatusCode = 429;
                    return;
                }
            }

            // Model Validation
            if (bag.Contains<ValidateModelCapability<T>>())
            {
                // Validate request model
            }

            // Caching (check if cached response exists)
            string? cacheKey = null;
            if (bag.TryGet<CacheCapability<T>>(out var cache))
            {
                cacheKey = GenerateCacheKey(context, cache.VaryByQuery);
                var cached = await GetCachedResponse(cacheKey);
                if (cached != null)
                {
                    await context.Response.WriteAsync(cached);
                    return;
                }
            }

            // Request Logging
            if (bag.TryGet<LogRequestCapability<T>>(out var logging))
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<T>>();
                logger.Log(logging.Level, "Processing request: {Method} {Path}", 
                          context.Request.Method, context.Request.Path);
            }

            // Execute the actual endpoint
            await next();

            // Cache the response
            if (cacheKey != null && bag.TryGet<CacheCapability<T>>(out var cacheConfig))
            {
                await CacheResponse(cacheKey, context.Response, cacheConfig.DurationSeconds);
            }
        }
        catch (Exception ex)
        {
            // Error handling
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Error: {ex.Message}");
        }
    }

    // Helper methods (simplified implementations)
    private static async Task<bool> IsRateLimited(string clientId, int requestsPerMinute)
    {
        // Implement rate limiting logic
        return false;
    }

    private static string GenerateCacheKey(HttpContext context, bool varyByQuery)
    {
        return $"{context.Request.Path}{(varyByQuery ? context.Request.QueryString : "")}";
    }

    private static async Task<string?> GetCachedResponse(string key)
    {
        // Implement cache retrieval
        return null;
    }

    private static async Task CacheResponse(string key, HttpResponse response, int durationSeconds)
    {
        // Implement response caching
    }
}

public class EndpointCapabilityBags
{
    public ICapabilityBag<UsersController>[] AllBags { get; }

    public EndpointCapabilityBags(params ICapabilityBag<UsersController>[] bags)
    {
        AllBags = bags;
    }
}
```

---

## Event-Driven Architecture

**Problem**: You want an event system where handlers can specify their execution order, filtering criteria, and error handling behavior through capabilities.

**Solution**: Use capabilities to configure event handler behavior and process them during event dispatch. This implements **Observer pattern** enhanced with **Strategy patterns** for handler behavior configuration.

**Architecture**: Each capability is a **behavioral strategy** that modifies how the observer (handler) processes events. The dispatcher acts as a **mediator** that coordinates multiple observers with their associated strategies.

### Event Handler Capabilities

```csharp
public record HandlerOrderCapability<T>(int Order) : ICapability<T>, IOrderedCapability
{
    int IOrderedCapability.Order => Order;
}

public record FilterCapability<T>(Func<object, bool> Predicate) : ICapability<T>;
public record RetryCapability<T>(int MaxAttempts, TimeSpan Delay) : ICapability<T>;
public record TimeoutCapability<T>(TimeSpan Timeout) : ICapability<T>;
public record LoggingCapability<T>(LogLevel Level, string Category) : ICapability<T>;
public record ErrorHandlingCapability<T>(Action<Exception> Handler) : ICapability<T>;
public record AsyncCapability<T> : ICapability<T>; // Handler should run async
public record CriticalCapability<T> : ICapability<T>; // Must not fail
```

### Event Handlers

```csharp
public class UserCreatedEvent
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class EmailNotificationHandler
{
    public async Task Handle(UserCreatedEvent evt)
    {
        Console.WriteLine($"Sending welcome email to {evt.Email}");
        await Task.Delay(100); // Simulate email sending
    }
}

public class AuditLogHandler  
{
    public async Task Handle(UserCreatedEvent evt)
    {
        Console.WriteLine($"Logging user creation: {evt.UserId} at {evt.CreatedAt}");
        await Task.Delay(50); // Simulate logging
    }
}

public class AnalyticsHandler
{
    public async Task Handle(UserCreatedEvent evt)
    {
        Console.WriteLine($"Recording analytics for user {evt.UserId}");
        await Task.Delay(25); // Simulate analytics
    }
}
```

### Event System with Capabilities

```csharp
public class CapabilityEventDispatcher
{
    private readonly Dictionary<Type, List<ICapabilityBag<object>>> _handlers = new();
    private readonly ILogger<CapabilityEventDispatcher> _logger;

    public CapabilityEventDispatcher(ILogger<CapabilityEventDispatcher> logger)
    {
        _logger = logger;
    }

    public void RegisterHandler<TEvent>(object handler, Action<CapabilityBagBuilder<object>> configureCapabilities)
    {
        var bag = Composer.For(handler)
            .Apply(configureCapabilities)
            .Build();

        var eventType = typeof(TEvent);
        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<ICapabilityBag<object>>();
        }

        _handlers[eventType].Add(bag);

        // Sort handlers by their order capabilities
        _handlers[eventType] = _handlers[eventType]
            .OrderBy(h => h.Transform<object, HandlerOrderCapability<object>, int>(c => c.Order) ?? 0)
            .ToList();
    }

    public async Task DispatchAsync<TEvent>(TEvent evt) where TEvent : class
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var handlerBags))
        {
            _logger.LogDebug("No handlers registered for event {EventType}", typeof(TEvent).Name);
            return;
        }

        var tasks = new List<Task>();

        foreach (var bag in handlerBags)
        {
            // Check if handler should process this event
            if (bag.TryGet<FilterCapability<object>>(out var filter) && 
                !filter.Predicate(evt))
            {
                continue;
            }

            // Create handler task
            var handlerTask = ExecuteHandler(bag, evt);

            // Decide if we should run sync or async
            if (bag.Contains<AsyncCapability<object>>())
            {
                tasks.Add(handlerTask);
            }
            else
            {
                await handlerTask; // Run synchronously
            }
        }

        // Wait for all async handlers
        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    private async Task ExecuteHandler<TEvent>(ICapabilityBag<object> bag, TEvent evt)
    {
        var handler = bag.Subject;
        var handlerType = handler.GetType();
        var method = handlerType.GetMethod("Handle", new[] { typeof(TEvent) });

        if (method == null)
        {
            _logger.LogWarning("Handler {HandlerType} does not have Handle method for {EventType}", 
                             handlerType.Name, typeof(TEvent).Name);
            return;
        }

        try
        {
            // Apply timeout if specified
            var executeTask = InvokeHandler(method, handler, evt);
            
            if (bag.TryGet<TimeoutCapability<object>>(out var timeout))
            {
                using var cts = new CancellationTokenSource(timeout.Timeout);
                executeTask = executeTask.WaitAsync(cts.Token);
            }

            // Apply retry if specified
            if (bag.TryGet<RetryCapability<object>>(out var retry))
            {
                await ExecuteWithRetry(executeTask, retry);
            }
            else
            {
                await executeTask;
            }

            // Log successful execution
            if (bag.TryGet<LoggingCapability<object>>(out var logging))
            {
                var logger = LoggerFactory.Create(b => b.AddConsole())
                    .CreateLogger(logging.Category);
                logger.Log(logging.Level, "Handler {HandlerType} processed {EventType} successfully", 
                          handlerType.Name, typeof(TEvent).Name);
            }
        }
        catch (Exception ex)
        {
            // Handle errors based on capabilities
            if (bag.TryGet<ErrorHandlingCapability<object>>(out var errorHandler))
            {
                errorHandler.Handler(ex);
            }
            else if (bag.Contains<CriticalCapability<object>>())
            {
                _logger.LogCritical(ex, "Critical handler {HandlerType} failed for {EventType}", 
                                   handlerType.Name, typeof(TEvent).Name);
                throw; // Re-throw for critical handlers
            }
            else
            {
                _logger.LogError(ex, "Handler {HandlerType} failed for {EventType}", 
                               handlerType.Name, typeof(TEvent).Name);
            }
        }
    }

    private static async Task<object?> InvokeHandler(MethodInfo method, object handler, object evt)
    {
        var result = method.Invoke(handler, new[] { evt });
        
        if (result is Task task)
        {
            await task;
            return task.GetType().IsGenericType ? 
                ((dynamic)task).Result : null;
        }
        
        return result;
    }

    private static async Task ExecuteWithRetry(Task task, RetryCapability<object> retry)
    {
        for (int attempt = 1; attempt <= retry.MaxAttempts; attempt++)
        {
            try
            {
                await task;
                return;
            }
            catch when (attempt < retry.MaxAttempts)
            {
                await Task.Delay(retry.Delay);
            }
        }
    }
}

// Extension method for fluent configuration
public static class CapabilityBagBuilderExtensions
{
    public static CapabilityBagBuilder<T> Apply<T>(this CapabilityBagBuilder<T> builder, 
        Action<CapabilityBagBuilder<T>> configure)
    {
        configure(builder);
        return builder;
    }
}
```

### Usage Example

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger<CapabilityEventDispatcher>();
        
        var dispatcher = new CapabilityEventDispatcher(logger);

        // Register handlers with different capabilities
        dispatcher.RegisterHandler<UserCreatedEvent>(new EmailNotificationHandler(), builder =>
            builder
                .Add(new HandlerOrderCapability<object>(100)) // Run later
                .Add(new AsyncCapability<object>()) // Can run in parallel
                .Add(new RetryCapability<object>(3, TimeSpan.FromSeconds(1)))
                .Add(new TimeoutCapability<object>(TimeSpan.FromSeconds(5)))
                .Add(new LoggingCapability<object>(LogLevel.Information, "EmailHandler")));

        dispatcher.RegisterHandler<UserCreatedEvent>(new AuditLogHandler(), builder =>
            builder
                .Add(new HandlerOrderCapability<object>(1)) // Run first
                .Add(new CriticalCapability<object>()) // Must not fail
                .Add(new LoggingCapability<object>(LogLevel.Debug, "AuditHandler")));

        dispatcher.RegisterHandler<UserCreatedEvent>(new AnalyticsHandler(), builder =>
            builder
                .Add(new HandlerOrderCapability<object>(50)) // Run in middle
                .Add(new AsyncCapability<object>())
                .Add(new FilterCapability<object>(evt => ((UserCreatedEvent)evt).Email.Contains("@company.com")))
                .Add(new ErrorHandlingCapability<object>(ex => Console.WriteLine($"Analytics failed: {ex.Message}"))));

        // Dispatch event
        var userEvent = new UserCreatedEvent
        {
            UserId = 123,
            Email = "user@company.com",
            CreatedAt = DateTime.UtcNow
        };

        await dispatcher.DispatchAsync(userEvent);
    }
}
```

---

## Summary

These examples demonstrate how **Capability Composition** enables elegant implementations of classic design patterns:

### Pattern Implementations
- **Extension Object**: Plugin architecture, primary capability marker system
- **Role Object**: Primary + secondary capabilities, subjects playing multiple roles
- **Strategy/Policy**: Configuration capabilities, event handler behaviors
- **Component-Based Design**: Cross-assembly capability contribution
- **Observer + Strategy**: Event system with configurable handler behaviors
- **Chain of Responsibility**: Web middleware pipeline driven by capabilities
- **Decorator** (attach vs wrap): Capabilities attach behaviors rather than wrapping objects

### Core Strengths Demonstrated
- **Type Safety**: Compile-time guarantees for capability-subject relationships
- **Performance**: Zero-allocation hot paths with type-safe array casting
- **Extensibility**: Cross-project capability contributions without coupling
- **Separation of Concerns**: Each capability encapsulates one behavioral aspect
- **Composability**: Mix and match capabilities to create complex behaviors
- **Maintainability**: Clear, declarative code that's easy to understand and modify

### Architectural Benefits
The **Capability Composition** approach provides:

1. **Open/Closed Principle**: Open for extension (new capabilities), closed for modification (core system)
2. **Single Responsibility**: Each capability has one clear purpose
3. **Dependency Inversion**: Depend on capability abstractions, not concrete implementations
4. **Interface Segregation**: Fine-grained capability interfaces rather than monolithic ones
5. **Composition over Inheritance**: Build complex behaviors through capability composition

Use these patterns as starting points for your own capability-driven architectures! The system scales from simple property bags to sophisticated behavioral composition frameworks.