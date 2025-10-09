using BenchmarkDotNet.Attributes;


namespace Cocoar.Capabilities.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class CoreVsRegistryBenchmarks
{
    public interface ICapability { }
    
    public record TestSubject(int Id, string Name);
    public record FeatureCapability(string Name) : ICapability;
    public record ConfigCapability(string Key, string Value) : ICapability;
    public record ValidationCapability(string Rule) : ICapability;
    public record CachingCapability(string CacheKey, TimeSpan Duration) : ICapability;
    public record LoggingCapability(string LoggerName) : ICapability;
    public record SecurityCapability(string Permission, string Role) : ICapability;
    public record MonitoringCapability(string MetricName) : ICapability;
    public record RetryCapability(string Operation, int MaxRetries) : ICapability;
    
    private IComposition _coreComposition = null!;
    private IComposition _registryComposition = null!;
    private TestSubject _registrySubject = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Core composition - no registry overhead
    _coreComposition = CreateCoreComposition(1, 50);
        
        // Registry composition - with global registry
        _registrySubject = new TestSubject(999, "RegistryTest");
        _registryComposition = CreateRegistryComposition(_registrySubject, 50);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Clean up registry to avoid memory leaks
        BenchmarkScopes.Shared.Compositions.Remove(_registrySubject);
    }



    [Benchmark(Description = "Core: Build 50 caps (fast)")]
    [BenchmarkCategory("Build", "Core")]
    public IComposition Build_Core_50()
    {
        return CreateCoreComposition(1, 50);
    }

    [Benchmark(Description = "Registry: Build + Register 50 caps")]
    [BenchmarkCategory("Build", "Registry")]
    public IComposition Build_Registry_50()
    {
        var subject = new TestSubject(100, "BuildTest");
        var composition = CreateRegistryComposition(subject, 50);
        
        // Clean up immediately to avoid accumulation
        BenchmarkScopes.Shared.Compositions.Remove(subject);
        return composition;
    }

    [Benchmark(Description = "Core: Build 200 caps (fast)")]
    [BenchmarkCategory("Build", "Core")]
    public IComposition Build_Core_200()
    {
        return CreateCoreComposition(1, 200);
    }

    [Benchmark(Description = "Registry: Build + Register 200 caps")]
    [BenchmarkCategory("Build", "Registry")]
    public IComposition Build_Registry_200()
    {
        var subject = new TestSubject(200, "BuildTest");
        var composition = CreateRegistryComposition(subject, 200);
        
        // Clean up immediately to avoid accumulation
        BenchmarkScopes.Shared.Compositions.Remove(subject);
        return composition;
    }



    [Benchmark(Description = "Core: Query typed capabilities")]
    [BenchmarkCategory("Query", "Core")]
    public int Query_Core_TypedCapabilities()
    {
        return _coreComposition.GetAll<FeatureCapability>().Count;
    }

    [Benchmark(Description = "Registry: Query typed capabilities")]
    [BenchmarkCategory("Query", "Registry")]
    public int Query_Registry_TypedCapabilities()
    {
        return _registryComposition.GetAll<FeatureCapability>().Count;
    }

    [Benchmark(Description = "Core: Query all capabilities")]
    [BenchmarkCategory("Query", "Core")]
    public int Query_Core_AllCapabilities()
    {
        return _coreComposition.GetAll().Count;
    }

    [Benchmark(Description = "Registry: Query all capabilities")]
    [BenchmarkCategory("Query", "Registry")]
    public int Query_Registry_AllCapabilities()
    {
        return _registryComposition.GetAll().Count;
    }



    [Benchmark(Description = "Core: Direct reference (fastest)")]
    [BenchmarkCategory("Lookup", "Core")]
    public IComposition Lookup_Core_DirectAccess()
    {
        // Core pattern: Direct composition reference (zero overhead)
        return _coreComposition;
    }

    [Benchmark(Description = "Registry: Global FindOrDefault")]
    [BenchmarkCategory("Lookup", "Registry")]
    public IComposition? Lookup_Registry_FindOrDefault()
    {
        // Registry pattern: Global lookup (convenience with overhead)
        BenchmarkScopes.Shared.Compositions.TryFind(_registrySubject, out var composition);
        return composition;
    }

    [Benchmark(Description = "Registry: TryFind pattern")]
    [BenchmarkCategory("Lookup", "Registry")]
    public bool Lookup_Registry_TryFind()
    {
        // Registry pattern: TryFind (slightly optimized)
        return BenchmarkScopes.Shared.Compositions.TryFind(_registrySubject, out _);
    }



    private static IComposition CreateCoreComposition(int subjects, int capabilitiesPerSubject)
    {
        var subject = new TestSubject(0, "CoreSubject");
        // Use lightweight scope (registries disabled)
        var composer = BenchmarkScopes.SharedLightweight.For(subject);
        
        for (int c = 0; c < capabilitiesPerSubject; c++)
        {
            var capability = CreateCapability(0, c);
            composer.Add(capability);
        }
        
        return composer.Build(); // registries disabled => no registration
    }

    private static IComposition CreateRegistryComposition(TestSubject subject, int capabilitiesPerSubject)
    {
        var composer = BenchmarkScopes.Shared.For(subject);
        
        for (int c = 0; c < capabilitiesPerSubject; c++)
        {
            var capability = CreateCapability(subject.Id, c);
            composer.Add(capability);
        }
        
        return composer.Build(useRegistry: true);
    }
    
    private static ICapability CreateCapability(int subjectId, int capabilityId)
    {
        return (capabilityId % 8) switch
        {
            0 => new FeatureCapability($"Feature_{subjectId}_{capabilityId}"),
            1 => new ConfigCapability($"Config_{subjectId}_{capabilityId}", $"Value_{capabilityId}"),
            2 => new ValidationCapability($"Validation_{subjectId}_{capabilityId}"),
            3 => new CachingCapability($"Cache_{subjectId}_{capabilityId}", TimeSpan.FromMinutes(capabilityId)),
            4 => new LoggingCapability($"Logger_{subjectId}_{capabilityId}"),
            5 => new SecurityCapability($"Security_{subjectId}_{capabilityId}", $"Role_{capabilityId}"),
            6 => new MonitoringCapability($"Monitor_{subjectId}_{capabilityId}"),
            _ => new RetryCapability($"Retry_{subjectId}_{capabilityId}", capabilityId + 1)
        };
    }
}
