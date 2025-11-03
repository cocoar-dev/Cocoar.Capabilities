# High-performance capability composition for .NET

![Cocoar.Capabilities](social-preview-small.png)
> A high-performance, low-allocation capability composition library for .NET that implements the Capability Composition pattern for building extensible, type-safe systems.

[![NuGet](https://img.shields.io/nuget/v/Cocoar.Capabilities.svg)](https://www.nuget.org/packages/Cocoar.Capabilities/)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Downloads](https://img.shields.io/nuget/dt/Cocoar.Capabilities.svg)](https://www.nuget.org/packages/Cocoar.Capabilities/)



## 🚀 Features

- **🎯 Type-Safe Composition** - Attach multiple capabilities to any object with full type safety
- **⚡ High Performance** - Zero-allocation lookups and minimal overhead
- **🔒 Immutable Compositions** - Thread-safe by design with immutable capability collections
- **🎨 Flexible Registration** - Support for multiple contract types per capability
- **📦 Primary Capabilities** - Enforce single "primary" capability per subject
- **🔄 Recomposition** - Modify existing compositions safely
- **🎭 Custom Ordering** - Control capability resolution order with flexible ordering strategies
- **🗂️ Registry Support** - Optional registries for managing compositions across your application

## 📦 Installation

```bash
dotnet add package Cocoar.Capabilities
```

## 🎓 Quick Start

```csharp
using Cocoar.Capabilities;

// Create a scope to manage capabilities
using var scope = new CapabilityScope();

// Define your subject
var document = new Document("README.md");

// Compose capabilities
var composition = scope.Compose(document)
    .Add(new EditCapability())
    .Add(new PrintCapability())
    .Add(new ShareCapability())
    .Build();

// Retrieve and use capabilities
var capabilities = composition.GetAll<EditCapability>();
foreach (var cap in capabilities)
{
    cap.Edit(document);
}
```

### Primary Capabilities

Enforce a single "primary" capability per subject:

```csharp
public record UserPrimaryCapability(string UserId, string Name) : IPrimaryCapability;

var composition = scope.Compose(user)
    .Add(new UserPrimaryCapability("user123", "John Doe"))
    .Add(new AdminCapability("Level2"))
    .Build();

// Retrieve the primary capability
var primary = composition.GetPrimary();
Console.WriteLine($"User: {primary.Name}");
```

### Multiple Contracts

Register a single capability under multiple contract types:

```csharp
var composition = scope.Compose(myObject)
    .AddAs<(IValidator, IFormatter)>(new DataProcessor())
    .Build();

// Access via either contract
var validator = composition.GetFirstOrDefault<IValidator>();
var formatter = composition.GetFirstOrDefault<IFormatter>();
```

### Owner and Anchors

Associate scopes with well-known subjects for enhanced composability:

```csharp
// Setup scope with owner and anchors
var pipeline = new PipelineHost("Main");
var envContext = new EnvironmentContext { Name = "Production" };

using var scope = new CapabilityScope();
scope.Owner.Set(pipeline).Scope
     .Anchors.Set<EnvironmentContext>(envContext).Scope
     .Anchors.Set("tenant", tenantContext);

// Later, compose capabilities using owner/anchors
scope.Owner.ComposeFor<PipelineHost>()
           .Add(new DiagnosticsCapability())
           .Build();

scope.Anchors.Compose<EnvironmentContext>()
             .Add(new EnvironmentCapability())
             .Build();

// Retrieve owner or anchors (throwing)
var owner = scope.Owner.Get<PipelineHost>();
var env = scope.Anchors.Get<EnvironmentContext>();

// Safe retrieval for long-running scopes
if (scope.Owner.TryGet<PipelineHost>(out var pipelineOwner))
{
    // Owner is alive, use it
}

if (scope.Anchors.TryGet<EnvironmentContext>(out var envAnchor))
{
    // Anchor is alive, use it
}
```

### Strongly-Typed Scopes

Create scopes with strongly-typed owners for enhanced type safety:

```csharp
// Create a typed scope - owner set at construction
var configManager = new ConfigurationManager();
using var scope = new CapabilityScope<ConfigurationManager>(configManager);

// No generic parameter needed - type is known!
var owner = scope.Owner.Get();  // Returns ConfigurationManager directly
var composer = scope.Owner.Compose();  // Composes for the owner

// All owner methods are strongly typed
if (scope.Owner.TryGetComposition(out var composition))
{
    // Use composition
}

// Works seamlessly with options
var options = new CapabilityScopeOptions { UseComposerRegistry = true };
using var typedScope = new CapabilityScope<ConfigManager>(config, options);
```

**Learn more:** [Owner and Anchor Quick Reference](docs/owner-and-anchor-quick-reference.md)

## 📚 Documentation

- **[Examples](docs/examples.md)** - Detailed examples and use cases
- **[API Reference](docs/api-reference.md)** - Complete API documentation
- **[Owner and Anchors](docs/owner-and-anchor-quick-reference.md)** - Scope association patterns

## 🎯 Key Concepts

- **CapabilityScope** - Entry point for all capability operations
- **CapabilityScope<TOwner>** - Strongly-typed scope with immutable owner set at construction
- **Composer** - Fluent builder for creating compositions
- **Composition** - Immutable collection of capabilities attached to a subject
- **Primary Capability** - Single "main" capability per subject (via `IPrimaryCapability`)
- **Owner & Anchors** - Associate scopes with well-known subjects for enhanced composability
- **Registry** - Optional centralized management of compositions

---
## Contributing & Versioning

- SemVer (additive MINOR, breaking MAJOR)
- PRs & issues welcome
- Licensed under Apache License 2.0 (explicit patent grant & attribution via NOTICE)

### License & Trademark
This project is licensed under the [Apache License, Version 2.0](LICENSE). See [`NOTICE`](NOTICE) for attribution.

"Cocoar" and related marks are trademarks of COCOAR e.U. Use of the name in forks or derivatives should preserve attribution and avoid implying official endorsement. See [TRADEMARKS](TRADEMARKS.md) for permitted and restricted uses.

