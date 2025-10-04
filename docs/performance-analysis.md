# Cocoar.Capabilities Performance Analysis

## Executive Summary

Cocoar.Capabilities offers **two architectures** with distinct performance characteristics, enabling you to choose the optimal approach for your scenario. Our comprehensive benchmarks demonstrate excellent performance across both Core-Only and Registry architectures.

## Architecture Comparison

### Registry Disabled (legacy docs term: Core-Only)
**Maximum performance** - you manage composition lifetimes directly:
- **Build**: ~4.6 μs (50 capabilities), ~42 μs (500 capabilities)
- **Query**: ~142 ns (feature queries), ~1 μs (all capabilities)  
- **Memory**: 11-102 KB build allocations, 320B-1.2KB query allocations

### Registry Architecture (`Cocoar.Capabilities`)
**Convenience with overhead** - automatic global composition storage:
- **Build**: ~8.3 μs (50 capabilities), ~47 μs (500 capabilities) - *+79% and +13% overhead*
- **Query**: ~151 ns (feature queries), ~8.5 μs (large all capabilities) - *+6% to +757% overhead*
- **Memory**: Similar to Core with small registry overhead (27-34 bytes)

## Detailed Performance Results

### Build Performance
| Architecture | Small (50 caps) | Large (500 caps) | Overhead | 
|-------------|------------------|------------------|----------|
| **Core** | 4.64 μs | 41.68 μs | Baseline |
| **Registry** | 8.30 μs | 47.17 μs | +79% / +13% |

### Query Performance - Feature Capabilities
| Architecture | Small | Large | Overhead |
|-------------|-------|-------|----------|
| **Core** | 142 ns | 139 ns | Baseline |
| **Registry** | 150 ns | 850 ns | +6% / +511% |

### Query Performance - All Capabilities  
| Architecture | Small | Large | Overhead |
|-------------|-------|-------|----------|
| **Core** | 1.06 μs | 992 ns | Baseline |
| **Registry** | 979 ns | 8.5 μs | -8% / +757% |

## Performance Characteristics

### Scaling Behavior
- **Core Builds**: Linear scaling - 10x capabilities = ~8x time
- **Core Queries**: Constant time regardless of composition size
- **Registry Builds**: Similar linear scaling with consistent overhead
- **Registry Queries**: Variable overhead depending on operation complexity

### Memory Allocation Patterns
| Scenario | Core Allocation | Registry Overhead | Efficiency |
|----------|-----------------|-------------------|------------|
| Small Build | 10.84 KB | +27 bytes | 99.75% |
| Large Build | 101.79 KB | +34 bytes | 99.97% |
| Feature Query | 320 B | +0 bytes | 100% |
| All Query (Small) | 1.18 KB | +0 bytes | 100% |
| All Query (Large) | 1.18 KB | +7.2 KB | 14% |

### Optional Capability Ordering Overhead

Capability ordering is conditional. If no capability implements `IOrderedCapability`, the ordering scan exits immediately (O(n) predicate scan with early break, typically branch-predictable) and no sort occurs.

When ordered capabilities are present:
1. A single pass records original indices to preserve stability when duplicate `Order` values exist.
2. A one-time stable sort runs using the original index map as a secondary key.
3. The resulting arrays are cached in the immutable composition; subsequent `GetAll()` / `GetAll<T>()` calls do not re-sort.

Benchmark highlights (relative observations – see `OrderingBenchmarks` for reproducible runs):
- Already sorted or small sets: sort cost often below noise threshold.
- Reverse-ordered worst case: additional cost is proportional to `n log n` but still dominated by capability instantiation for moderate sizes (≤500).
- Duplicate order groups: stability bookkeeping adds a small dictionary allocation already amortized by capability count.
- Recompositions with no structural change: skip sort entirely (arrays reused) yielding near-zero overhead.

Practical guidance: only pay for ordering when you declare it; you can freely mix ordered and unordered capability sets without global penalties.

## Understanding Registry Overhead

### Why Overhead Exists
Registry overhead is **inherent to any composition storage system**:

1. **Build Phase**: Core build + registration in global storage
2. **Query Phase**: Core query + storage lookup + additional indirection

This is **not** a library limitation - it's the fundamental cost of persistent composition storage. Any mechanism providing global composition access (DI containers, caches, etc.) would have similar overhead.

### When Overhead Matters
- **High-frequency builds**: Core is 13-79% faster
- **Complex queries on large sets**: Core is 511-757% faster
- **Simple queries on small sets**: Registry can be 8% faster (caching benefits)

## Performance Optimization Guidelines

### Choose Core-Only When:
✅ Building many large compositions  
✅ Performing frequent capability queries  
✅ Maximum performance is critical  
✅ You have existing object lifecycle management  
✅ Memory efficiency is paramount  

### Choose Registry When:
✅ Global composition discovery is needed  
✅ Simple compositions with occasional queries  
✅ Convenience outweighs performance  
✅ No existing object storage mechanisms  
✅ Prototype/development speed is priority  

## Technical Implementation Details

### Thread Safety
- **Immutable by design** - no locks required
- **Concurrent reads** - unlimited parallelism for queries
- **Isolated builds** - each composition build is independent

### Memory Management
- **Weak references** for reference types (automatic cleanup)
- **Explicit control** for value types (prevents leaks)
- **Zero allocations** for capability existence checks (`Has<T>()`)
- **Minimal GC pressure** during normal operations

### Framework Compatibility
Targets modern .NET (net8.0+) with AOT-friendly design (no runtime code generation) and zero external dependencies. Legacy multi-package (.Core vs registry) distribution has been consolidated into a single package. Assembly size remains small (~tens of KB) and stable across configurations.

## Benchmark Environment

**Hardware**: Snapdragon X Elite - X1E78100 @ 3.42GHz (ARM64)  
**Runtime**: .NET 9.0.9 with RyuJIT  
**Configuration**: Release build, BenchmarkDotNet with statistical analysis  
**Methodology**: 13-15 iterations with outlier detection and confidence intervals  

> Performance results are representative but will vary by hardware, runtime version, and workload characteristics. Use these numbers for relative comparison and architectural decision-making.

## Migration Guidance (Legacy Static API → Scope API)

Previous examples used static helpers (`Composer.For`, `BuildAndRegister`, `Composition.FindOrDefault`). Transition to `CapabilityScope` is direct:

### Legacy (Static)
```csharp
using var legacyScope = new CapabilityScope(new CapabilityScopeOptions { UseCompositionRegistry = true });
var composition = legacyScope.For(subject).Add(...).Build(); // registered (options enabled)
var found = legacyScope.Compositions.FindOrDefault(subject);
```

### Current (Scope + Options)
```csharp
using var scope = new CapabilityScope(new CapabilityScopeOptions { UseCompositionRegistry = true });
var composition = scope.For(subject).Add(...).Build(); // auto-registered
var found = scope.Compositions.FindOrDefault(subject);
```

Disable registry for max performance:
```csharp
using var scope = new CapabilityScope(new CapabilityScopeOptions { UseCompositionRegistry = false });
var localOnly = scope.For(subject).Add(...).Build(); // not tracked
```

Force a single build to register even if globally disabled:
```csharp
var mixed = scope.For(subject).Add(...).Build(useRegistry: true);
```

See `static-api-migration-strategy.md` for deeper rewrite patterns.