# Cocoar.Capabilities - Simple Explanation

**If you're a C# developer who's never heard of "capabilities" before, this guide is for you.**

*Written by someone who struggled to understand this concept at first, despite being a Senior Software Architect.*

## The Problem (Why This Exists)

Have you ever wanted to:
- **Add behavior to objects you don't own** (third-party libraries, framework types)?
- **Let multiple libraries enhance the same object** without them knowing about each other?
- **Avoid circular dependencies** between projects but still share functionality?
- **Attach complex logic, not just simple properties** to any object?

If yes, keep reading. If no, you probably don't need this library (yet).

## What Is This? (In Simple Words)

Think of **Cocoar.Capabilities** as a **smart dictionary** where:

- **Keys** = Any object you want to enhance (we call this the "Subject")
- **Values** = Behaviors, data, or logic you want to attach (we call these "Capabilities") 

But unlike a regular dictionary, it's:
- ✅ **Type-safe** (compile-time checking)
- ✅ **High-performance** (sub-microsecond lookups)
- ✅ **Cross-project friendly** (no circular dependencies)

## Real Example: The Journey

### Before (Traditional Approach)
```csharp
public class UserService 
{
    // Problem: To add logging, caching, validation, etc.
    // I need to modify this class or inherit from it
    // But what if this is from a third-party library?
    // What if multiple libraries want to add different behaviors?
}
```

### After (Capabilities Approach)
```csharp
// Any object becomes a "subject" (dictionary key)
var userService = new UserService();

// Attach "capabilities" (dictionary values) to it
var enhanced = Composer.For(userService)
    .Add(new LoggingCapability("Debug mode"))
    .Add(new CachingCapability(TimeSpan.FromMinutes(5)))
    .Add(new ValidationCapability(user => user.IsValid()))
    .Build();

// Now use those capabilities
var loggers = enhanced.GetAll<LoggingCapability>();
var caches = enhanced.GetAll<CachingCapability>();
```

## Key Mental Shifts

### 1. **"Subject" ≠ Email Subject**
- A **Subject** is just **any object** you want to enhance
- Think: "The subject of our enhancement" = the target object

### 2. **"Capability" ≠ Application Feature**  
- A **Capability** is **anything you attach** to a subject
- Think: "Adding capabilities (possibilities) to an object"
- German speakers: Think "Möglichkeiten" 

### 3. **Objects as Dictionary Keys**
- **Any object** can be a "key" that unlocks attached behaviors
- **Multiple libraries** can attach their own capabilities
- **No modification** of the original object needed

## When Would You Use This?

### Scenario 1: Cross-Project Extensions
```csharp
// Project A: Core library defines a config class
public class DatabaseConfig { public string ConnectionString { get; set; } }

// Project B: Validation library adds validation (without knowing about Project C)
composer.Add(new ValidationCapability<DatabaseConfig>(cfg => !string.IsNullOrEmpty(cfg.ConnectionString)));

// Project C: DI library adds registration info (without knowing about Project B)
composer.Add(new DIRegistrationCapability<DatabaseConfig>(ServiceLifetime.Singleton));

// Consumer: Gets both capabilities without circular dependencies
var config = new DatabaseConfig();
var enhanced = Composer.For(config)
    .Add(validationFromProjectB)
    .Add(diInfoFromProjectC)
    .Build();
```

### Scenario 2: Third-Party Object Enhancement
```csharp
// You can't modify HttpClient, but you can enhance it
var httpClient = new HttpClient();
var enhanced = Composer.For(httpClient)
    .Add(new RetryCapability(maxRetries: 3))
    .Add(new CircuitBreakerCapability(threshold: 5))
    .Add(new LoggingCapability("HTTP"))
    .Build();

// Now httpClient has all these behaviors attached
```

## Simple API Overview

```csharp
// 1. Start with any object (the "Subject")
var myObject = new AnyClass();

// 2. Attach capabilities (behaviors/data)
var composition = Composer.For(myObject)
    .Add(new SomeCapability())           // Add behavior
    .Add(new AnotherCapability())        // Add more behavior
    .BuildAndRegister();                 // Build and make globally findable

// 3. Use the capabilities
var behaviors = composition.GetAll<SomeCapability>();
if (composition.Has<AnotherCapability>()) {
    // Do something with that capability
}

// 4. Find it globally later (if using Registry package)
var found = Composition.FindOrDefault(myObject);
```

## Two Packages = Two Approaches

### Core-Only (21 KB)
```bash
dotnet add package Cocoar.Capabilities.Core
```
- **Maximum performance**
- **You manage** where to store the compositions
- **Perfect for** high-performance scenarios

### Registry (37 KB total)  
```bash
dotnet add package Cocoar.Capabilities
```
- **Global discovery** - find compositions anywhere
- **Convenience methods** like `BuildAndRegister()`
- **Perfect for** easy cross-project scenarios

## Is This Like...?

### Dependency Injection?
**No.** DI gives you instances. Capabilities attach behaviors to existing instances.

### Extension Methods?
**Kind of,** but much more powerful:
- Extension methods are compile-time, capabilities are runtime
- Capabilities can carry state and complex logic
- Multiple libraries can add capabilities without conflicts

### Attributes/Metadata?
**Similar concept,** but:
- Capabilities can contain actual logic, not just data
- They're attached at runtime, not compile-time
- Much more flexible and powerful

## Next Steps

1. **Try the examples** in this README
2. **Read the full documentation** (it will make sense now!)
3. **Check out real-world usage** in Cocoar.Configuration

The journey from "What is this?" to "This is powerful!" is worth it. Trust the process!

---

*This explanation was written after struggling to understand capabilities for days. If it still doesn't click, that's normal - the concept is genuinely different from traditional OOP patterns.*