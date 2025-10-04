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
using var scope = new CapabilityScope();
var userService = new UserService();
var composition = scope.For(userService)
    .Add(new LoggingCapability<UserService>(LogLevel.Info))
    .Add(new CachingCapability<UserService>(TimeSpan.FromMinutes(5)))
    .Build(); // Immutable snapshot

// Capabilities are discoverable and type-safe
var cache = composition.GetAll<CachingCapability<UserService>>().FirstOrDefault();
var loggers = composition.GetAll<LoggingCapability<UserService>>();
```

## 🌟 Key Benefits

- **🔒 Type Safe**: Compile-time guarantees for capability-subject relationships
- **⚡ High Performance**: ~25 ns registry lookups, ~51 ns feature queries, ~4.5 μs builds (50 caps)
- **🧵 Thread Safe**: Immutable by design - no locks needed
- **🔌 Extensible**: Cross-library capability attachment and discovery
- **📦 Lightweight**: Single package ~28 KB (no dependencies, AOT-friendly)
- **🎯 Contract-Based**: Explicit registration semantics - subjects need no interfaces
- **💾 Smart Memory**: Reference types use ConditionalWeakTable for automatic cleanup; value types stored in ConcurrentDictionary

## Install

Install the single package:

```bash
dotnet add package Cocoar.Capabilities
```

Registry behavior (global lookup) and composer tracking are now configured per `CapabilityScope` via `CapabilityScopeOptions`; no separate "Core" package is required.

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

```csharp
using var scope = new CapabilityScope();
var userService = new UserService();

// Build immutable composition (store it yourself or rely on scope registries if enabled)
var composition = scope.For(userService)
    .Add(new LoggingCapability<UserService>(LogLevel.Debug, "UserManagement"))
    .Add(new CachingCapability<UserService>(TimeSpan.FromMinutes(5)))
    .Add(new ValidationCapability<UserService>(user => user.IsValid()))
    .Build();

// Enable registration explicitly at build time
var registered = scope.For(userService)
    .Add(new LoggingCapability<UserService>(LogLevel.Info, "UserManagement"))
    .Build(useRegistry: true); // discoverable via scope.Compositions
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

**Global Discovery (per-scope)**
```csharp
using var scope = new CapabilityScope(new CapabilityScopeOptions { UseCompositionRegistry = true });
scope.For(userService).Add(new LoggingCapability<UserService>(LogLevel.Debug, "UserManagement")).Build();
var again = scope.Compositions.FindOrDefault(userService);
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

Ordering is entirely opt-in: if no capability implements `IOrderedCapability`, no ordering scan or sort occurs (zero overhead path). When ordering is present, a single stable sort is performed at build/recompose time; enumerations reuse the pre-ordered array (no per-call sorting). See Ordering benchmarks in `Cocoar.Capabilities.Benchmarks` for measured build deltas across random, sorted, reverse, and duplicate-priority scenarios.

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
using var scope2 = new CapabilityScope();
scope2.For(service).AddLogging(LogLevel.Info).AsSingleton().Build();
```

## Performance & Architecture

### Registry Participation

Each `CapabilityScope` can optionally track composers and/or compositions. Both options default to `true` but can be disabled for minimal overhead:

```csharp
var scope = new CapabilityScope(new CapabilityScopeOptions
{
    UseComposerRegistry = false,
    UseCompositionRegistry = false
});

// Force registration for a single build even if disabled globally
var composition = scope.For(subject)
    .Add(new SomeCapability<Subject>())
    .Build(useRegistry: true);

// Discover later if registered
var found = scope.Compositions.FindOrDefault(subject);
```

### Performance Summary (High-Level)

Instead of embedding raw microsecond/nanosecond figures (easy to misinterpret without hardware & runtime context), we summarize behavior qualitatively:

- **Build Time**: Core is the baseline; Registry adds a small mostly fixed overhead (registration + key canonicalization). Relative overhead shrinks as capability count grows.
- **Feature Queries (Get/Has for a specific capability type)**: Essentially the same for both; overhead is usually within typical measurement noise.
- **Full Enumeration (GetAll for large sets)**: Registry incurs extra indirection; absolute cost remains in the microsecond range but can look large as a percentage because the Core path is extremely small.
- **Memory**: Registry adds only a few dozen bytes of bookkeeping per registered composition; capability object sizes dominate actual usage.
- **Scaling Characteristics**: Build scales linearly with number of capabilities; feature queries are near-constant; enumeration scales with result size.

For reproducible, versioned benchmark data (including exact numbers, environment, .NET version, and methodology) see the separate document:

[Detailed performance analysis & methodology →](docs/performance-analysis.md)

### Choosing Configuration

| Disable Registry When | Enable Registry When |
| --------------------- | ------------------- |
| You already manage object lifetimes (DI/caches) | You want discovery without passing references |
| Maximum raw performance matters | Convenience outweighs small overhead |
| Very frequent queries or large compositions | Occasional queries / simpler compositions |
| Tight memory / allocation budgets | Simplicity of central lookup |

### Interpreting Overhead

Any system that lets you retrieve compositions later must store them somewhere. The Registry just makes this explicit and integrated; its overhead is the inherent cost of that convenience, not a library inefficiency.

### Technical Notes

- Lock-free via immutable compositions
- Subject key canonicalization for consistent string value semantics
- Per-scope customization of key mapping (no global mutable state)
- Works with reference & value type subjects (automatic cleanup for references)
- AOT-friendly: no runtime code generation, no external dependencies

If you need concrete numbers for capacity planning, consult the dedicated performance document rather than relying on simplified README summaries.

### Migration Note

Older examples referencing separate `Cocoar.Capabilities.Core` vs `Cocoar.Capabilities` packages, static `Composer.For`, `BuildAndRegister()`, or `Composition.FindOrDefault()` should be updated to use `CapabilityScope` and `CapabilityScopeOptions`. See `docs/static-api-migration-strategy.md` for detailed guidance.

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
- [Lifecycle & Disposal](docs/lifecycle-and-disposal.md) - Scope disposal effects and composition survivability

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