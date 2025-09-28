# Cocoar Capabilities System - Master Implementation Plan

**Version**: 1.0 Final  
**Date**: September 27, 2025  
**Status**: Ready for Implementation  
**Reviewers**: 2 Technical Experts (Approved)  

---

## 🎯 Executive Summary

We're building a **general-purpose Capabilities System library** (`Cocoar.Capabilities`) that solves cross-project extensibility problems. This library will serve as the foundation for `Cocoar.Configuration` and can be reused in any .NET project needing composable, type-safe capability attachment.

### Key Benefits
- **Cross-Project Extension**: DI project adds capabilities to Core objects without circular dependencies
- **Type Safety**: Compile-time guarantees for capability-subject relationships  
- **Performance**: Zero-allocation hot paths, immutable by design
- **Ergonomic**: Beautiful wrapper APIs for common scenarios
- **Extensible**: Full Capabilities System power available for advanced cases

---

## 🏗️ Architecture Overview

### Core Design Principles
1. **Immutable by Design**: No `Freeze()` method, no runtime state bugs
2. **Type-Safe Contracts**: `ICapability<TSubject>` ensures capabilities match subjects
3. **Multi-First Storage**: Internal lists avoid single→multi promotion edge cases
4. **Zero-Allocation Performance**: Dictionary<Type, Array> with cached empty arrays
5. **One-Shot Builder**: Builder dies after `Build()`, bag lives forever

### Layer Architecture
```
┌─────────────────────────────────────────────────────┐
│  Ergonomic Wrapper APIs (Phase 2)                  │
│  Configure.Type<T>().ExposeAs<I>().AsSingleton()   │
├─────────────────────────────────────────────────────┤
│  Capabilities System Core (Phase 1)                │
│  Composer.For(T).Add(capability).Build()           │
├─────────────────────────────────────────────────────┤
│  ICapabilityBag<T> Implementation                  │
│  Dictionary<Type, Array> storage                    │
└─────────────────────────────────────────────────────┘
```

---

## 🔒 Core Invariants (LOCKED - Don't Change)

### 1. Retrieval Semantics
- **Exact Type Matching Only**: `TryGet<SingletonCapability>()` only finds `SingletonCapability`, not base classes or interfaces
- **API Methods**: `TryGet<T>()`, `GetRequired<T>()`, `GetAll<T>()`
- **Future**: Assignable-type matching can be added later without breaking changes

**⚠️ Common Pitfall**: Developers often add concrete capabilities but try to retrieve by interface:
```csharp
// ❌ Won't work - exact-type matching only
builder.Add(new ConcreteCapability()); // Registered as ConcreteCapability
bag.TryGet<ICapabilityInterface>(out _); // Returns false!

// ✅ Use AddAs<TContract>() for interface retrieval  
builder.AddAs<ICapabilityInterface>(new ConcreteCapability());
bag.TryGet<ICapabilityInterface>(out _); // Works perfectly!
```

### 2. Ordering & Precedence Rules
```csharp
public interface IOrderedCapability { int Order { get; } }

// Ordering Rules:
// 1. Lower Order value runs first (0, 10, 100...)
// 2. Non-IOrderedCapability treated as Order = 0
// 3. Stable tie-breaker: insertion order
// 4. Builder sorts ONCE at Build() time
// 5. TryGet/GetRequired return FIRST item after ordering
```

### 3. Builder Lifecycle
```csharp
var builder = Composer.For(subject);
var bag1 = builder.Build();  // ✅ First call succeeds
var bag2 = builder.Build();  // ❌ Throws InvalidOperationException
// After Build(): builder is unusable, bag is fully immutable
```

### 4. Storage & Performance
- **Internal**: `Dictionary<Type, Array>` - arrays typed to concrete capability types for type safety
- **External**: `IReadOnlyList<TCapability>` for `GetAll()` via safe casting of typed arrays
- **Zero-Allocation**: No LINQ in hot paths, `Array.Empty<TCapability>()` for missing capabilities
- **Thread Safety**: Bag is thread-safe (immutable), Builder is NOT thread-safe

### 5. Exception Messages
```csharp
// Clear, actionable error messages
bag.GetRequired<MissingCapability>(); 
// → "Capability 'MissingCapability' not found for subject 'DatabaseConfig'. Available: [SingletonCapability, ExposeAsCapability]"
```

