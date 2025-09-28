# Quick Start Guide

Get up and running with Cocoar.Capabilities in minutes.

## Installation

Choose your architecture and add the appropriate NuGet package:

### Registry Architecture (Global Discovery)
```bash
dotnet add package Cocoar.Capabilities
```
**Best for**: Convenience, global composition access, simple scenarios

### Core-Only Architecture (Maximum Performance)  
```bash
dotnet add package Cocoar.Capabilities.Core
```
**Best for**: High-performance scenarios, existing object lifecycle management

> Both packages share the same API for capability definition and querying. The difference is in composition lifecycle management.

## Core Concepts

**Capabilities** are behaviors or features you can attach to any object. The library provides a type-safe way to:
- Add capabilities to objects without inheritance
- Query what capabilities an object has
- Organize capabilities through contracts and ordering

## Your First Capability

### 1. Define a Capability

```csharp
using Cocoar.Capabilities;

// Simple capability with data
public record LoggingCapability<T>(LogLevel Level) : ICapability<T>;

// Interface-based capability contract
public interface IValidationCapability<T> : ICapability<T>
{
    bool IsValid(T subject);
}

public record EmailValidator<T> : IValidationCapability<T>
{
    public bool IsValid(T subject) => /* validation logic */;
}
```

### 2. Attach Capabilities to Objects

```csharp
var userService = new UserService();

// Build a composition with capabilities
var composition = Composer.For(userService)
    .Add(new LoggingCapability<UserService>(LogLevel.Info))
    .Add(new EmailValidator<UserService>())
    .Build();
```

### 3. Query Capabilities

```csharp
// Check if capability exists
if (composition.Has<LoggingCapability<UserService>>())
{
    Console.WriteLine("Logging is enabled");
}

// Get all capabilities of a type
var validators = composition.GetAll<IValidationCapability<UserService>>();
foreach (var validator in validators)
{
    validator.IsValid(userService);
}

// Get specific capability data
var loggingCaps = composition.GetAll<LoggingCapability<UserService>>();
var logLevel = loggingCaps.FirstOrDefault()?.Level ?? LogLevel.None;
```

### 4. Find Compositions Globally

```csharp
// Compositions are automatically registered globally
var foundComposition = Composition.FindRequired(userService);

// Works from anywhere in your application
if (Composition.TryFind(userService, out var comp))
{
    var hasLogging = comp.Has<LoggingCapability<UserService>>();
}
```

## Common Patterns

### Contract-Based Registration

Register capabilities under interface contracts for polymorphic querying:

```csharp
var composition = Composer.For(service)
    .AddAs<IValidationCapability<Service>>(new EmailValidator<Service>())
    .AddAs<IValidationCapability<Service>>(new PhoneValidator<Service>())
    .Build();

// Query by contract interface
var allValidators = composition.GetAll<IValidationCapability<Service>>();
```

### Primary Capabilities

Use primary capabilities to define the main behavior or type of a subject:

```csharp
public record DatabasePrimaryCapability<T> : IPrimaryCapability<T>;
public record CachePrimaryCapability<T> : IPrimaryCapability<T>;

var composition = Composer.For(service)
    .WithPrimary(new DatabasePrimaryCapability<Service>())
    .Add(new LoggingCapability<Service>(LogLevel.Debug))
    .Build();

// Only one primary capability allowed per subject
if (composition.TryGetPrimary(out var primary))
{
    // Handle based on primary capability type
}
```

### Ordered Capabilities

Control execution order with `IOrderedCapability`:

```csharp
public record MiddlewareCapability<T>(string Name, int Priority) 
    : ICapability<T>, IOrderedCapability
{
    public int Order => Priority;
}

var composition = Composer.For(pipeline)
    .Add(new MiddlewareCapability<Pipeline>("Auth", 100))
    .Add(new MiddlewareCapability<Pipeline>("Logging", 200))
    .Add(new MiddlewareCapability<Pipeline>("Validation", 150))
    .Build();

// GetAll() returns capabilities ordered by Order property (ascending)
var orderedMiddleware = composition.GetAll<MiddlewareCapability<Pipeline>>();
// Result: Auth (100), Validation (150), Logging (200)
```

