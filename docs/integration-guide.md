# Integration Guide - Building Libraries with Cocoar.Capabilities

This guide shows how to architect libraries using Cocoar.Capabilities as the foundation. It provides concrete patterns, code templates, and best practices for creating extensible, capability-driven systems.

## Table of Contents

- [Integration Architecture](#integration-architecture)
- [Cocoar.Configuration Integration](#cocoarconfiguration-integration) - Complete real-world example
- [Generic Integration Patterns](#generic-integration-patterns)
- [External Library Extension](#external-library-extension)
- [Best Practices](#best-practices)

---

## Integration Architecture

### Core Principles

When building a library with Cocoar.Capabilities:

1. **Put contracts in `.Abstractions`** - External libraries reference this
2. **Use Primary Capability Pattern** - Exactly one primary + unlimited secondary
3. **Dynamic consumption** - Process capabilities without knowing concrete types
4. **Fluent wrappers** - Provide ergonomic builders for common scenarios
5. **Extension points** - Enable external libraries to add new capability types

### Standard Project Structure

```
YourLibrary.Abstractions/        # Contracts - external libraries reference this
├── IYourPrimaryCapability.cs     # Marker interface
├── ISpecificPrimaryTypes.cs      # Primary capability variants
├── CommonCapabilities.cs         # Shared secondary capabilities
└── Helpers.cs                    # Primary constraint validation

YourLibrary/                      # Core implementation
├── DefaultPrimaries.cs           # Built-in primary implementations
├── FluentExtensions.cs           # Ergonomic builder methods
├── RegistrationPipeline.cs       # Capability processing logic
└── PublicApi.cs                  # Main library entry points

YourLibrary.SomeExtension/        # Example extension package
├── CustomPrimaries.cs            # New primary capability types
└── FluentExtensions.cs           # Builder methods for new capabilities
```

---

## Cocoar.Configuration Integration

**Complete real-world integration** showing how Cocoar.Configuration could use Capability Composition for extensible configuration management.

### 1. Abstractions Package (`Cocoar.Configuration.Abstractions`)

External packages reference this for extension.

```csharp
namespace Cocoar.Configuration.Abstractions;
using Cocoar.Capabilities;

public sealed class ConfigureSpec { } // Subject for configuration specifications

// ===== PRIMARY CAPABILITY CONTRACTS =====

// Marker: exactly ONE per ConfigureSpec
public interface IPrimaryCapability<T> : ICapability<T> { }

// Common primary variants (extensible by others)
public interface IPrimaryTypeCapability<T> : IPrimaryCapability<T>
{
    Type SelectedType { get; }
}

public interface IPrimaryFactoryCapability<T> : IPrimaryCapability<T>
{
    Type ResultType { get; }
    object CreateInstance(IServiceProvider services);
}

// Future: external packages can add IPrimaryHttpCapability<T>, IPrimaryDbCapability<T>, etc.

// ===== SECONDARY CAPABILITIES =====

// Used by DI/registration pipeline
public sealed record DisableAutoRegistrationCapability<T> : ICapability<T>;
public sealed record ExposeAsCapability<T>(Type ContractType) : ICapability<T>;
public sealed record LifetimeCapability<T>(ServiceLifetime Lifetime) : ICapability<T>;
public sealed record ValidateOnStartupCapability<T>(Action<object> Validator) : ICapability<T>;
public sealed record HealthCheckCapability<T>(string Name, Func<object, bool> HealthCheck) : ICapability<T>;

// ===== CONSTRAINT VALIDATION =====

public static class Primary
{
    /// <summary>
    /// Enforces exactly one primary capability per subject.
    /// Zero primaries or multiple primaries both throw clear errors.
    /// </summary>
    public static IPrimaryCapability<T> GetRequiredSingle<T>(ICapabilityBag<T> bag)
    {
        var all = bag.GetAll<IPrimaryCapability<T>>();
        return all.Count switch
        {
            1 => all[0],
            0 => throw new InvalidOperationException(
                    $"No primary capability set for '{typeof(T).Name}'. " +
                    "Must specify exactly one primary (e.g., WithConcrete<T>(), WithFactory<T>())."),
            _ => throw new InvalidOperationException(
                    $"Multiple primary capabilities for '{typeof(T).Name}': " +
                    string.Join(", ", all.Select(x => x.GetType().Name)) +
                    ". Only one primary capability is allowed per configuration spec.")
        };
    }
}
```

### 2. Core Implementation (`Cocoar.Configuration`)

Provides default primaries and fluent API.

```csharp
namespace Cocoar.Configuration;
using Cocoar.Capabilities;
using Cocoar.Configuration.Abstractions;

// ===== DEFAULT PRIMARY IMPLEMENTATIONS =====

public sealed record ConcreteTypePrimary<T>(Type Concrete) : IPrimaryTypeCapability<T>
{
    public Type SelectedType => Concrete;
}

public sealed record FactoryPrimary<T>(
    Type Result, 
    Func<IServiceProvider, object> Factory) : IPrimaryFactoryCapability<T>
{
    public Type ResultType => Result;
    public object CreateInstance(IServiceProvider sp) => Factory(sp);
}

// ===== FLUENT BUILDER API =====

public static class ConfigureSpecComposer
{
    /// <summary>
    /// Start building a configuration specification.
    /// </summary>
    public static CapabilityBagBuilder<ConfigureSpec> Configure()
        => Composer.For(new ConfigureSpec());

    // ===== PRIMARY CAPABILITIES =====

    /// <summary>
    /// Primary: Register a concrete implementation type.
    /// </summary>
    public static CapabilityBagBuilder<ConfigureSpec> WithConcrete<TImpl>(
        this CapabilityBagBuilder<ConfigureSpec> builder)
        => builder.AddAs<IPrimaryCapability<ConfigureSpec>>(
               new ConcreteTypePrimary<ConfigureSpec>(typeof(TImpl)));

    /// <summary>
    /// Primary: Register using a factory function.
    /// </summary>
    public static CapabilityBagBuilder<ConfigureSpec> WithFactory<TResult>(
        this CapabilityBagBuilder<ConfigureSpec> builder,
        Func<IServiceProvider, TResult> factory)
        => builder.AddAs<IPrimaryCapability<ConfigureSpec>>(
               new FactoryPrimary<ConfigureSpec>(typeof(TResult), sp => factory(sp)!));

    // ===== SECONDARY CAPABILITIES =====

    /// <summary>
    /// Expose the service under an additional contract interface.
    /// </summary>
    public static CapabilityBagBuilder<ConfigureSpec> ExposeAs<TContract>(
        this CapabilityBagBuilder<ConfigureSpec> builder)
        => builder.Add(new ExposeAsCapability<ConfigureSpec>(typeof(TContract)));

    /// <summary>
    /// Set the service lifetime (Singleton, Scoped, Transient).
    /// </summary>
    public static CapabilityBagBuilder<ConfigureSpec> WithLifetime(
        this CapabilityBagBuilder<ConfigureSpec> builder, 
        ServiceLifetime lifetime)
        => builder.Add(new LifetimeCapability<ConfigureSpec>(lifetime));

    /// <summary>
    /// Skip automatic DI registration (manual registration only).
    /// </summary>
    public static CapabilityBagBuilder<ConfigureSpec> DisableAutoRegistration(
        this CapabilityBagBuilder<ConfigureSpec> builder)
        => builder.Add(new DisableAutoRegistrationCapability<ConfigureSpec>());

    /// <summary>
    /// Add startup validation that runs when the application starts.
    /// </summary>
    public static CapabilityBagBuilder<ConfigureSpec> ValidateOnStartup(
        this CapabilityBagBuilder<ConfigureSpec> builder,
        Action<object> validator)
        => builder.Add(new ValidateOnStartupCapability<ConfigureSpec>(validator));

    /// <summary>
    /// Add a health check for this service.
    /// </summary>
    public static CapabilityBagBuilder<ConfigureSpec> WithHealthCheck(
        this CapabilityBagBuilder<ConfigureSpec> builder,
        string name,
        Func<object, bool> healthCheck)
        => builder.Add(new HealthCheckCapability<ConfigureSpec>(name, healthCheck));

    // ===== CONVENIENCE SHORTCUTS =====

    public static CapabilityBagBuilder<ConfigureSpec> AsSingleton(
        this CapabilityBagBuilder<ConfigureSpec> builder)
        => builder.WithLifetime(ServiceLifetime.Singleton);

    public static CapabilityBagBuilder<ConfigureSpec> AsScoped(
        this CapabilityBagBuilder<ConfigureSpec> builder)
        => builder.WithLifetime(ServiceLifetime.Scoped);

    public static CapabilityBagBuilder<ConfigureSpec> AsTransient(
        this CapabilityBagBuilder<ConfigureSpec> builder)
        => builder.WithLifetime(ServiceLifetime.Transient);
}

// ===== REGISTRATION PIPELINE =====

public static class CocoarConfigurationRegistration
{
    /// <summary>
    /// Process a configuration specification and register services accordingly.
    /// This method demonstrates dynamic consumption - it doesn't need to know
    /// concrete primary types, only the marker interface.
    /// </summary>
    public static IServiceCollection AddFromConfigureSpec(
        this IServiceCollection services, 
        ICapabilityBag<ConfigureSpec> bag)
    {
        // Get the exactly-one primary capability
        var primary = Primary.GetRequiredSingle(bag);

        // Skip if explicitly disabled
        if (bag.Contains<DisableAutoRegistrationCapability<ConfigureSpec>>())
        {
            Console.WriteLine("Skipping auto-registration (disabled by capability)");
            ProcessNonRegistrationCapabilities(services, bag);
            return services;
        }

        // Determine service lifetime (default to Transient)
        var lifetime = bag.TryGet<LifetimeCapability<ConfigureSpec>>(out var lifetimeCapability)
                       ? lifetimeCapability.Lifetime 
                       : ServiceLifetime.Transient;

        // Process primary capability dynamically
        switch (primary)
        {
            case IPrimaryTypeCapability<ConfigureSpec> byType:
            {
                var implType = byType.SelectedType;
                Console.WriteLine($"Registering {implType.Name} with lifetime {lifetime}");

                // Register under all exposed contracts
                foreach (var expose in bag.GetAll<ExposeAsCapability<ConfigureSpec>>())
                {
                    services.Add(new ServiceDescriptor(expose.ContractType, implType, lifetime));
                    Console.WriteLine($"  -> Exposed as {expose.ContractType.Name}");
                }
                break;
            }

            case IPrimaryFactoryCapability<ConfigureSpec> byFactory:
            {
                var resultType = byFactory.ResultType;
                Console.WriteLine($"Registering {resultType.Name} via factory with lifetime {lifetime}");

                // Register factory under all exposed contracts
                foreach (var expose in bag.GetAll<ExposeAsCapability<ConfigureSpec>>())
                {
                    services.Add(new ServiceDescriptor(
                        expose.ContractType,
                        sp => byFactory.CreateInstance(sp),
                        lifetime));
                    Console.WriteLine($"  -> Exposed as {expose.ContractType.Name}");
                }
                break;
            }

            default:
                throw new NotSupportedException(
                    $"Unknown primary capability: {primary.GetType().Name}. " +
                    "Either implement a known sub-contract (IPrimaryTypeCapability<T> or " +
                    "IPrimaryFactoryCapability<T>) or register a resolver for custom primary types.");
        }

        ProcessNonRegistrationCapabilities(services, bag);
        return services;
    }

    private static void ProcessNonRegistrationCapabilities(
        IServiceCollection services, 
        ICapabilityBag<ConfigureSpec> bag)
    {
        // Process startup validation
        foreach (var validation in bag.GetAll<ValidateOnStartupCapability<ConfigureSpec>>())
        {
            // In a real implementation, you'd store these and run them during app startup
            Console.WriteLine("Registered startup validation");
        }

        // Process health checks
        foreach (var healthCheck in bag.GetAll<HealthCheckCapability<ConfigureSpec>>())
        {
            services.AddHealthChecks().AddCheck(healthCheck.Name, () =>
            {
                // In a real implementation, you'd resolve the service and call the health check
                var isHealthy = healthCheck.HealthCheck(new object()); // Simplified
                return isHealthy 
                    ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
                    : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy();
            });
            Console.WriteLine($"Registered health check: {healthCheck.Name}");
        }
    }
}
```

### 3. Usage Examples

```csharp
// ===== BASIC USAGE =====

public interface IUserService
{
    Task<User> GetUserAsync(int id);
}

public class DatabaseUserService : IUserService
{
    public async Task<User> GetUserAsync(int id)
    {
        // Database implementation
        return new User { Id = id, Name = "John" };
    }
}

// Configure with capabilities
var userServiceSpec = ConfigureSpecComposer.Configure()
    .WithConcrete<DatabaseUserService>()    // Primary: use concrete type
    .ExposeAs<IUserService>()               // Expose under interface
    .AsSingleton()                          // Singleton lifetime
    .WithHealthCheck("user-service", obj => true)  // Add health check
    .Build();

// Register in DI container
services.AddFromConfigureSpec(userServiceSpec);

// ===== FACTORY-BASED USAGE =====

var cacheServiceSpec = ConfigureSpecComposer.Configure()
    .WithFactory<ICacheService>(sp =>       // Primary: use factory
    {
        var config = sp.GetRequiredService<CacheConfig>();
        return new RedisCacheService(config.ConnectionString);
    })
    .ExposeAs<ICacheService>()
    .AsScoped()
    .ValidateOnStartup(obj =>               // Custom validation
    {
        if (obj is not ICacheService cache)
            throw new InvalidOperationException("Invalid cache service");
        // Additional validation logic
    })
    .Build();

services.AddFromConfigureSpec(cacheServiceSpec);

// ===== MANUAL REGISTRATION (NO AUTO-REGISTRATION) =====

var manualSpec = ConfigureSpecComposer.Configure()
    .WithConcrete<PaymentService>()
    .DisableAutoRegistration()             // Skip automatic DI registration
    .WithHealthCheck("payment", obj => true)
    .Build();

services.AddFromConfigureSpec(manualSpec); // Only processes health checks
// Manual registration:
services.AddScoped<PaymentService>();
```

### 4. External Library Extension

**External package** (e.g., `Cocoar.Configuration.Azure`) adds new primary capability types:

```csharp
namespace Cocoar.Configuration.Azure;
using Cocoar.Capabilities;
using Cocoar.Configuration.Abstractions;

// ===== NEW PRIMARY CAPABILITY TYPE =====

/// <summary>
/// Primary capability that creates Azure clients with automatic credential management.
/// </summary>
public sealed record AzureClientPrimary<T>(
    Func<IServiceProvider, object> Factory, 
    Type ResultType) : IPrimaryFactoryCapability<T>
{
    Type IPrimaryFactoryCapability<T>.ResultType => ResultType;
    object IPrimaryFactoryCapability<T>.CreateInstance(IServiceProvider sp) => Factory(sp);
}

// ===== FLUENT EXTENSIONS =====

public static class ConfigureSpecAzureExtensions
{
    /// <summary>
    /// Primary: Use an Azure client with automatic credential management.
    /// </summary>
    public static CapabilityBagBuilder<ConfigureSpec> UseAzureClient<TClient>(
        this CapabilityBagBuilder<ConfigureSpec> builder,
        Func<IServiceProvider, TClient> factory) where TClient : class
        => builder.AddAs<IPrimaryCapability<ConfigureSpec>>(
               new AzureClientPrimary<ConfigureSpec>(
                   sp => factory(sp), 
                   typeof(TClient)));

    /// <summary>
    /// Convenience: Use Azure Service Bus client.
    /// </summary>
    public static CapabilityBagBuilder<ConfigureSpec> UseAzureServiceBus(
        this CapabilityBagBuilder<ConfigureSpec> builder,
        string connectionString)
        => builder.UseAzureClient<ServiceBusClient>(sp =>
               new ServiceBusClient(connectionString));
}

// ===== AZURE-SPECIFIC SECONDARY CAPABILITIES =====

public sealed record AzureManagedIdentityCapability<T>(string? ClientId = null) : ICapability<T>;
public sealed record AzureKeyVaultCapability<T>(string VaultUrl) : ICapability<T>;
```

**Consumer usage:**

```csharp
// Uses extension from Azure package
var serviceBusSpec = ConfigureSpecComposer.Configure()
    .UseAzureServiceBus("connection-string")        // New primary from extension
    .ExposeAs<IMessageSender>()                     // Standard capability
    .AsSingleton()                                  // Standard capability
    .Add(new AzureManagedIdentityCapability<ConfigureSpec>()) // Azure-specific
    .Build();

services.AddFromConfigureSpec(serviceBusSpec);     // Works seamlessly!
```

---

## Generic Integration Patterns

### Pattern 1: Simple Capability Processing

For libraries that don't need primary capabilities:

```csharp
public static class SimpleLibraryExtensions
{
    public static IServiceCollection AddMyLibrary<T>(
        this IServiceCollection services,
        T subject,
        Action<CapabilityBagBuilder<T>> configure) where T : notnull
    {
        var bag = Composer.For(subject).Apply(configure).Build();
        
        // Process specific capabilities your library cares about
        if (bag.TryGet<CachingCapability<T>>(out var caching))
        {
            // Configure caching
        }
        
        if (bag.TryGet<LoggingCapability<T>>(out var logging))
        {
            // Configure logging
        }
        
        return services;
    }
}

// Helper extension
public static class CapabilityBagBuilderExtensions
{
    public static CapabilityBagBuilder<T> Apply<T>(
        this CapabilityBagBuilder<T> builder,
        Action<CapabilityBagBuilder<T>> configure)
    {
        configure(builder);
        return builder;
    }
}
```

### Pattern 2: Plugin Discovery

For libraries that want automatic capability discovery:

```csharp
public interface ICapabilityContributor<T>
{
    void ContributeCapabilities(CapabilityBagBuilder<T> builder);
    bool CanContributeTo(Type subjectType);
}

public static class CapabilityDiscovery
{
    public static CapabilityBagBuilder<T> WithDiscoveredCapabilities<T>(T subject) 
        where T : notnull
    {
        var builder = Composer.For(subject);
        
        // Find all capability contributors in loaded assemblies
        var contributors = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(ICapabilityContributor<T>).IsAssignableFrom(t))
            .Select(t => Activator.CreateInstance(t))
            .Cast<ICapabilityContributor<T>>()
            .Where(c => c.CanContributeTo(typeof(T)));
            
        foreach (var contributor in contributors)
        {
            contributor.ContributeCapabilities(builder);
        }
        
        return builder;
    }
}
```

### Pattern 3: Validation Pipeline

For libraries that need capability validation:

```csharp
public interface ICapabilityValidator<T>
{
    ValidationResult Validate(ICapabilityBag<T> bag);
}

public class ValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    
    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(params string[] errors) => 
        new() { IsValid = false, Errors = errors.ToList() };
}

public static class CapabilityValidation
{
    public static ValidationResult ValidateBag<T>(ICapabilityBag<T> bag)
    {
        var errors = new List<string>();
        
        // Example: Ensure at least one primary capability
        if (bag.GetAll<IPrimaryCapability<T>>().Count == 0)
        {
            errors.Add("At least one primary capability is required");
        }
        
        // Example: Validate capability combinations
        if (bag.Contains<SingletonCapability<T>>() && bag.Contains<TransientCapability<T>>())
        {
            errors.Add("Cannot have both Singleton and Transient capabilities");
        }
        
        return errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors.ToArray());
    }
}
```

---

## External Library Extension

### Guidelines for Extension Authors

When creating packages that extend capability-based libraries:

#### 1. Reference Only the Abstractions Package

```xml
<PackageReference Include="YourLibrary.Abstractions" Version="1.0.0" />
<PackageReference Include="Cocoar.Capabilities" Version="1.0.0" />
```

#### 2. Follow Naming Conventions

```csharp
// ✅ Good naming
namespace YourLibrary.Azure;
public sealed record AzureBlobCapability<T>(...) : ICapability<T>;

// ✅ Good extension methods
public static class ConfigBuilderAzureExtensions
{
    public static CapabilityBagBuilder<T> WithAzureBlob<T>(...);
}

// ❌ Avoid generic names
public sealed record BlobCapability<T>(...) : ICapability<T>; // Too generic
```

#### 3. Provide Clear Documentation

```csharp
/// <summary>
/// Adds Azure Blob Storage capabilities to the configuration.
/// Requires Azure.Storage.Blobs package to be installed.
/// </summary>
/// <param name="connectionString">Azure Storage connection string</param>
/// <param name="containerName">Blob container name</param>
public static CapabilityBagBuilder<ConfigureSpec> WithAzureBlob(
    this CapabilityBagBuilder<ConfigureSpec> builder,
    string connectionString,
    string containerName) => /* ... */;
```

#### 4. Handle Dependencies Gracefully

```csharp
public static class AzureCapabilities
{
    static AzureCapabilities()
    {
        // Verify required packages are available
        try
        {
            _ = typeof(BlobServiceClient);
        }
        catch (TypeLoadException)
        {
            throw new InvalidOperationException(
                "Azure capabilities require Azure.Storage.Blobs package. " +
                "Install with: dotnet add package Azure.Storage.Blobs");
        }
    }
}
```

---

## Best Practices

### 1. Capability Design

**✅ Good Capability Design:**
```csharp
// Single responsibility
public record CacheCapability<T>(TimeSpan Duration, bool VaryByUser) : ICapability<T>;

// Immutable data
public record ConfigPathCapability<T>(string Path) : ICapability<T>;

// Clear intent
public record ValidateOnStartupCapability<T>(Action<T> Validator) : ICapability<T>;
```

**❌ Poor Capability Design:**
```csharp
// Multiple responsibilities
public record MegaCapability<T>(string CachePath, TimeSpan Duration, bool EnableLogging, 
    LogLevel Level, string HealthCheckName) : ICapability<T>;

// Mutable state
public class MutableCapability<T> : ICapability<T>
{
    public string Value { get; set; } // ❌ Mutable
}
```

### 2. Primary Capability Constraints

**Always enforce exactly one primary:**
```csharp
// ✅ Use the helper
var primary = Primary.GetRequiredSingle(bag);

// ❌ Don't assume or ignore
var primaries = bag.GetAll<IPrimaryCapability<T>>();
// What if there are 0 or 2+? Code will break!
```

### 3. Extension Method Guidelines

**✅ Good Extension Methods:**
```csharp
// Fluent and chainable
public static CapabilityBagBuilder<T> WithCaching<T>(
    this CapabilityBagBuilder<T> builder,
    TimeSpan duration) => builder.Add(new CacheCapability<T>(duration));

// Clear parameter names
public static CapabilityBagBuilder<T> WithRetry<T>(
    this CapabilityBagBuilder<T> builder,
    int maxAttempts,
    TimeSpan delay) => /* ... */;
```

**❌ Poor Extension Methods:**
```csharp
// Not fluent
public static void AddCaching<T>(CapabilityBagBuilder<T> builder, TimeSpan duration)
{
    builder.Add(new CacheCapability<T>(duration));
    // No return value - breaks chaining!
}

// Unclear parameters
public static CapabilityBagBuilder<T> WithRetry<T>(
    this CapabilityBagBuilder<T> builder,
    int x, TimeSpan y) => /* ... */; // What are x and y?
```

### 4. Error Messages

**Provide actionable error messages:**
```csharp
// ✅ Helpful error
throw new InvalidOperationException(
    $"No HTTP client capability found for '{typeof(T).Name}'. " +
    "Add one using .WithHttpClient() or .WithHttpFactory().");

// ❌ Unhelpful error  
throw new InvalidOperationException("Missing capability");
```

### 5. Registration Patterns

**✅ Use the marker pattern correctly:**
```csharp
// Register primary under marker for discovery
builder.AddAs<IPrimaryCapability<T>>(new ConcretePrimary<T>());

// Register concrete capabilities normally
builder.Add(new SecondaryCpability<T>());
```

### 6. Testing

**Test capability behavior, not implementation:**
```csharp
[Test]
public void ConfigureSpec_WithConcrete_RegistersCorrectService()
{
    // Arrange
    var spec = ConfigureSpecComposer.Configure()
        .WithConcrete<MyService>()
        .ExposeAs<IMyService>()
        .Build();
    
    var services = new ServiceCollection();
    
    // Act
    services.AddFromConfigureSpec(spec);
    
    // Assert
    var provider = services.BuildServiceProvider();
    var service = provider.GetService<IMyService>();
    Assert.IsNotNull(service);
    Assert.IsInstanceOf<MyService>(service);
}
```

---

This guide provides the complete foundation for building extensible, capability-driven libraries using Cocoar.Capabilities. The Cocoar.Configuration example shows how to implement all the patterns in practice, while the generic patterns provide templates for other library types.