### 6. DI/AOT Constraints (Phase 1)
- **No DI Dependencies**: Core library is DI-agnostic
- **No Runtime Reflection**: All operations use compile-time generics
- **AOT-Friendly**: No dynamic code generation in core

---

## 📋 Complete API Specification

### Phase 1: Core API
```csharp
namespace Cocoar.Capabilities;

// === CORE INTERFACES ===
public interface ICapability { }
public interface ICapability<in TSubject> : ICapability { }

public interface ICapabilityBag<TSubject>
{
    // Subject access
    TSubject Subject { get; }
    
    // Capability retrieval
    bool TryGet<TCapability>(out TCapability capability) where TCapability : class, ICapability<TSubject>;
    TCapability GetRequired<TCapability>() where TCapability : class, ICapability<TSubject>;
    IReadOnlyList<TCapability> GetAll<TCapability>() where TCapability : class, ICapability<TSubject>;
    
    // Convenience methods
    bool Contains<TCapability>() where TCapability : class, ICapability<TSubject>;
    int Count<TCapability>() where TCapability : class, ICapability<TSubject>;
    int TotalCapabilityCount { get; }
}

// === BUILDER ===
public sealed class CapabilityBagBuilder<TSubject> where TSubject : notnull
{
    public CapabilityBagBuilder(TSubject subject);
    public TSubject Subject { get; }
    
    public CapabilityBagBuilder<TSubject> Add<TCapability>(TCapability capability) 
        where TCapability : ICapability<TSubject>;
    
    // Register under an interface/contract for exact-type retrieval by that contract
    public CapabilityBagBuilder<TSubject> AddAs<TContract>(ICapability<TSubject> capability)
        where TContract : class, ICapability<TSubject>;
    
    public ICapabilityBag<TSubject> Build(); // One-shot operation
}

// === ORDERING SUPPORT ===
public interface IOrderedCapability 
{ 
    int Order { get; } // Lower values run first
}

// === HELPERS ===
public static class Composer
{
    public static CapabilityBagBuilder<TSubject> For<TSubject>(TSubject subject) 
        where TSubject : notnull => new(subject);
}

// === EXTENSIONS ===
public static class CapabilityBagExtensions
{
    public static void Use<TSubject, TCapability>(
        this ICapabilityBag<TSubject> bag, Action<TCapability> action)
        where TCapability : class, ICapability<TSubject>
    {
        if (bag.TryGet<TCapability>(out var capability)) action(capability);
    }
    
    public static TResult? Transform<TSubject, TCapability, TResult>(
        this ICapabilityBag<TSubject> bag, Func<TCapability, TResult> transformer)
        where TCapability : class, ICapability<TSubject>
    {
        return bag.TryGet<TCapability>(out var capability) ? transformer(capability) : default;
    }
}
```

### Phase 2: Wrapper APIs (Future)
```csharp
namespace Cocoar.Capabilities.Wrappers;

public static class Configure
{
    public static TypeConfigBuilder<T> Type<T>() where T : class, new();
    public static TypeConfigBuilder<T> Type<T>(T instance) where T : class;
    public static InterfaceConfigBuilder<T> Interface<T>() where T : class;
}

public class TypeConfigBuilder<T> where T : class
{
    public TypeConfigBuilder<T> ExposeAs<TInterface>();
    public TypeConfigBuilder<T> AsSingleton();
    public TypeConfigBuilder<T> AsScoped();
    public TypeConfigBuilder<T> AsTransient();
    public TypeConfigBuilder<T> SkipRegistration();
    public TypeConfigBuilder<T> WithHealthCheck(string name);
    public ICapabilityBag<T> Build();
}
```

---

## 🚀 Implementation Roadmap

### Phase 1: Core Foundation (Week 1) - PRIORITY
**Goal**: Production-ready core capabilities system

#### Deliverables
- [ ] `Cocoar.Capabilities` project with core interfaces
- [ ] `CapabilityBag<T>` implementation with `Dictionary<Type, Array>` storage for type safety
- [ ] `CapabilityBagBuilder<T>` with one-shot Build() lifecycle and proper null guards
- [ ] `IOrderedCapability` support with stable sorting
- [ ] Zero-allocation performance optimization
- [ ] Comprehensive unit test suite (100% coverage for core types, 95%+ overall)
- [ ] Clear exception messages with helpful diagnostics
- [ ] Performance benchmarks proving zero-allocation claims

