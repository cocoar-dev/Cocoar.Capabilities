# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.10.0] - 2025-10-03

### 🎉 First Public Release

**Cocoar.Capabilities** is a general-purpose capabilities system for .NET that enables type-safe, composable capability attachment to any object without requiring interface implementations or inheritance.

### Added

#### Core Features
- **Capability Composition Pattern**: Type-safe capability attachment to any object without requiring interfaces
- **High Performance Architecture**: Sub-microsecond queries (135 ns), efficient build operations (8.55 μs for 50 capabilities)
- **Thread Safety**: Immutable compositions with lock-free operations
- **Type Safety**: Compile-time guarantees for capability-subject relationships
- **Zero Dependencies**: 25KB assembly, AOT-friendly, no external dependencies

#### Core API (Cocoar.Capabilities.Core)
- **Composer API**: Fluent builder pattern for capability composition
  - `Composer.ForSubject<T>(subject)` - Create composer for any object
  - `Add<TCapability>(capability)` - Add capability with automatic contract detection
  - `AddAs<TContract>(capability)` - Explicit contract registration  
  - `WithPrimary<TPrimary>(capability)` - Single identity enforcement
  - `RemoveWhere<T>(predicate)` - Conditional capability removal
  - `Build()` - Create immutable composition
- **ICapability<T>**: Generic capability interface for type-safe attachment
- **IPrimaryCapability<T>**: Marker interface for primary capabilities  
- **IOrderedCapability**: Deterministic processing with configurable ordering
- **IComposition<T>**: Immutable capability container with query methods

#### Registry API (Cocoar.Capabilities)
- **Global Registry**: Optional discovery system
  - `Composition.FindOrDefault<T>(subject)` - Global capability lookup
  - `CompositionRegistry` - Pluggable registry provider interface
  - Automatic registration and cleanup for reference/value types

#### Memory Management
- **Smart References**: Automatic weak references for reference types
- **Explicit Control**: Manual lifecycle management for value types via `ClearValueTypes()`
- **Minimal Allocations**: Optimized for production workloads

#### Performance Characteristics
- **Build Time**: ~171 ns per capability (linear scaling)
- **Query Time**: ~135 ns per lookup (constant time, scale-independent)  
- **Memory Usage**: 11-102 KB build allocations, 320B-1.2KB runtime allocations
- **Thread Model**: Lock-free through immutability

#### Documentation & Examples
- Complete API reference and core concepts guide
- Real-world examples including cross-project configuration systems
- Performance analysis and optimization guides  
- Pattern cookbook for advanced usage scenarios
- Integration examples with dependency injection containers

#### Quality Assurance
- Comprehensive test suite with 95%+ code coverage
- Performance benchmarks with BenchmarkDotNet
- CI/CD pipeline with automated testing and packaging
- Production-ready error handling and edge cases
- Thread safety validation and concurrent access testing
- Value type and reference type lifecycle testing

### Technical Details

#### Packages
- **Cocoar.Capabilities.Core**: Core capability system (25KB, zero dependencies)
  - `Composer<T>` - Fluent builder API  
  - `IComposition<T>` - Immutable capability containers
  - `ICapability<T>` - Type-safe capability contracts
  - Core algorithms and performance optimizations
- **Cocoar.Capabilities**: Registry system for global capability discovery  
  - `CompositionRegistry` - Global capability lookup
  - `Composition.FindOrDefault()` - Discovery extensions
  - Depends on Cocoar.Capabilities.Core

#### Target Frameworks
- .NET Standard 2.0 (broad compatibility)
- .NET 8.0+ optimizations
- AOT compilation ready

#### Breaking Changes
- N/A (Initial release)

### Migration Guide
- N/A (Initial release)

---

## Future Releases

Future versions will follow semantic versioning and document all changes in this changelog.