# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial implementation of Cocoar.Capabilities library
- Core capability composition system with `ICapability<T>` and `ICapabilityBag<T>`
- High-performance `CapabilityBag<T>` with Dictionary<Type, Array> storage
- Thread-safe, immutable design with zero-allocation performance guarantees
- `CapabilityBagBuilder<T>` with fluent API for capability composition
- `Composer` helper class for ergonomic capability bag creation
- Extension methods for common capability operations
- Comprehensive test suite with 50+ tests covering all scenarios
- Complete documentation with README, examples, and integration guide
- Support for .NET 9.0 with AOT-friendly design

### Technical Details
- **Zero Dependencies**: No external package dependencies
- **Performance**: Zero-allocation design with exact-type matching semantics  
- **Thread Safety**: Immutable capability bags, thread-safe operations
- **Type Safety**: Compile-time type checking with generic constraints
- **Extensibility**: Open for extension via capability composition patterns

## [1.0.0] - TBD

Initial stable release.

---

## Release Process

This project uses [GitVersion](https://gitversion.net/) for automated semantic versioning:

- **Major version** (x.0.0): Breaking changes to public API
- **Minor version** (1.x.0): New features, backward compatible
- **Patch version** (1.0.x): Bug fixes, backward compatible

### Branch Strategy
- `main`: Stable releases (patch increments)
- `develop`: Next minor version development  
- `feature/*`: Feature development (no version impact)
- `hotfix/*`: Critical fixes (patch increments)

### Version Tags
- `v1.0.0`: Stable release
- `v1.0.0-beta.1`: Beta release
- `v1.0.0-alpha.1`: Alpha release