#### Success Criteria
1. **All invariants implemented correctly**
2. **Zero-allocation TryGet/GetAll with type-safe array casting**
3. **Proper ordering with insertion-order stability**
4. **One-shot Build() with clear lifecycle**
5. **Thread-safe immutable bags**
6. **Clear, helpful error messages**
7. **100% API documentation coverage**

#### Unit Tests Must Cover
```csharp
[TestClass] public class CapabilityBagTests
{
    // Core functionality
    [TestMethod] void TryGet_ExactTypeMatching_ReturnsCorrectCapability();
    [TestMethod] void GetRequired_MissingCapability_ThrowsWithClearMessage();
    [TestMethod] void GetAll_MultipleCapabilities_ReturnsInOrder();
    
    // Ordering behavior  
    [TestMethod] void Ordering_IOrderedCapability_LowerOrderFirst();
    [TestMethod] void Ordering_SameOrder_InsertionOrderStable();
    [TestMethod] void Ordering_NonOrdered_TreatedAsOrderZero();
    
    // Lifecycle
    [TestMethod] void Build_CalledTwice_ThrowsInvalidOperation();
    [TestMethod] void Builder_AfterBuild_IsUnusable();
    [TestMethod] void Bag_IsImmutable_ThreadSafe();
    
    // Performance - CRITICAL: Type-safe array casting 
    [TestMethod] void TryGet_TypeSafeArrayCasting_NeverThrows();
    [TestMethod] void GetAll_TypeSafeArrayCasting_NeverThrows();
    [TestMethod] void GetAll_EmptyResult_ReturnsArrayEmpty();
    
    // Edge cases - CRITICAL: Null guards and proper error messages
    [TestMethod] void Add_NullCapability_ThrowsArgumentNull();
    [TestMethod] void Constructor_NullSubject_ThrowsArgumentNull();
    [TestMethod] void GetRequired_MissingCapability_ThrowsWithHelpfulMessage();
}
```

### Phase 2: Wrapper APIs (Week 2)
**Goal**: Ergonomic APIs for common scenarios

#### Deliverables
- [ ] `Configure.Type<T>()` fluent API
- [ ] Common capability implementations (ExposeAs, Lifetime, SkipRegistration)
- [ ] `ConfigurationBuilder` with fluent composition
- [ ] Integration examples with dependency injection
- [ ] Migration helpers from raw Feature System to wrappers

### Phase 3: Advanced Capabilities (Week 3)
**Goal**: Cross-project contributors and source generation

#### Deliverables
- [ ] `ICapabilityContributor<T>` interface
- [ ] `[ContributesCapabilities]` attribute
- [ ] Source generator for `CapabilityRegistry<T>`
- [ ] Cross-assembly capability discovery
- [ ] Performance optimizations for contributor scenarios

### Phase 4: Cocoar.Configuration Integration (Week 4)
**Goal**: Replace existing system with Feature System

#### Deliverables
- [ ] Define configuration-specific features
- [ ] Update `AddCocoarConfiguration` to use Feature System
- [ ] Migrate all builders and processing logic
- [ ] Backward compatibility adapters
- [ ] Update all tests and examples
- [ ] Performance comparison with old system

### Phase 5: Polish & Documentation (Week 5)  
**Goal**: Production-ready release

#### Deliverables
- [ ] Complete API documentation
- [ ] Usage guides and examples
- [ ] Performance benchmarks
- [ ] NuGet package preparation
- [ ] Migration guide from old APIs
- [ ] Blog post about cross-project extensibility patterns

---

## 💡 Usage Examples

### Basic Capabilities System Usage
```csharp
// Define capabilities for a configuration type
public record ExposeAsCapability(Type ContractType) : ICapability<DatabaseConfig>;
public record SingletonLifetimeCapability : ICapability<DatabaseConfig>;
public record HealthCheckCapability(string Name) : ICapability<DatabaseConfig>;

// Create capability bag
var dbConfig = new DatabaseConfig { ConnectionString = "..." };
var bag = Composer.For(dbConfig)
    .Add(new ExposeAsCapability(typeof(IDbConfig)))
    .Add(new SingletonLifetimeCapability())
    .Add(new HealthCheckCapability("database"))
    .Build();

// Use capabilities
if (bag.TryGet<SingletonLifetimeCapability>(out var _))
{
    services.AddSingleton<DatabaseConfig>(bag.Subject);
}

foreach (var expose in bag.GetAll<ExposeAsCapability>())
{
    services.AddSingleton(expose.ContractType, provider => bag.Subject);
}
```

