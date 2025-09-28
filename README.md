# Cocoar.Capabilities

A **general-purpose capabilities system** for .NET that enables type-safe, composable capability attachment to any object. Perfect for cross-project extensibility without circular dependencies.

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Tests](https://img.shields.io/badge/tests-50%20passing-green)]()
[![Coverage](https://img.shields.io/badge/coverage-100%25-brightgreen)]()

## 🎯 What is it?

Cocoar.Capabilities allows you to **attach typed capabilities to any object** and retrieve them later in a type-safe manner. Think of it as a **strongly-typed, high-performance property bag** that enables cross-project extensibility patterns.

**This library implements Capability Composition**: a component/extension-object style where a subject carries a typed, immutable bag of capabilities (policies/behaviors). It's composition-over-inheritance, with exact-type lookups and cross-project contributions.

### Architecture Pattern

The design maps to well-established patterns:

- **Subject** ↔ Host object (any class)
- **Capability** ↔ Component/extension/role (behavior or policy)  
- **CapabilityBag** ↔ Extension registry on the host
- **`AddAs<TContract>()`** ↔ Register under an extension interface
- **Primary marker** ↔ Ensures exactly one "role of kind Primary" is present

**Related Patterns**: Extension Object, Role Object, Component-Based Design, Strategy/Policy, Composition over Inheritance

### Key Benefits

- **🔒 Type Safe**: Compile-time guarantees for capability-subject relationships
- **⚡ Zero Allocation**: Optimized hot paths with `Array.Empty<T>()` and type-safe casting
- **🧵 Thread Safe**: Immutable by design - no locks needed
- **🔌 Extensible**: Any project can define capabilities for any subject type
- **📦 Lightweight**: Zero dependencies, AOT-friendly

## 🚀 Quick Start

### Installation

```bash
# When published to NuGet
dotnet add package Cocoar.Capabilities

# For now, reference the project directly
<ProjectReference Include="path/to/Cocoar.Capabilities/Cocoar.Capabilities.csproj" />
```

### Basic Usage

```csharp
using Cocoar.Capabilities;

// 1. Define capabilities for your domain
public record LogLevelCapability<T>(LogLevel Level) : ICapability<T>;
public record CacheCapability<T>(TimeSpan Ttl) : ICapability<T>;

// 2. Create a capability bag
var userService = new UserService();
var bag = Composer.For(userService)
    .Add(new LogLevelCapability<UserService>(LogLevel.Debug))
    .Add(new CacheCapability<UserService>(TimeSpan.FromMinutes(5)))
    .Build();

// 3. Use capabilities later
if (bag.TryGet<LogLevelCapability<UserService>>(out var logLevel))
{
    logger.SetLevel(logLevel.Level);
}

if (bag.TryGet<CacheCapability<UserService>>(out var cache))
{
    ConfigureCache(cache.Ttl);
}
```

## 📚 Core Concepts

### 1. Capabilities

A **capability** represents a piece of functionality or configuration that can be attached to a subject:

```csharp
// Generic capability - works with any subject type T
public record MyCapability<T>(string Value) : ICapability<T>;

// Specific capability - only works with UserService
public record UserCapability(int UserId) : ICapability<UserService>;
```

### 2. Subjects

A **subject** is any object that can have capabilities attached. No special interfaces or base classes required:

```csharp
// Any class can be a subject
public class UserService { }
public class DatabaseConfig { }
public class PaymentProcessor { }
```

### 3. Capability Bags

A **capability bag** is an immutable container that stores capabilities for a specific subject:

```csharp
var bag = Composer.For(myObject)
    .Add(new SomeCapability<MyObject>("value"))
    .Add(new AnotherCapability<MyObject>(42))
    .Build(); // Immutable from this point
```

## 🔧 API Reference

### Building Capability Bags

```csharp
// Create a builder for any object
var builder = Composer.For(myObject);

// Add capabilities by concrete type
builder.Add(new MyCapability<MyObject>("value"));

// Add capabilities by interface/contract (for exact-type retrieval)
builder.AddAs<IMyCapability<MyObject>>(new ConcreteCapability<MyObject>());

// Build immutable bag (one-shot operation)
var bag = builder.Build();

// ❌ This throws - builder is unusable after Build()
var bag2 = builder.Build(); // InvalidOperationException
```

### Retrieving Capabilities

```csharp
var bag = /* ... built bag ... */;

// Try to get a capability (safe)
if (bag.TryGet<MyCapability<MyObject>>(out var capability))
{
    Console.WriteLine(capability.Value);
}

// Get required capability (throws if missing)
var required = bag.GetRequired<MyCapability<MyObject>>();

// Get all capabilities of a type
var allCapabilities = bag.GetAll<MyCapability<MyObject>>();

// Check if capability exists
bool exists = bag.Contains<MyCapability<MyObject>>();

// Count capabilities of a type
int count = bag.Count<MyCapability<MyObject>>();

// Total capability count across all types
int total = bag.TotalCapabilityCount;
```

### Convenience Extensions

```csharp
// Execute action only if capability exists
bag.Use<MyObject, MyCapability<MyObject>>(cap => 
{
    Console.WriteLine($"Found: {cap.Value}");
});

// Transform capability value (returns default if missing)
var result = bag.Transform<MyObject, MyCapability<MyObject>, string>(cap => 
    cap.Value.ToUpper());
```

## 🎯 Real-World Examples

### Configuration System

```csharp
// Define configuration-specific capabilities
public record ExposeAsCapability<T>(Type ContractType) : ICapability<T>;
public record SingletonLifetimeCapability<T> : ICapability<T>;
public record HealthCheckCapability<T>(string Name) : ICapability<T>;

// Create configuration with capabilities
var dbConfig = new DatabaseConfig { ConnectionString = "..." };
var configBag = Composer.For(dbConfig)
    .Add(new ExposeAsCapability<DatabaseConfig>(typeof(IDbConfig)))
    .Add(new SingletonLifetimeCapability<DatabaseConfig>())
    .Add(new HealthCheckCapability<DatabaseConfig>("database"))
    .Build();

// Process capabilities in your DI registration code
if (configBag.Contains<SingletonLifetimeCapability<DatabaseConfig>>())
{
    services.AddSingleton(configBag.Subject);
}

foreach (var expose in configBag.GetAll<ExposeAsCapability<DatabaseConfig>>())
{
    services.AddSingleton(expose.ContractType, _ => configBag.Subject);
}
```

### Plugin Architecture

```csharp
// Plugin can automatically contribute capabilities
public class SecurityPlugin<T> : ICapabilityPlugin<T>
{
    public void ContributeCapabilities(CapabilityBagBuilder<T> builder)
    {
        builder.Add(new AuthenticationCapability<T>());
        builder.Add(new AuthorizationCapability<T>("DefaultPolicy"));
    }
}

// Host discovers and applies plugins
var bag = PluginSystem.CreateWithPlugins(myObject)
    .Add(new CustomCapability<MyObject>()) // Your own capabilities
    .Build();
```

### Web Framework Integration

```csharp
// Declarative endpoint configuration
var controllerBag = Composer.For(new UsersController())
    .Add(new RouteCapability<UsersController>("/api/users"))
    .Add(new AuthorizeCapability<UsersController>("AdminPolicy"))
    .Add(new RateLimitCapability<UsersController>(100))
    .Build();

// Framework processes capabilities automatically
ProcessWebCapabilities(controllerBag);
```

> 📖 **See [Advanced Examples](docs/examples.md)** for complete implementations including:
> - **Primary Capability Pattern** - Extensible "exactly one" constraint system
> - **Cross-Assembly Plugin Architecture** - Automatic capability discovery
> - **Complete Configuration System** - Full DI integration with validation and health checks
> - **Web Framework Integration** - Declarative routing, auth, caching, and rate limiting
> - **Event-Driven Architecture** - Handler orchestration with ordering and error handling

## ⚡ Advanced Features

### Ordered Capabilities

Capabilities can implement `IOrderedCapability` for predictable ordering:

```csharp
public record PriorityCapability<T>(int Priority, string Name) : ICapability<T>, IOrderedCapability
{
    public int Order => Priority; // Lower values run first
}

var bag = Composer.For(myObject)
    .Add(new PriorityCapability<MyObject>(100, "Last"))
    .Add(new PriorityCapability<MyObject>(1, "First"))  
    .Add(new PriorityCapability<MyObject>(50, "Middle"))
    .Build();

var ordered = bag.GetAll<PriorityCapability<MyObject>>();
// Returns: ["First", "Middle", "Last"]
```

### Contract-Based Retrieval

Use `AddAs<T>()` when you need to retrieve capabilities by interface:

```csharp
public interface IValidationCapability<T> : ICapability<T>
{
    bool IsValid { get; }
}

public record EmailValidationCapability<T> : IValidationCapability<T>
{
    public bool IsValid => true;
}

var bag = Composer.For(myObject)
    // Register concrete type under interface contract
    .AddAs<IValidationCapability<MyObject>>(new EmailValidationCapability<MyObject>())
    .Build();

// Retrieve by interface
var validator = bag.GetRequired<IValidationCapability<MyObject>>();
```

### Cross-Project Extensibility

Different projects can add capabilities to the same subject without dependencies:

```csharp
// Core.dll - Defines the subject
namespace MyApp.Core
{
    public class UserService { }
}

// DI.dll - Adds DI capabilities  
namespace MyApp.DI
{
    public record SingletonCapability<T> : ICapability<T>;
}

// Web.dll - Adds web capabilities
namespace MyApp.Web  
{
    public record RouteCapability<T>(string Template) : ICapability<T>;
}

// Composition.dll - Composes everything
var userService = new UserService();
var bag = Composer.For(userService)
    .Add(new SingletonCapability<UserService>())  // From DI.dll
    .Add(new RouteCapability<UserService>("/api/users"))  // From Web.dll
    .Build();
```

## 🚨 Important Notes

### Exact Type Matching

The system uses **exact type matching** - it only finds capabilities registered under the exact same type:

```csharp
// ❌ This won't work
builder.Add(new ConcreteCapability<T>());
bag.TryGet<ICapability<T>>(out _); // Returns false!

// ✅ Use AddAs<T> for interface retrieval
builder.AddAs<ICapability<T>>(new ConcreteCapability<T>());
bag.TryGet<ICapability<T>>(out _); // Returns true!
```

### Builder Lifecycle

Builders are **single-use** - they become unusable after `Build()`:

```csharp
var builder = Composer.For(myObject);
var bag1 = builder.Build();  // ✅ Works
var bag2 = builder.Build();  // ❌ Throws InvalidOperationException
```

### Thread Safety

- **Capability Bags**: Thread-safe (immutable)
- **Builders**: NOT thread-safe (single-threaded use only)

```csharp
var bag = builder.Build(); // Thread-safe from this point

// ✅ Safe to use from multiple threads
Task.Run(() => bag.TryGet<MyCapability<T>>(out _));
Task.Run(() => bag.GetAll<MyCapability<T>>());
```

## 🔧 Integration Patterns

### For Library Authors

When creating a library that uses Cocoar.Capabilities:

1. **Define your domain capabilities:**
```csharp
namespace MyLibrary.Capabilities
{
    public record MyLibraryCapability<T>(string Setting) : ICapability<T>;
    public record AnotherCapability<T>(int Value) : ICapability<T>;
}
```

2. **Accept capability bags in your APIs:**
```csharp
public void Configure<T>(ICapabilityBag<T> capabilityBag)
{
    // Process capabilities to configure behavior
    if (capabilityBag.TryGet<MyLibraryCapability<T>>(out var cap))
    {
        // Apply configuration
    }
}
```

3. **Provide fluent builders (optional):**
```csharp
public static class MyLibraryExtensions
{
    public static CapabilityBagBuilder<T> WithMyFeature<T>(
        this CapabilityBagBuilder<T> builder, string setting)
    {
        return builder.Add(new MyLibraryCapability<T>(setting));
    }
}

// Usage:
var bag = Composer.For(myObject)
    .WithMyFeature("custom-setting")  // Your extension method
    .Build();
```

### For Consumers

When using a library that accepts capability bags:

1. **Create capability bags for your objects:**
```csharp
var myObject = new MyClass();
var bag = Composer.For(myObject)
    .Add(new RequiredCapability<MyClass>("value"))
    .Add(new OptionalCapability<MyClass>(42))
    .Build();
```

2. **Pass to library APIs:**
```csharp
library.Configure(bag);
```

## 🧪 Testing

The library includes comprehensive tests covering all scenarios:

- **50 tests** covering core functionality, edge cases, and performance
- **100% coverage** on core types
- **Thread safety tests** for concurrent access
- **Performance tests** validating zero-allocation claims

Run tests:
```bash
dotnet test
```

## 📦 Project Structure

```
Cocoar.Capabilities/
├── ICapability.cs              # Core capability interfaces
├── ICapabilityBag.cs           # Capability bag contract
├── CapabilityBag.cs            # Implementation with Dictionary<Type, Array>
├── CapabilityBagBuilder.cs     # Fluent builder with one-shot lifecycle
├── IOrderedCapability.cs       # Ordering support
├── Composer.cs                 # Helper factory methods
└── Extensions/
    └── CapabilityBagExtensions.cs # Convenience extension methods
```

## 🤝 Contributing

Contributions welcome! The library is designed to be:

- **Stable**: Core APIs are locked and won't change
- **Extensible**: New features can be added without breaking existing code  
- **Well-tested**: All changes require comprehensive tests

## 📄 License

[MIT License](LICENSE) - Use it anywhere, commercial or personal.

---

## 🎯 What's Next?

This library provides the **foundation** for capability-driven architectures. Build your own domain-specific wrapper APIs on top for the best developer experience!

**Example integration**: Check out how [Cocoar.Configuration](https://github.com/cocoar-dev/cocoar.configuration) uses this library to provide type-safe configuration management with capabilities.