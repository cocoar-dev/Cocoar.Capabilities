# Core Concepts & Architecture

Understanding the foundational principles and architectural patterns behind Cocoar.Capabilities.

## The Capability Composition Pattern

**Cocoar.Capabilities implements Capability Composition**: An architectural pattern where objects carry typed, immutable collections of capabilities (behaviors/policies). This enables composition-over-inheritance with type safety and cross-project extensibility.

### Core Philosophy

> **"Any object can be extended with typed behaviors without modification"**

Instead of changing classes or creating inheritance hierarchies, you **attach capabilities** that define how objects should behave in different contexts.

## Architectural Principles

1. **🔧 Composition over Inheritance** - Extend behavior by attaching capabilities, not extending classes
2. **🔒 Type Safety** - Compile-time guarantees for capability-subject relationships  
3. **🧊 Immutability** - Thread-safe by design, no locks needed
4. **🔌 Cross-Project Extensibility** - Any library can define capabilities for any subject type
5. **⚡ High Performance** - ~140ns queries, ~4.6μs builds (Core), registry overhead available when needed
6. **📝 Contract-Only Semantics** - Capabilities only queryable by explicitly registered types

## Pattern Mapping

| Concept | Role | Example |
|---------|------|---------|
| **Subject** | Host object | `DatabaseConfig`, `UserService`, `PaymentController` |
| **Capability** | Behavior/Policy | `LoggingCapability`, `CachingCapability`, `ValidationCapability` |
| **Composition** | Capability Container | Immutable container with type-safe lookup |
| **Composer** | Builder | Fluent API for capability registration |
| **Contract Registration** | Interface Binding | Register concrete under interface contracts |
| **Primary Capability** | Identity Marker | Exactly one "core identity" per subject |

## Related Design Patterns

**Cocoar.Capabilities** implements and enables several established patterns:

- **🎭 Extension Object Pattern** - Dynamically extend objects with new interfaces
- **🎪 Role Object Pattern** - Objects play different roles in different contexts  
- **🧩 Component-Based Architecture** - Compose behavior from reusable components
- **📋 Strategy/Policy Pattern** - Encapsulate algorithms as attachable capabilities
- **🎨 Decorator Pattern** - Attach (not wrap) additional responsibilities
- **🏭 Registry Pattern** - Global capability discovery and lookup

## Core Architecture

### Subjects (Any Object)

A **subject** is any object that can have capabilities attached. No special interfaces or inheritance required:

```csharp
// Value types
var userId = 12345;
var status = OrderStatus.Pending;
var point = new Point(10, 20);

// Reference types  
var userService = new UserService();
var config = new DatabaseConfig();
var controller = new PaymentController();

// Even reflection objects
var method = typeof(UserService).GetMethod("CreateUser");
var property = typeof(User).GetProperty("Email");
```

**Design Decision**: Universal subject support maximizes compatibility and reduces coupling.

### Capabilities (Behaviors/Policies)

A **capability** represents functionality, configuration, or policy attachable to subjects:

```csharp
// Generic capabilities - work with any subject type T
public record LoggingCapability<T>(LogLevel Level, string Category) : ICapability<T>;
public record CachingCapability<T>(TimeSpan Duration) : ICapability<T>;

// Specific capabilities - work with specific subject types
public record UserPermissionCapability(string[] Roles) : ICapability<UserService>;
public record ConnectionStringCapability(string ConnectionString) : ICapability<DatabaseConfig>;

// Interface-based capabilities for contract enforcement
public interface IValidationCapability<T> : ICapability<T>
{
    bool IsValid(T subject);
}

// Primary capabilities for identity
public record DatabasePrimaryCapability<T> : IPrimaryCapability<T>;
```

**Design Decision**: Generic type parameters ensure type safety and prevent incompatible capability attachment.

### Compositions (Immutable Containers)

A **composition** is an immutable container storing all capabilities for a specific subject:

```csharp
// Build composition
var composition = Composer.For(userService)
    .Add(new LoggingCapability<UserService>(LogLevel.Info, "UserManagement"))
    .Add(new CachingCapability<UserService>(TimeSpan.FromMinutes(5)))
    .AddAs<IValidationCapability<UserService>>(new UserValidationCapability())
    .Build(); // ← Immutable from this point

// Query capabilities
var loggers = composition.GetAll<LoggingCapability<UserService>>();
var cache = composition.GetAll<CachingCapability<UserService>>().FirstOrDefault();
var validators = composition.GetAll<IValidationCapability<UserService>>();
```