### With Wrapper APIs (Phase 2)
```csharp
services.AddCocoarConfiguration(rules, config => config
    .Configure<DatabaseConfig>(db => db
        .ExposeAs<IDbConfig>()
        .AsSingleton()
        .WithHealthCheck("database"))
    .Configure<CacheConfig>(cache => cache
        .AsScoped()
        .SkipRegistration()));
```

### Cross-Project Extension
```csharp
// Core project defines subjects
namespace MyApp.Core {
    public class DatabaseConfig { ... }
}

// DI project defines capabilities for Core subjects  
namespace MyApp.DI {
    public record ExposeAsCapability(Type ContractType) : ICapability<DatabaseConfig>;
}

// AspNetCore project adds more capabilities
namespace MyApp.Web {
    public record HealthCheckCapability(string Name) : ICapability<DatabaseConfig>;
}

// Composition works seamlessly
var config = Composer.For(new DatabaseConfig())
    .Add(new ExposeAsCapability(typeof(IDbConfig)))  // From DI project
    .Add(new HealthCheckCapability("db"))           // From Web project
    .Build();
```

---

## 🎯 Integration with Cocoar.Configuration

### Current Problem
```csharp
// Messy, inconsistent APIs
services.AddCocoarConfiguration(new[]{ Rule(config) }, configured: new object[]{
    Configure.ConcreteType<DatabaseConfig>().ExposeAs<IDbConfig>().Build()
});
```

### With Capabilities System
```csharp
// Clean, type-safe, extensible
services.AddCocoarConfiguration(rules, configure => [
    Composer.For(new DatabaseConfig())
        .Add(new ExposeAsCapability(typeof(IDbConfig)))
        .Add(new SingletonLifetimeCapability())
        .Build()
]);
```

### With Wrapper APIs
```csharp
// Beautiful, ergonomic
services.AddCocoarConfiguration(rules, config => config
    .Configure<DatabaseConfig>(db => db
        .ExposeAs<IDbConfig>()
        .AsSingleton()));
```

---

## 🚨 CRITICAL FIXES REQUIRED BEFORE CODING

**These issues MUST be addressed to avoid runtime failures and type safety violations:**

### 1. Array Covariance Bug (Runtime Type Safety)
**Problem**: Original design used `Dictionary<Type, IFeature<TSubject>[]>` with unsafe casting:
```csharp
// ❌ UNSAFE - Can throw InvalidCastException at runtime
return (IReadOnlyList<TFeature>)(object)features;
```

**Solution**: Use `Dictionary<Type, Array>` with concrete typed arrays:
```csharp
// ✅ SAFE - Arrays are typed exactly to feature types
var arr = Array.CreateInstance(typeof(TFeature), count);
return (TFeature[])arr; // Safe because array element type matches exactly
```

### 2. Missing Interface Members
**Problem**: `ICapabilityBag<T>` declares members not implemented in sample code:
- `Contains<TCapability>()`
- `Count<TCapability>()`  
- `TotalCapabilityCount`

**Solution**: Implement all declared interface members (see corrected code above).

### 3. Null Guards and Constructor Issues
**Problem**: Missing null checks and subject assignment in builder constructor.

**Solution**: Add proper guards and initialization:
```csharp
public FeatureBagBuilder(TSubject subject)
    => _subject = subject ?? throw new ArgumentNullException(nameof(subject));
```

### 4. Exception Message Implementation
**Problem**: Specification requires helpful error messages but implementation was missing.

**Solution**: Implement clear, actionable messages with available feature types.

### 5. Coverage Target Consistency
**Problem**: Document mentioned both "95%+" and "100%" coverage targets.

**Solution**: **100% coverage for core types** (`FeatureBag`, `FeatureBagBuilder`), **95%+ overall project coverage**.

---

## 🛠️ Technical Implementation Details

