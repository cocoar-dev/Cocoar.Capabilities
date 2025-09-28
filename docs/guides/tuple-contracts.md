# Tuple Contract Syntax - Advanced Registration

> **📚 Prerequisites:** 
> Understanding of basic registration concepts from [Registration and Querying Behavior](../registration-and-querying.md)

## Overview

The tuple contract syntax allows registering a single capability under **multiple contract types simultaneously**. This provides flexibility for capabilities that need to be queryable through different interfaces or types without requiring separate registrations.

## Basic Syntax

```csharp
// Register under multiple contracts using tuple syntax
composer.AddAs<(IContract1<T>, IContract2<T>, ConcreteType<T>)>(capability);
```

**Result**: The capability becomes queryable under all specified types in the tuple.

## Supported Tuple Types

### ValueTuple Syntax (Recommended)
```csharp
// Up to 8 different contract types
composer.AddAs<(ILogging<T>, ICaching<T>)>(capability);
composer.AddAs<(IType1<T>, IType2<T>, IType3<T>)>(capability);
```

### What's Supported
- **Any number of contract types** (within ValueTuple limits)
- **Interfaces and concrete types** can be mixed
- **Generic and non-generic types**
- **Nested capability interfaces**

### What's NOT Supported
```csharp
// ❌ Regular Tuple (not ValueTuple)
composer.AddAs<Tuple<IContract1<T>, IContract2<T>>>(capability);

// ❌ Non-capability types
composer.AddAs<(string, int)>(capability); // Must implement ICapability<T>
```

## Usage Examples

### Interface + Concrete Registration
```csharp
public class DatabaseLogger<T> : ILoggingCapability<T>
{
    public void Log(string message) => /* implementation */;
}

var logger = new DatabaseLogger<UserService>();

// Register under both interface and concrete type
composer.AddAs<(ILoggingCapability<UserService>, DatabaseLogger<UserService>)>(logger);

// Now queryable under both types
var interfaceQuery = composition.GetAll<ILoggingCapability<UserService>>(); // ✅ Found
var concreteQuery = composition.GetAll<DatabaseLogger<UserService>>();      // ✅ Found
```

### Multiple Interface Contracts
```csharp
public class CachingValidator<T> : IValidationCapability<T>, ICachingCapability<T>
{
    public bool IsValid(T item) => /* validation logic */;
    public void Cache(T item) => /* caching logic */;
}

var validator = new CachingValidator<Product>();

// Register under multiple interface contracts
composer.AddAs<(IValidationCapability<Product>, ICachingCapability<Product>)>(validator);

// Queryable under both interfaces
var validators = composition.GetAll<IValidationCapability<Product>>(); // ✅ Found
var cachers = composition.GetAll<ICachingCapability<Product>>();       // ✅ Found
```

### Cross-Cutting Concern Registration
```csharp
public class AuditingCapability<T> : ILoggingCapability<T>, IMetricsCapability<T>, ISecurityCapability<T>
{
    // Implements multiple cross-cutting concerns
}

var auditor = new AuditingCapability<OrderService>();

// Register under all implemented interfaces
composer.AddAs<(
    ILoggingCapability<OrderService>, 
    IMetricsCapability<OrderService>, 
    ISecurityCapability<OrderService>
)>(auditor);
```

## Advanced Patterns

### Polymorphic Registration
```csharp
public class DatabaseRepository<T> : IRepository<T>, IReadOnlyRepository<T>, ICacheableRepository<T>
{
    // Implementation
}

var repository = new DatabaseRepository<User>();

// Register for different access patterns
composer.AddAs<(
    IRepository<User>,          // Full read/write access
    IReadOnlyRepository<User>,  // Read-only access  
    ICacheableRepository<User>  // Caching behavior
)>(repository);

// Different consumers can query for different contracts
var fullAccess = composition.GetAll<IRepository<User>>();
var readOnly = composition.GetAll<IReadOnlyRepository<User>>();
var cacheable = composition.GetAll<ICacheableRepository<User>>();
```

### Role-Based Access
```csharp
public class AdminUserService : IUserService, IAdminService, ISecurityService
{
    // Implementation with admin privileges
}

var adminService = new AdminUserService();

// Register under role-specific contracts
composer.AddAs<(IUserService, IAdminService, ISecurityService)>(adminService);

// Different parts of application query for appropriate role
var userOps = composition.GetAll<IUserService>();     // General user operations
var adminOps = composition.GetAll<IAdminService>();   // Admin-only operations  
var securityOps = composition.GetAll<ISecurityService>(); // Security operations
```

## Performance Considerations

### Registration Performance
```csharp
// Single contract - fastest
composer.AddAs<IContract<T>>(capability);

// Multiple contracts - slight overhead during registration
composer.AddAs<(IContract1<T>, IContract2<T>)>(capability);

// Many contracts - more reflection overhead
composer.AddAs<(IContract1<T>, IContract2<T>, IContract3<T>, IContract4<T>)>(capability);
```

### Query Performance
```csharp
// Each contract type maintains separate query path
// No performance difference between single and tuple registration during queries

var contract1Results = composition.GetAll<IContract1<T>>(); // Same speed
var contract2Results = composition.GetAll<IContract2<T>>(); // Same speed
```

## Design Guidelines

### 1. **Logical Grouping**
Only group contracts that logically belong together:

```csharp
// ✅ Good - related concerns
composer.AddAs<(ILoggingCapability<T>, IAuditingCapability<T>)>(capability);

// ❌ Avoid - unrelated concerns
composer.AddAs<(ILoggingCapability<T>, IPaymentCapability<T>)>(capability);
```

### 2. **Interface Segregation**
Use tuple contracts to support interface segregation:

```csharp
// Large interface split into focused contracts
public class FileProcessor : IFileReader, IFileWriter, IFileValidator
{
    // Implementation
}

// Consumers only get what they need
composer.AddAs<(IFileReader, IFileWriter, IFileValidator)>(processor);
```

## Best Practices

1. **Limit Contract Count**: Generally use 2-4 contracts per tuple for clarity
2. **Related Interfaces**: Only group logically related interface contracts
3. **Clear Naming**: Use descriptive interface names that indicate their purpose
4. **Document Intent**: Comment why multiple contracts are needed
5. **Test All Paths**: Verify capability is queryable under all registered contracts

## Migration from Separate Registrations

### Before (Multiple Registrations)
```csharp
composer.AddAs<ILoggingCapability<T>>(capability);
composer.AddAs<IAuditingCapability<T>>(capability);
composer.AddAs<ConcreteType<T>>(capability);
```

### After (Tuple Registration)
```csharp
composer.AddAs<(ILoggingCapability<T>, IAuditingCapability<T>, ConcreteType<T>)>(capability);
```

**Benefits**: Single registration, clearer intent, guaranteed consistency.

---

*Tuple contract syntax provides powerful, flexible registration patterns while maintaining query performance and type safety.*