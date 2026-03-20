# Changelog

## [1.2.1] - 2026-03-20

### Fixed
- **ScopeOwnerApi**: `Get<T>()` now returns `null` instead of throwing when the owner is not set, has been garbage collected, or is not of the expected type. `GetOrThrow<T>()` retains the throwing behavior and is no longer just an alias for `Get<T>()`.
- **ScopeAnchorsApi**: `Get<T>()` and `Get(string)` now return `null` instead of throwing when the anchor is not set or has been garbage collected. `GetOrThrow<T>()` and `GetOrThrow(string)` retain the throwing behavior.

## [1.2.0] - 2025-11-03

### Added
- **Strongly-Typed Scopes**: `CapabilityScope<TOwner>` for compile-time type safety
  - Generic scope class with immutable owner set at construction
  - Constructors: `CapabilityScope(TOwner owner)` and `CapabilityScope(TOwner owner, CapabilityScopeOptions options)`
  - Inherits from `CapabilityScope` for full backward compatibility
  - Enables domain-specific scope classes (e.g., `class PipelineScope : CapabilityScope<PipelineHost>`)
- **Strongly-Typed Owner API**: `ScopeOwnerApi<TOwner>` with no generic parameters needed
  - Methods: `Get()`, `TryGet()`, `Compose()`, `GetComposition()`, `GetRequiredComposition()`, `GetComposer()`, `GetRequiredComposer()`
  - All methods work with concrete owner type without generic parameters
  - Try-pattern methods: `TryGetComposition()`, `TryGetComposer()`
- **Enhanced ScopeOwnerApi**: Try-pattern and typed methods
  - Added `TryGetComposition(out Composition?)` for safe composition retrieval
  - Added `TryGetComposer(out Composer?)` for safe composer retrieval
  - Added `ComposeFor<T>()` and `GetCompositionFor<T>()` for type-checked operations
  - Added `GetComposerFor<T>()`, `GetRequiredComposerFor<T>()` for composer registry operations
  - Obsolete attributes on old method names (`Compose<T>()` → `ComposeFor<T>()`, `GetComposition<T>()` → `GetCompositionFor<T>()`)
- **Enhanced ScopeAnchorsApi**: Consistent API surface with try-pattern, required, and *For methods
  - Added `ComposeFor<T>()` - new preferred method name with obsolete alias on `Compose<T>()`
  - Added `GetCompositionFor<T>()` - new preferred method name with obsolete alias on `GetComposition<T>()`
  - Added `TryGetCompositionFor<T>()` and `TryGetComposition(string)` for safe composition retrieval
  - Added `GetRequiredCompositionFor<T>()` and `GetRequiredComposition(string)` that throw when composition doesn't exist
  - Added `GetComposerFor<T>()`, `GetComposer(string)` for getting composers from registry
  - Added `TryGetComposerFor<T>()`, `TryGetComposer(string)` for try-pattern composer retrieval
  - Added `GetRequiredComposerFor<T>()`, `GetRequiredComposer(string)` that throw when composer not in registry
  - Complete API parity between Owner and Anchors APIs

### Documentation
- Updated README with strongly-typed scope examples
- Added comprehensive API documentation

## [1.1.0] - 2025-10-26

### Added
- **Owner and Anchors API**: Associate scopes with context objects
  - `scope.Owner.*` API for managing a single distinguished owner
  - `scope.Anchors.*` API for managing typed and named anchors
  - Owner safety: `Set()` throws if owner already set, `Replace()` for explicit replacement
  - Weak reference storage prevents memory leaks
  - Fluent chaining with `.Scope` property to return to scope
  - Direct composition via `Owner.Compose()` and `Anchors.ComposeFor<T>()`
- **Using* Extension Methods**: Fluent convenience methods for inline capability usage
  - `UsingFirst<T>()`, `UsingFirstOrDefault<T>()` - Use first capability with action or function
  - `UsingLast<T>()`, `UsingLastOrDefault<T>()` - Use last capability with action or function
  - `UsingEach<T>()` - Execute action or function for each capability
  - `UsingAll<T>()` - Execute action or function with full collection
  - Action-based overloads return `IComposition` for chaining
  - Function-based overloads return results directly

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

### Technical
- Target framework: .NET 8.0
- Thread-safe immutable compositions
- High-performance array-backed storage
- Source Link support for debugging