### Storage Implementation
```csharp
internal sealed class CapabilityBag<TSubject> : ICapabilityBag<TSubject>
{
    private readonly TSubject _subject;
    // Arrays are typed to concrete capability types (e.g., ExposeAsCapability[])
    private readonly IReadOnlyDictionary<Type, Array> _capabilitiesByType;
    private readonly int _totalCapabilityCount;
    
    public CapabilityBag(TSubject subject, IReadOnlyDictionary<Type, Array> capabilitiesByType, int totalCapabilityCount)
    {
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
        _capabilitiesByType = capabilitiesByType;
        _totalCapabilityCount = totalCapabilityCount;
    }
    
    public TSubject Subject => _subject;
    public int TotalCapabilityCount => _totalCapabilityCount;
    
    public bool TryGet<TCapability>(out TCapability capability) where TCapability : class, ICapability<TSubject>
    {
        if (_capabilitiesByType.TryGetValue(typeof(TCapability), out var arr) && arr.Length > 0)
        {
            capability = ((TCapability[])arr)[0]; // Safe: array element type is exactly TCapability
            return true;
        }
        capability = null!;
        return false;
    }
    
    public TCapability GetRequired<TCapability>() where TCapability : class, ICapability<TSubject>
    {
        if (TryGet<TCapability>(out var c)) return c;
        
        // Clear exception message with available capability types
        var available = _capabilitiesByType.Keys.Select(t => t.Name).OrderBy(n => n);
        var msg = $"Capability '{typeof(TCapability).Name}' not found for subject '{Subject?.GetType().Name}'. " +
                  $"Available: [{string.Join(", ", available)}]";
        throw new InvalidOperationException(msg);
    }
    
    public IReadOnlyList<TCapability> GetAll<TCapability>() where TCapability : class, ICapability<TSubject>
    {
        return _capabilitiesByType.TryGetValue(typeof(TCapability), out var arr)
            ? (TCapability[])arr // Safe: array is typed exactly as TCapability[]
            : Array.Empty<TCapability>(); // Zero-allocation empty result
    }
    
    public bool Contains<TCapability>() where TCapability : class, ICapability<TSubject>
        => _capabilitiesByType.TryGetValue(typeof(TCapability), out var arr) && arr.Length > 0;

    public int Count<TCapability>() where TCapability : class, ICapability<TSubject>
        => _capabilitiesByType.TryGetValue(typeof(TCapability), out var arr) ? arr.Length : 0;
}
```

### Builder Implementation
```csharp
public sealed class CapabilityBagBuilder<TSubject> where TSubject : notnull
{
    private readonly TSubject _subject;
    // Store by type with concrete capability instances
    private readonly Dictionary<Type, List<ICapability<TSubject>>> _capabilitiesByType = new();
    private bool _built = false;
    
    public CapabilityBagBuilder(TSubject subject)
    {
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
    }
    
    public TSubject Subject => _subject;
    
    public CapabilityBagBuilder<TSubject> Add<TCapability>(TCapability capability) 
        where TCapability : ICapability<TSubject>
    {
        if (_built) throw new InvalidOperationException("Build() has already been called.");
        if (capability is null) throw new ArgumentNullException(nameof(capability));
        
        var type = typeof(TCapability);
        if (!_capabilitiesByType.TryGetValue(type, out var list))
        {
            list = new List<ICapability<TSubject>>();
            _capabilitiesByType[type] = list;
        }
        
        list.Add(capability);
        return this;
    }
    
    public ICapabilityBag<TSubject> Build()
    {
        if (_built) throw new InvalidOperationException("Build() can only be called once.");
        _built = true;
        
        // Build arrays typed to concrete capability types
        var result = new Dictionary<Type, Array>(_capabilitiesByType.Count);
        int totalCount = 0;
        
        foreach (var (type, list) in _capabilitiesByType)
        {
            var ordered = SortCapabilities(list);
            // Create array typed exactly to the capability type (e.g., ExposeAsCapability[])
            var arr = Array.CreateInstance(type, ordered.Count);
            for (int i = 0; i < ordered.Count; i++)
            {
                arr.SetValue(ordered[i], i);
            }
            
            result[type] = arr;
            totalCount += arr.Length;
        }
        
        return new CapabilityBag<TSubject>(_subject, result, totalCount);
    }
    
    // Optional: Register capability under specific contract type
    public CapabilityBagBuilder<TSubject> AddAs<TContract>(ICapability<TSubject> capability)
        where TContract : class, ICapability<TSubject>
    {
        if (_built) throw new InvalidOperationException("Build() has already been called.");
        if (capability is null) throw new ArgumentNullException(nameof(capability));
        
        var type = typeof(TContract);
        if (!_capabilitiesByType.TryGetValue(type, out var list))
        {
            list = new List<ICapability<TSubject>>();
            _capabilitiesByType[type] = list;
        }
        
        list.Add(capability);
        return this;
    }
    
    private static List<ICapability<TSubject>> SortCapabilities(List<ICapability<TSubject>> capabilities)
    {
        return capabilities
            .Select((capability, index) => new { capability, index })
            .OrderBy(x => (x.capability as IOrderedCapability)?.Order ?? 0)  // Order first
            .ThenBy(x => x.index)  // Insertion order for ties
            .Select(x => x.capability)
            .ToList();
    }
}
```