**Design Decision**: Immutability provides thread safety without locks and prevents accidental modification.

## Type System Design

### Contract-Only Registration Semantics

The system uses **contract-only semantics** - capabilities are only queryable by the exact types they were registered under:

```csharp
public class EmailValidator : IValidationCapability<User>
{
    public bool IsValid(User user) => IsValidEmail(user.Email);
}

var validator = new EmailValidator();

// Scenario 1: Concrete registration
composer.Add(validator);  // Registered as EmailValidator only
composition.GetAll<EmailValidator>();                // ✅ Found
composition.GetAll<IValidationCapability<User>>();   // ❌ NOT Found

// Scenario 2: Interface registration  
composer.AddAs<IValidationCapability<User>>(validator);  // Registered as interface only
composition.GetAll<EmailValidator>();                // ❌ NOT Found
composition.GetAll<IValidationCapability<User>>();   // ✅ Found

// Scenario 3: Multiple registration (tuple syntax)
composer.AddAs<(IValidationCapability<User>, EmailValidator)>(validator);  
composition.GetAll<EmailValidator>();                // ✅ Found
composition.GetAll<IValidationCapability<User>>();   // ✅ Found
```

**Benefits**:
- **🎯 Predictable**: You get exactly what you registered for
- **⚡ Performance**: No expensive type hierarchy walking  
- **🔍 Explicit**: Clear intent when using interfaces vs concrete types
- **🚫 No Interface Contamination**: Prevents accidental queryability

### Memory Management Strategy

The system implements **dual storage** based on subject type:

```csharp
// Value types: Strong references (manual cleanup)
var number = 42;
var composition = Composer.For(number).Add(capability).Build();
// Stays in memory until: Composition.Remove(number)

// Reference types: Weak references (automatic cleanup)  
var service = new UserService();
var composition = Composer.For(service).Add(capability).Build();
// Automatically cleaned up when service is garbage collected
```

**Design Decision**: Different strategies optimize for value type immutability and reference type lifecycle management.

## Advanced Concepts

### Primary Capabilities

**Primary capabilities** represent the core identity or main behavior of a subject:

```csharp
// Only one primary capability allowed per subject
composer.WithPrimary(new DatabasePrimaryCapability<UserService>());

// Query primary capabilities
if (composition.TryGetPrimary(out var primary))
{
    // Use primary behavior
}
```

**Use Cases**: Configuration strategies, core behaviors, identity markers.

### Capability Ordering

**Ordered capabilities** enable deterministic processing sequences:

```csharp
public record OrderedCapability<T>(int Priority) : ICapability<T>, IOrderedCapability
{
    public int Order => Priority;
}

// Lower Order values execute first
composer.Add(new OrderedCapability<T>(-10));  // First
composer.Add(new OrderedCapability<T>(0));    // Second  
composer.Add(new OrderedCapability<T>(100));  // Third

// GetAll() returns capabilities sorted by Order
var ordered = composition.GetAll<OrderedCapability<T>>(); // Auto-sorted
```

**Use Cases**: Middleware pipelines, event handlers, processing chains.

### Cross-Project Extensibility

**Extension methods** enable cross-project capability registration:

```csharp
// Core project
public static class CoreExtensions
{
    public static Composer<T> AddLogging<T>(this Composer<T> composer, LogLevel level)
        => composer.Add(new LoggingCapability<T>(level));
}

// DI project (no circular dependency)
public static class DIExtensions  
{
    public static Composer<T> AsSingleton<T>(this Composer<T> composer)
        => composer.Add(new SingletonLifetimeCapability<T>());
}

// Usage: Both extensions work together
var composition = Composer.For(service)
    .AddLogging(LogLevel.Info)    // Core project extension
    .AsSingleton()                // DI project extension  
    .Build();
```

**Benefits**: Clean separation, no circular dependencies, unified API.

## Performance Characteristics

### Registration Performance
- **Single registration**: O(1) 
- **Tuple registration**: O(k) where k = number of contracts
- **Primary validation**: O(1) at build time

### Query Performance  
- **Single capability**: O(1) array lookup
- **Multiple capabilities**: O(n) where n = capabilities of requested type
- **Ordering**: O(n log n) when ordered capabilities are present

### Memory Usage
- **Compositions**: One per subject (not per instance)
- **Capabilities**: Shared references, no duplication
- **Value types**: Strong references until explicit cleanup
- **Reference types**: Automatic cleanup via weak references

---

**The capability composition pattern provides a powerful foundation for building extensible, type-safe systems without the complexity of traditional inheritance hierarchies.**