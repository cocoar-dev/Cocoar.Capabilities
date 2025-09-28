# Cocoar.Capabilities Performance Analysis

## Executive Summary

Cocoar.Capabilities offers **two architectures** with distinct performance characteristics, enabling you to choose the optimal approach for your scenario. Our comprehensive benchmarks demonstrate excellent performance across both Core-Only and Registry architectures.

## Architecture Comparison

### Core-Only Architecture (`Cocoar.Capabilities.Core`)
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
- **.NET Standard 2.0** - maximum platform compatibility
- **AOT-friendly** - no runtime code generation
- **Zero dependencies** - no external library requirements
- **Assembly sizes**: Core ~21KB, Registry total ~16KB

## Benchmark Environment

**Hardware**: Snapdragon X Elite - X1E78100 @ 3.42GHz (ARM64)  
**Runtime**: .NET 9.0.9 with RyuJIT  
**Configuration**: Release build, BenchmarkDotNet with statistical analysis  
**Methodology**: 13-15 iterations with outlier detection and confidence intervals  

> Performance results are representative but will vary by hardware, runtime version, and workload characteristics. Use these numbers for relative comparison and architectural decision-making.

## Migration Guidance

### From Registry to Core-Only
```csharp
// BEFORE (Registry)
var composition = Composer.For(subject).Add(...).BuildAndRegister();
// Later...
var found = Composition.FindOrDefault(subject);

// AFTER (Core-Only)  
var composition = Composer.For(subject).Add(...).Build();
// Store in your existing object lifecycle system
_serviceRegistry.Register(subject, composition);
```

### From Core-Only to Registry
```csharp
// BEFORE (Core-Only)
var composition = Composer.For(subject).Add(...).Build();
_myStorage[subject] = composition;

// AFTER (Registry)
var composition = Composer.For(subject).Add(...).BuildAndRegister();
// Automatic global registration
```

The choice is reversible - the capability definition and query APIs remain identical across both architectures.