**Important**: The `AddAs<TContract>()` method prevents the most common developer mistake with exact-type matching. Since `TryGet<ICapability>()` won't find capabilities registered as concrete types, this helper explicitly registers capabilities under their contract types. **Always use `AddAs<T>()` when you plan to retrieve by interface!**

---

## 🧪 Testing Strategy

### Unit Test Categories
1. **Core Functionality**: Capability storage, retrieval, ordering
2. **Lifecycle Management**: Builder state, one-shot Build(), immutability  
3. **Performance**: Zero-allocation guarantees, memory efficiency
4. **Error Handling**: Clear exception messages, edge cases
5. **Thread Safety**: Concurrent access to immutable bags
6. **API Contracts**: Generic constraints, type safety

### Performance Benchmarks
```csharp
[Benchmark]
public bool TryGet_ExistingCapability() => _bag.TryGet<TestCapability>(out var _);

[Benchmark]  
public bool TryGet_MissingCapability() => _bag.TryGet<MissingCapability>(out var _);

[Benchmark]
public IReadOnlyList<TestCapability> GetAll_MultipleCapabilities() => _bag.GetAll<TestCapability>();

// Target: Zero allocations in steady state
```

---

## 📦 Project Structure

```
src/
├── Cocoar.Capabilities/                   # Core library
│   ├── ICapability.cs
│   ├── ICapabilityBag.cs  
│   ├── CapabilityBag.cs
│   ├── CapabilityBagBuilder.cs
│   ├── IOrderedCapability.cs
│   ├── Composer.cs                        # Static helpers
│   └── Extensions/
│       └── CapabilityBagExtensions.cs
├── Cocoar.Capabilities.Wrappers/          # Phase 2: Ergonomic APIs
│   ├── Configure.cs
│   ├── TypeConfigBuilder.cs
│   └── ConfigurationBuilder.cs
├── Cocoar.Capabilities.DI/                # Phase 2: DI integration
├── Cocoar.Capabilities.Generator/         # Phase 3: Source generation
└── Cocoar.Capabilities.Benchmarks/       # Performance testing

tests/
├── Cocoar.Capabilities.Tests/             # Core unit tests
├── Cocoar.Capabilities.Integration.Tests/ # Integration scenarios  
└── Cocoar.Capabilities.Performance.Tests/ # Benchmark tests
```

---

## ✅ Definition of Done

### Phase 1 Complete When:
- [ ] All core APIs implemented with exact specifications
- [ ] All invariants enforced (exact-type matching, one-shot Build(), etc.)
- [ ] Zero-allocation performance achieved and benchmarked
- [ ] 100% unit test coverage with comprehensive edge cases
- [ ] Clear, helpful exception messages implemented
- [ ] Thread safety verified for immutable bags
- [ ] API documentation complete with examples
- [ ] Integration spike with Cocoar.Configuration successful

### Overall Project Complete When:
- [ ] All 5 phases delivered according to specifications
- [ ] Cocoar.Configuration fully migrated to Capabilities System
- [ ] Performance equal or better than original system
- [ ] Backward compatibility maintained where possible
- [ ] Complete documentation and usage guides
- [ ] NuGet packages published and tested

---

## 🎯 Next Actions

1. **Create `Cocoar.Capabilities` project** in solution
2. **Implement core interfaces** (`ICapability`, `ICapabilityBag<T>`)
3. **Build `CapabilityBag<T>` with Dictionary storage** 
4. **Implement `CapabilityBagBuilder<T>` with one-shot lifecycle**
5. **Add comprehensive unit tests** covering all invariants
6. **Performance benchmark** to verify zero-allocation claims
7. **Integration spike** with simple Cocoar.Configuration example

**Ready to begin implementation!** 🚀

---

*This document represents the complete, approved plan for the Cocoar Capabilities System. All decisions are final and implementation should follow these specifications exactly to avoid future refactoring.*