using BenchmarkDotNet.Attributes;
using System;
using System.Linq;
using Cocoar.Capabilities.Core;
using Cocoar.Capabilities;

namespace Cocoar.Capabilities.Benchmarks;

/// <summary>
/// Performance comparison between Core-only operations (fast) and Registry-enabled operations (convenient).
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class CoreVsRegistryBenchmarks
{
    public record TestSubject(int Id, string Name);
    public record FeatureCapability(string Name) : ICapability<TestSubject>;
    public record ConfigCapability(string Key, string Value) : ICapability<TestSubject>;
    public record ValidationCapability(string Rule) : ICapability<TestSubject>;
    public record CachingCapability(string CacheKey, TimeSpan Duration) : ICapability<TestSubject>;
    public record LoggingCapability(string LoggerName) : ICapability<TestSubject>;
    public record SecurityCapability(string Permission, string Role) : ICapability<TestSubject>;
    public record MonitoringCapability(string MetricName) : ICapability<TestSubject>;
    public record RetryCapability(string Operation, int MaxRetries) : ICapability<TestSubject>;
    
    private IComposition<TestSubject> _coreComposition = null!;
    private IComposition<TestSubject> _registryComposition = null!;
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
        Composition.Remove(_registrySubject);
    }



    [Benchmark(Description = "Core: Build 50 caps (fast)")]
    [BenchmarkCategory("Build", "Core")]
    public IComposition<TestSubject> Build_Core_50()
    {
        return CreateCoreComposition(1, 50);
    }

    [Benchmark(Description = "Registry: Build + Register 50 caps")]
    [BenchmarkCategory("Build", "Registry")]
    public IComposition<TestSubject> Build_Registry_50()
    {
        var subject = new TestSubject(100, "BuildTest");
        var composition = CreateRegistryComposition(subject, 50);
        
        // Clean up immediately to avoid accumulation
        Composition.Remove(subject);
        return composition;
    }

    [Benchmark(Description = "Core: Build 200 caps (fast)")]
    [BenchmarkCategory("Build", "Core")]
    public IComposition<TestSubject> Build_Core_200()
    {
        return CreateCoreComposition(1, 200);
    }

    [Benchmark(Description = "Registry: Build + Register 200 caps")]
    [BenchmarkCategory("Build", "Registry")]
    public IComposition<TestSubject> Build_Registry_200()
    {
        var subject = new TestSubject(200, "BuildTest");
        var composition = CreateRegistryComposition(subject, 200);
        
        // Clean up immediately to avoid accumulation
        Composition.Remove(subject);
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
    public IComposition<TestSubject> Lookup_Core_DirectAccess()
    {
        // Core pattern: Direct composition reference (zero overhead)
        return _coreComposition;
    }

    [Benchmark(Description = "Registry: Global FindOrDefault")]
    [BenchmarkCategory("Lookup", "Registry")]
    public IComposition<TestSubject>? Lookup_Registry_FindOrDefault()
    {
        // Registry pattern: Global lookup (convenience with overhead)
        return Composition.FindOrDefault(_registrySubject);
    }

    [Benchmark(Description = "Registry: TryFind pattern")]
    [BenchmarkCategory("Lookup", "Registry")]
    public bool Lookup_Registry_TryFind()
    {
        // Registry pattern: TryFind (slightly optimized)
        return Composition.TryFind(_registrySubject, out _);
    }



    private static IComposition<TestSubject> CreateCoreComposition(int subjects, int capabilitiesPerSubject)
    {
        // Core-only: Build without registry registration (fastest)
        var subject = new TestSubject(0, "CoreSubject");
        var composer = Composer.For(subject);
        
        for (int c = 0; c < capabilitiesPerSubject; c++)
        {
            var capability = CreateCapability(0, c);
            composer.Add(capability);
        }
        
        return composer.Build();
    }

    private static IComposition<TestSubject> CreateRegistryComposition(TestSubject subject, int capabilitiesPerSubject)
    {
        // Registry-enabled: Build and register globally (convenient)
        var composer = Composer.For(subject);
        
        for (int c = 0; c < capabilitiesPerSubject; c++)
        {
            var capability = CreateCapability(subject.Id, c);
            composer.Add(capability);
        }
        
        return composer.BuildAndRegister();
    }
    
    private static ICapability<TestSubject> CreateCapability(int subjectId, int capabilityId)
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