# Changelog

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

