# Changelog

## [Unreleased]

### Added
- **Owner and Anchors API**: Associate scopes with context objects
  - `scope.Owner.*` API for managing a single distinguished owner
  - `scope.Anchors.*` API for managing typed and named anchors
  - Owner safety: `Set()` throws if owner already set, `Replace()` for explicit replacement
  - Weak reference storage prevents memory leaks
  - Fluent chaining with `.Scope` property to return to scope
  - Direct composition via `Owner.Compose()` and `Anchors.Compose<T>()`
  - Methods: `Set`, `Replace`, `Get`, `GetOrThrow`, `TryGet`, `Compose`, `GetComposition`
  - Comprehensive documentation in ADR, README, API reference, and quick reference guide

## [1.0.0] - 2025-10-10

### Added
- Initial release of Cocoar.Capabilities
- Type-safe capability composition with fluent API
- Primary capability support via `IPrimaryCapability` marker interface
- Capability ordering with explicit `order` parameter or `Func<object, int>` selector
- Recomposition support for modifying existing compositions
- Optional composer and composition registries
- Rich query API: `GetAll<T>()`, `GetFirst<T>()`, `GetLast<T>()`, `Has<T>()`, `Count<T>()`
- Try-Add pattern with `TryAdd()` and `TryAddAs()` methods
- Multiple contract registration with tuple syntax `AddAs<(IContract1, IContract2)>()`
- Zero-allocation `ForEach()` extension method for `IReadOnlyList<T>`
- Comprehensive documentation with examples and API reference

### Technical
- Target framework: .NET 8.0
- Thread-safe immutable compositions
- High-performance array-backed storage
- Source Link support for debugging