### Conditional Registration

Avoid duplicate registrations with `TryAdd`:

```csharp
var composer = Composer.For(service)
    .TryAdd(new LoggingCapability<Service>(LogLevel.Info))  // Adds if not present
    .TryAdd(new LoggingCapability<Service>(LogLevel.Debug)) // Skipped - already has LoggingCapability
    .Build();
```

### Dynamic Capability Management

Modify capabilities using recomposition:

```csharp
// Start with basic composition
var composition = Composer.For(service)
    .Add(new LoggingCapability<Service>(LogLevel.Info))
    .Build();

// Later, add more capabilities
var updatedComposition = Composer.Recompose(composition)
    .Add(new CachingCapability<Service>(TimeSpan.FromMinutes(5)))
    .RemoveWhere(cap => cap is LoggingCapability<Service> log && log.Level == LogLevel.Info)
    .Add(new LoggingCapability<Service>(LogLevel.Debug))
    .Build();
```

## Real-World Example

Here's how to build a configuration system with capabilities:

```csharp
// Configuration capabilities
public record EnvironmentConfig<T>(string Environment) : IPrimaryCapability<T>;
public record ConnectionStringCapability<T>(string Name, string Value) : ICapability<T>;
public record FeatureFlagCapability<T>(string Flag, bool Enabled) : ICapability<T>;

// Build configuration
public class ConfigurationService
{
    public IComposition<ConfigurationService> BuildConfiguration()
    {
        return Composer.For(this)
            .WithPrimary(new EnvironmentConfig<ConfigurationService>("Development"))
            .Add(new ConnectionStringCapability<ConfigurationService>("Database", "Server=localhost;..."))
            .Add(new FeatureFlagCapability<ConfigurationService>("NewUI", true))
            .Add(new FeatureFlagCapability<ConfigurationService>("BetaFeatures", false))
            .Build();
    }
}

// Usage throughout application
public class DatabaseService
{
    public void Initialize()
    {
        var configService = new ConfigurationService();
        var config = Composition.FindRequired(configService);
        
        // Get environment
        var env = config.GetPrimaryOrDefaultAs<EnvironmentConfig<ConfigurationService>>();
        Console.WriteLine($"Running in: {env?.Environment}");
        
        // Get connection strings
        var connections = config.GetAll<ConnectionStringCapability<ConfigurationService>>();
        var dbConnection = connections.FirstOrDefault(c => c.Name == "Database")?.Value;
        
        // Check feature flags
        var features = config.GetAll<FeatureFlagCapability<ConfigurationService>>();
        var newUIEnabled = features.Any(f => f.Flag == "NewUI" && f.Enabled);
    }
}
```

## Performance Tips

1. **Batch Registration**: Register multiple capabilities in one composition for better performance
2. **Contract Queries**: Use specific contracts instead of broad interface queries when possible
3. **Value Type Subjects**: The library optimizes memory usage for value types automatically
4. **Reuse Compositions**: Compositions are immutable and can be safely shared across threads

## Next Steps

- Read [Core Concepts](core-concepts.md) for deeper understanding
- Explore [Examples](examples/) for more patterns
- Check the [API Reference](api-reference.md) for complete method documentation
- See [Guides](guides/) for advanced scenarios

## Common Gotchas

1. **Subject Equality**: Subjects are matched by reference (for reference types) or value equality (for value types)
2. **Primary Capability Limit**: Only one primary capability per subject - additional registrations throw exceptions
3. **Immutability**: Compositions are immutable - use `Composer.Recompose()` to modify existing compositions
4. **Generic Constraints**: Capability types must implement `ICapability<TSubject>` for the specific subject type

Need help? Check out the [examples](examples/) section for more detailed usage patterns!