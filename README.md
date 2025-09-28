# Cocoar.Capabilities

A **general-purpose capabilities system** for .NET that enables type-safe, composable capability attachment to any object. Perfect for cross-project extensibility without circular dependencies.

> **New to capabilities?** Start with our [Simple Explanation](README-SIMPLE.md) for a beginner-friendly introduction.

[![Build (develop)](https://github.com/cocoar-dev/cocoar.capabilities/actions/workflows/develop-prerelease.yml/badge.svg)](https://github.com/cocoar-dev/cocoar.capabilities/actions/workflows/develop-prerelease.yml)
[![PR Validation](https://github.com/cocoar-dev/cocoar.capabilities/actions/workflows/pr-validation.yml/badge.svg)](https://github.com/cocoar-dev/cocoar.capabilities/actions/workflows/pr-validation.yml)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Cocoar.Capabilities.svg)](https://www.nuget.org/packages/Cocoar.Capabilities/)
[![Downloads](https://img.shields.io/nuget/dt/Cocoar.Capabilities.svg)](https://www.nuget.org/packages/Cocoar.Capabilities/)

## What is it?

**Cocoar.Capabilities** implements the **Capability Composition pattern** - a type-safe, high-performance approach to object extensibility that eliminates circular dependencies and enables cross-project collaboration.

Think of it as a **strongly-typed property bag** where any library can attach behavior to any object, and consumers can discover and use these capabilities in a predictable, compile-time safe manner.

```csharp
// Any object can have capabilities attached
var userService = new UserService();
var composition = Composer.For(userService)
    .Add(new LoggingCapability<UserService>(LogLevel.Info))
    .Add(new CachingCapability<UserService>(TimeSpan.FromMinutes(5)))
    .Build();

var cache = composition.GetAll<CachingCapability<UserService>>().FirstOrDefault();
// Capabilities are discoverable and type-safe
var loggers = composition.GetAll<LoggingCapability<UserService>>();
```

## 🌟 Key Benefits

- **🔒 Type Safe**: Compile-time guarantees for capability-subject relationships
- **⚡ High Performance**: ~140ns queries, ~4.6μs builds (Core), registry overhead available when needed
- **🧵 Thread Safe**: Immutable by design - no locks needed
- **🔌 Extensible**: Cross-library capability attachment and discovery
- **📦 Lightweight**: Core-only 21KB, Registry +16KB additional - zero dependencies, AOT-friendly
- **🎯 Contract-Based**: Explicit registration semantics - subjects need no interfaces
- **💾 Smart Memory**: Automatic cleanup with weak references for reference types, explicit control for value types

## Install

**Choose your architecture:**

```bash
# Registry Architecture - Global composition discovery (~37 KB total)
dotnet add package Cocoar.Capabilities

# Core-Only Architecture - Maximum performance (~21 KB)
dotnet add package Cocoar.Capabilities.Core
```

> **Package sizes:** Core ≈ **21 KB**. Registry adds ≈ **16 KB** additional. Zero dependencies, AOT-friendly.

## Quick Start

### 1. Define Capabilities
```csharp
using Cocoar.Capabilities; // Available in both packages

// Capabilities are just records/classes implementing ICapability<T>
public record LoggingCapability<T>(LogLevel Level, string Category) : ICapability<T>;
public record CachingCapability<T>(TimeSpan Duration) : ICapability<T>;
public record ValidationCapability<T>(Func<T, bool> Validator) : ICapability<T>;
```

### 2. Attach Capabilities to Objects

**Core-Only Approach** (maximum performance):
```csharp
var userService = new UserService();

// Build immutable composition (store in your DI/cache/lifecycle)
var composition = Composer.For(userService)
    .Add(new LoggingCapability<UserService>(LogLevel.Debug, "UserManagement"))
    .Add(new CachingCapability<UserService>(TimeSpan.FromMinutes(5)))
    .Add(new ValidationCapability<UserService>(user => user.IsValid()))
    .Build();
```

**Registry Approach** (global discovery):
```csharp
var userService = new UserService();

// Build and register globally (requires Cocoar.Capabilities package)
var composition = Composer.For(userService)
    .Add(new LoggingCapability<UserService>(LogLevel.Debug, "UserManagement"))
    .Add(new CachingCapability<UserService>(TimeSpan.FromMinutes(5)))
    .Add(new ValidationCapability<UserService>(user => user.IsValid()))
    .BuildAndRegister(); // Now discoverable globally
```

### 3. Query and Use Capabilities

**Basic Querying** (both packages):
```csharp
// Type-safe capability discovery
var loggers = composition.GetAll<LoggingCapability<UserService>>();
foreach (var logger in loggers)
{
    Logger.Log(logger.Level, $"[{logger.Category}] Processing user request");
}

// Optional capability usage
if (composition.Has<CachingCapability<UserService>>())
{
    var cache = composition.GetAll<CachingCapability<UserService>>().First();
    // Use caching with cache.Duration
}
```

**Global Discovery** (Registry package only):
```csharp
// Find compositions registered globally
var globalComposition = Composition.FindOrDefault(userService);
if (globalComposition != null)
{
    var capabilities = globalComposition.GetAll<LoggingCapability<UserService>>();
}
```

## Core Concepts

### Subjects
Any object can be a subject - no special interfaces required:
```csharp
// Reference types
var service = new UserService();
var config = new DatabaseConfig();

// Value types  
var userId = 12345;
var status = OrderStatus.Pending;
var point = new Point(10, 20);

// Even reflection objects
var method = typeof(UserService).GetMethod("CreateUser");
```

### Capabilities
Behaviors, policies, or metadata attachable to subjects:
```csharp
// Generic capabilities work with any subject
public record MetricsCapability<T>(string MetricName) : ICapability<T>;

// Specific capabilities for particular subjects
public record DatabaseConnectionCapability(string ConnectionString) : ICapability<DatabaseConfig>;

// Interface-based capabilities for contracts
public interface IValidationCapability<T> : ICapability<T>
{
    bool IsValid(T subject);
}
```

### Contract Registration
Explicit control over how capabilities are queryable:
```csharp
var validator = new EmailValidator(); // implements IValidationCapability<User>

// Concrete registration - only queryable as EmailValidator
composer.Add(validator);

// Contract registration - only queryable as IValidationCapability<User>  
composer.AddAs<IValidationCapability<User>>(validator);

// Multiple registration - queryable as both
composer.AddAs<(IValidationCapability<User>, EmailValidator)>(validator);
```

## Real-World Example: Cross-Project Configuration System

See how Cocoar.Capabilities enables sophisticated cross-project architectures:

```csharp
// Core project defines base capabilities
configure.ConcreteType<DatabaseConfig>()
    .AddValidation(config => ValidateConnectionString(config.ConnectionString))
    .AddHealthCheck("database", config => TestConnection(config))
    .ExposeAs(typeof(IDbConfig));

// DI project extends with new strategies - same API!
configure.ExposedType<CacheConfig>()
    .AddValidation(config => ValidateRedisConnection(config))    // Same extension method
    .AddHealthCheck("cache", config => TestRedisConnection(config)) // Same extension method  
    .AsSingleton()                                              // DI-specific
    .WithDependencyInjection(services => ConfigureServices(services)); // DI-specific
```

This demonstrates the **Primary Capability Strategy Pattern** - using capabilities to enable unified APIs across different project strategies. [See full example →](docs/examples/configuration-system.md)

## Advanced Features

### Primary Capabilities
Enforce single "core identity" per subject:
```csharp
// Only one primary capability allowed per subject
composer.WithPrimary(new DatabasePrimaryCapability<UserService>());

if (composition.TryGetPrimary(out var primary))
{
    // Use primary behavior
}
```

### Capability Ordering
Deterministic processing sequences:
```csharp
public record OrderedMiddleware<T>(int Priority) : ICapability<T>, IOrderedCapability
{
    public int Order => Priority; // Lower values execute first
}

// GetAll() automatically sorts by Order
var middleware = composition.GetAll<OrderedMiddleware<T>>(); // Pre-sorted
```

### Cross-Project Extension Methods
Enable clean separation without circular dependencies:
```csharp
// Core project
public static Composer<T> AddLogging<T>(this Composer<T> composer, LogLevel level)
    => composer.Add(new LoggingCapability<T>(level));

// DI project - no circular dependency
public static Composer<T> AsSingleton<T>(this Composer<T> composer)
    => composer.Add(new SingletonLifetimeCapability<T>());

// Usage: both work together seamlessly
Composer.For(service).AddLogging(LogLevel.Info).AsSingleton().Build();
```

## Performance & Architecture

### Core vs Registry Performance

Cocoar.Capabilities offers **two architectures** depending on your composition lifecycle needs:

#### **Core-Only Architecture** (`Cocoar.Capabilities.Core`)
Direct composition handling - you manage composition lifetimes:

```csharp
var composition = Composer.For(subject).Add(...).Build();
// You store and pass composition around as needed
```

**Performance characteristics:**
- **Build**: ~4.6 μs (50 capabilities), ~42 μs (500 capabilities)  
- **Query**: ~142 ns (feature queries), ~1 μs (all capabilities)
- **Memory**: 11-102 KB build allocations, 320B-1.2KB query allocations

#### **Registry Architecture** (`Cocoar.Capabilities`)
Automatic composition storage and global retrieval:

```csharp
var composition = Composer.For(subject).Add(...).BuildAndRegister();
// Later: var composition = Composition.FindOrDefault(subject);
```

**Performance characteristics:**
- **Build**: ~8.3 μs (50 capabilities), ~47 μs (500 capabilities) - *+79% and +13% overhead*
- **Query**: ~151 ns (feature queries), ~8.5 μs (large all capabilities) - *+6% to +757% overhead*
- **Memory**: Similar to Core with small registry overhead (27-34 bytes)

### **When to Choose Each Architecture**

**✅ Use Core-Only When:**
- Building many large compositions (13-79% faster builds)
- Performing frequent capability queries, especially on large sets (up to 757% faster)
- You already have object lifecycle management (DI containers, caches, etc.)
- Memory efficiency is critical
- Maximum performance is required

**✅ Use Registry When:**
- You need global composition discovery without carrying references
- Building simple compositions with occasional queries
- Convenience and ease-of-use outweigh performance considerations
- You don't have existing object storage mechanisms

### **Understanding Registry Overhead**

The Registry overhead exists because **any** composition storage/retrieval system would require:
1. **Build Phase**: Core composition creation + registration in storage
2. **Query Phase**: Core query + storage lookup + additional indirection

This is **not** a library limitation - it's the inherent cost of any persistent composition storage. If you need global access to compositions without carrying references, some storage mechanism is required.

**Key insight**: The performance difference represents the cost of convenience. Choose based on your architecture needs, not just raw numbers.

### **Technical Details**
- **Thread Safety**: Lock-free through immutability
- **Scaling**: Linear build time, constant query time (Core), various patterns (Registry)
- **Framework Compatibility**: .NET Standard 2.0 for maximum platform support
- **Memory**: Automatic cleanup with weak references, minimal GC pressure

[View detailed performance analysis →](docs/performance-analysis.md)

## Documentation

### Getting Started
- [Core Concepts & Architecture](docs/core-concepts.md) - Understand the capability composition pattern
- [API Reference](docs/api-reference.md) - Complete API documentation  
- [Registration & Querying](docs/registration-and-querying.md) - How the type system works

### Advanced Topics  
- [Primary Capabilities](docs/guides/primary-capabilities.md) - Single identity enforcement and strategy patterns
- [Capability Ordering](docs/guides/capability-ordering.md) - Deterministic processing sequences
- [Tuple Contract Syntax](docs/guides/tuple-contracts.md) - Multiple contract registration
- [Memory Management](docs/guides/memory-management.md) - Lifecycle and performance optimization

### Examples & Patterns
- [Configuration System](docs/examples/configuration-system.md) - Real-world cross-project architecture
- [Pattern Cookbook](docs/guides/pattern-cookbook.md) - Creative capability usage patterns
- [Performance Optimization](docs/guides/performance-optimization.md) - Best practices for high-performance usage

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## License

This project is licensed under the [Apache License 2.0](LICENSE).

---

**Built with ❤️ by the Cocoar team**