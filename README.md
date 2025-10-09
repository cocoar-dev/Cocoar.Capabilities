# Cocoar.Capabilities

[![NuGet](https://img.shields.io/nuget/v/Cocoar.Capabilities.svg)](https://www.nuget.org/packages/Cocoar.Capabilities/)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Downloads](https://img.shields.io/nuget/dt/Cocoar.Capabilities.svg)](https://www.nuget.org/packages/Cocoar.Capabilities/)

A high-performance, low-allocation capability composition library for .NET that implements the Capability Composition pattern for building extensible, type-safe systems.

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
var composition = scope.For(document)
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

var composition = scope.For(user)
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
var composition = scope.For(myObject)
    .AddAs<(IValidator, IFormatter)>(new DataProcessor())
    .Build();

// Access via either contract
var validator = composition.GetAll<IValidator>().First();
var formatter = composition.GetAll<IFormatter>().First();
```

## 📚 Documentation

- **[Examples](docs/Examples.md)** - Detailed examples and use cases
- **[API Reference](docs/API-Reference.md)** - Complete API documentation

## 🎯 Key Concepts

- **CapabilityScope** - Entry point for all capability operations
- **Composer** - Fluent builder for creating compositions
- **Composition** - Immutable collection of capabilities attached to a subject
- **Primary Capability** - Single "main" capability per subject (via `IPrimaryCapability`)
- **Registry** - Optional centralized management of compositions

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guidelines](CONTRIBUTING.md) and [Code of Conduct](CODE_OF_CONDUCT.md).

## 📄 License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## 🔗 Links

- [NuGet Package](https://www.nuget.org/packages/Cocoar.Capabilities/)
- [GitHub Repository](https://github.com/cocoar-dev/Cocoar.Capabilities)
- [Issue Tracker](https://github.com/cocoar-dev/Cocoar.Capabilities/issues)
- [Changelog](CHANGELOG.md)

---

Built with ❤️ by the Cocoar Development Team
