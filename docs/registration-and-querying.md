# Registration and Querying Behavior Guide

This document explains the complete behavior of capability registration and querying in Cocoar.Capabilities, including the recent changes that introduced contract-only registration semantics and solved interface contamination issues.

> **📚 Related Guides:**
> - For advanced tuple registration patterns, see [Tuple Contract Syntax](guides/tuple-contracts.md)
> - For primary capability concepts, see [Primary Capabilities](guides/primary-capabilities.md)
> - For memory management details, see [Memory Management](guides/memory-management.md)

## Overview

The system now uses an **ID-based architecture** with **contract-only registration semantics**. Each `CapabilityScope` owns its own registry; there is no injectable or shared global registry surface. This means:

- Each capability gets a unique ID when registered
- Capabilities are only queryable by the exact types they were registered under
- No more "interface contamination" where capabilities become accidentally queryable by unrelated interfaces

## Registration Methods

### `Add<TCapability>(TCapability capability)`

Registers a capability under its **concrete type only**.

```csharp
using var scope = new CapabilityScope();
var logCapability = new LogCapability<UserService>(LogLevel.Info);
var composition = scope.For(userService)
    .Add(logCapability)
    .Build();
```

**Registration Result:**
- ✅ Queryable by: `LogCapability<UserService>` (concrete type)
- ❌ NOT queryable by: `ILogCapability<UserService>` (interface, even if implemented)

### `AddAs<TContract>(TContract capability)`

Registers a capability under the **specified contract type only**.

```csharp
using var scope = new CapabilityScope();
var logCapability = new LogCapability<UserService>(LogLevel.Info);
var composition = scope.For(userService)
    .AddAs<ILogCapability<UserService>>(logCapability)
    .Build();
```

**Registration Result:**
- ✅ Queryable by: `ILogCapability<UserService>` (specified contract)
- ❌ NOT queryable by: `LogCapability<UserService>` (concrete type)

### `AddAs<(Type1, Type2, ...)>(capability)` - Tuple Registration

Registers a capability under **multiple contract types simultaneously**.

```csharp
using var scope = new CapabilityScope();
var logCapability = new LogCapability<UserService>(LogLevel.Info);
builder.AddAs<(ILogCapability<UserService>, LogCapability<UserService>)>(logCapability);
```

**Registration Result:**
- ✅ Queryable by: `ILogCapability<UserService>` (first contract)
- ✅ Queryable by: `LogCapability<UserService>` (second contract)

## Querying Behavior

All querying methods (`TryGet`, `GetRequired`, `GetAll`, `Contains`) use **exact type matching** and only return capabilities that were explicitly registered under the queried type.

Cross-component retrieval is performed through the same scope instance that created the composition (or a reference you keep to the composition itself). You can keep a scope as a long-lived container if you need shared discovery.

### Example: Interface Implementation vs Registration

```csharp
// Setup
public class EmailValidator : IValidationCapability<UserService>
{
    public bool IsValid(string email) => email.Contains("@");
}

var validator = new EmailValidator();
```

#### Scenario 1: Concrete Registration
```csharp
builder.Add(validator);  // Registered as EmailValidator only

// Querying
bag.TryGet<EmailValidator>(out var concrete);              // ✅ Found
bag.TryGet<IValidationCapability<UserService>>(out var i); // ❌ NOT Found
```

#### Scenario 2: Interface Registration
```csharp
builder.AddAs<IValidationCapability<UserService>>(validator);  // Registered as interface only

// Querying
bag.TryGet<EmailValidator>(out var concrete);              // ❌ NOT Found
bag.TryGet<IValidationCapability<UserService>>(out var i); // ✅ Found
```

#### Scenario 3: Multiple Registration (Tuple)
```csharp
builder.AddAs<(IValidationCapability<UserService>, EmailValidator)>(validator);

// Querying
bag.TryGet<EmailValidator>(out var concrete);              // ✅ Found
bag.TryGet<IValidationCapability<UserService>>(out var i); // ✅ Found
```

## Key Behavior Changes

### Before (Old System)
❌ **Interface Contamination Problem:**
```csharp
builder.Add(new EmailValidator());  // Concrete registration

// Old behavior - interface contamination
bag.TryGet<IValidationCapability<UserService>>(out var validator);  // Would find it!
// This was problematic because it wasn't explicitly registered for the interface
```

### After (New System)
✅ **Contract-Only Semantics:**
```csharp
builder.Add(new EmailValidator());  // Concrete registration only

// New behavior - contract-only
bag.TryGet<IValidationCapability<UserService>>(out var validator);  // Does NOT find it
bag.TryGet<EmailValidator>(out var concrete);                       // Finds it correctly
```

## Practical Examples

### Example 1: Mixed Registration Types

```csharp
var validator1 = new EmailValidator();
var validator2 = new PhoneValidator();
var validator3 = new AddressValidator();

var bag = Composer.For(userService)
    .Add(validator1)                                           // Concrete only
    .AddAs<IValidationCapability<UserService>>(validator2)     // Interface only  
    .AddAs<(IValidationCapability<UserService>, AddressValidator)>(validator3)  // Both
    .Build();

// Querying results:
var concreteValidators = bag.GetAll<EmailValidator>();                    // [validator1]
var interfaceValidators = bag.GetAll<IValidationCapability<UserService>>();  // [validator2, validator3]
var addressValidators = bag.GetAll<AddressValidator>();                   // [validator3]
```

### Example 2: RemoveWhere with Mixed Registration

```csharp
builder.Add(new LogCapability<UserService>("Debug"))                    // Concrete
       .AddAs<ILogCapability<UserService>>(new LogCapability<UserService>("Info"))  // Interface
       .AddAs<(ILogCapability<UserService>, LogCapability<UserService>)>(new LogCapability<UserService>("Error"));  // Both

// Remove all ILogCapability implementations (works regardless of registration type)
builder.RemoveWhere(cap => cap is ILogCapability<UserService>);

// Result: All three are removed because they all implement ILogCapability<UserService>
```

## Best Practices

### 1. Be Explicit About Contracts
```csharp
// ✅ Good - explicit about what should be queryable
builder.AddAs<ILogCapability<UserService>>(logCapability);

// ⚠️ Be aware - only queryable by concrete type
builder.Add(logCapability);
```

### 2. Use Tuple Registration for Dual Access
```csharp
// ✅ Good - when you need both interface and concrete access
builder.AddAs<(ILogCapability<UserService>, LogCapability<UserService>)>(logCapability);
```

### 3. Prefer Interface Registration for Flexibility
```csharp
// ✅ Good - allows swapping implementations
builder.AddAs<IValidationCapability<UserService>>(new EmailValidator());
builder.AddAs<IValidationCapability<UserService>>(new PhoneValidator());

// Query by interface
var validators = bag.GetAll<IValidationCapability<UserService>>();  // Gets both
```

## Summary

The new system provides:

- ✅ **Predictable behavior**: Capabilities are only queryable by explicitly registered types
- ✅ **No interface contamination**: Concrete registrations don't accidentally become queryable by interfaces
- ✅ **Flexible registration**: Tuple syntax allows multiple contract registration
- ✅ **Powerful removal**: RemoveWhere works with pattern matching across all registration types
- ✅ **Performance**: ID-based internal architecture is faster and simpler

The key principle: **You get exactly what you register for, nothing more, nothing less.** Per-scope registries ensure isolation and predictable lifecycle management.