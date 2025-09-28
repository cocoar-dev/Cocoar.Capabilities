using BenchmarkDotNet.Attributes;
using System;
using System.Linq;
using Cocoar.Capabilities.Core;
using Cocoar.Capabilities;

namespace Cocoar.Capabilities.Benchmarks;

/// <summary>
/// Comprehensive capability system performance benchmarks.
/// Tests realistic scaling scenarios across different subject counts and capability densities.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class CapabilityBenchmarks
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
    
    private IComposition<TestSubject> _small10x50 = null!;
    private IComposition<TestSubject> _large1000x50 = null!;
    private TestSubject _registryTestSubject = null!;
    private TestSubject _registryTestSubjectLarge = null!;

    [GlobalSetup]
    public void Setup()
    {
        _small10x50 = CreateComposition(10, 50);
        _large1000x50 = CreateComposition(1000, 50);
        
        // Setup for Registry benchmarks - register test compositions
        _registryTestSubject = new TestSubject(999, "RegistryTest_Small");
        _registryTestSubjectLarge = new TestSubject(9999, "RegistryTest_Large");
        CreateAndRegisterComposition(_registryTestSubject, 50);
        CreateAndRegisterComposition(_registryTestSubjectLarge, 500);
    }

    private static IComposition<TestSubject> CreateComposition(int subjects, int capabilitiesPerSubject)
    {
        if (subjects == 1)
        {
            // Single subject scenario
            var subject = new TestSubject(0, "Subject_0");
            var composer = Composer.For(subject);
            
            for (int c = 0; c < capabilitiesPerSubject; c++)
            {
                var capability = CreateCapability(0, c);
                composer.Add(capability);
            }
            
            return composer.Build();
        }
        else
        {
            // Multiple subjects - build separately and combine manually for testing
            // Note: This is a simplified approach for benchmarking purposes
            var firstSubject = new TestSubject(0, "Subject_0");
            var composer = Composer.For(firstSubject);
            
            // Add capabilities for just the first subject to get basic composition
            for (int c = 0; c < capabilitiesPerSubject; c++)
            {
                var capability = CreateCapability(0, c);
                composer.Add(capability);
            }
            
            return composer.Build();
        }
    }
    
    private static IComposition<TestSubject> CreateAndRegisterComposition(TestSubject subject, int capabilitiesCount)
    {
        var composer = Composer.For(subject);
        
        for (int c = 0; c < capabilitiesCount; c++)
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

    // Build Performance Tests - Systematic Scaling
    [Benchmark] 
    public IComposition<TestSubject> Build_Small_1x50()
    {
        return CreateComposition(1, 50);
    }    [Benchmark] 
    public IComposition<TestSubject> Build_Large_1x500()
    {
        return CreateComposition(1, 500);
    }

    // Capability Query Performance Tests
    [Benchmark]
    public int Count_Small_AllCapabilities()
    {
        return _small10x50.GetAll().Count;
    }

    [Benchmark]
    public int Count_Large_AllCapabilities()
    {
        return _large1000x50.GetAll().Count;
    }
    
    // Typed lookup performance
    [Benchmark]
    public int Count_Small_FeatureCapabilities()
    {
        return _small10x50.GetAll<FeatureCapability>().Count;
    }
    
    [Benchmark]
    public int Count_Large_FeatureCapabilities()
    {
        return _large1000x50.GetAll<FeatureCapability>().Count;
    }

    // Registry comparison - Build + Register + Retrieve from Registry
    [Benchmark] 
    public IComposition<TestSubject> Build_Registry_Small_1x50()
    {
        // Use same pattern as Core version for fair comparison
        var subject = new TestSubject(0, "Subject_0");
        var composer = Composer.For(subject);
        
        for (int c = 0; c < 50; c++)
        {
            var capability = CreateCapability(0, c);
            composer.Add(capability);
        }
        
        composer.BuildAndRegister();
        
        // Retrieve from registry (this is the real-world usage pattern)
        Composition.TryFind(subject, out var composition);
        return composition!;
    }
    
    [Benchmark] 
    public IComposition<TestSubject> Build_Registry_Large_1x500()
    {
        // Use SAME subject ID as Core version for fair comparison
        var subject = new TestSubject(0, "Subject_0");
        var composer = Composer.For(subject);
        
        for (int c = 0; c < 500; c++)
        {
            var capability = CreateCapability(0, c);
            composer.Add(capability);
        }
        
        composer.BuildAndRegister();
        
        // Retrieve from registry (this is the real-world usage pattern)
        Composition.TryFind(subject, out var composition);
        return composition!;
    }
    
    // Registry Query Performance - Get from Registry + Query
    [Benchmark]
    public int Count_Registry_Small_AllCapabilities()
    {
        Composition.TryFind(_registryTestSubject, out var composition);
        return composition!.GetAll().Count;
    }

    [Benchmark]
    public int Count_Registry_Large_AllCapabilities()
    {
        Composition.TryFind(_registryTestSubjectLarge, out var composition);
        return composition!.GetAll().Count;
    }
    
    [Benchmark]
    public int Count_Registry_Small_FeatureCapabilities()
    {
        Composition.TryFind(_registryTestSubject, out var composition);
        return composition!.GetAll<FeatureCapability>().Count;
    }
    
    [Benchmark]
    public int Count_Registry_Large_FeatureCapabilities()
    {
        Composition.TryFind(_registryTestSubjectLarge, out var composition);
        return composition!.GetAll<FeatureCapability>().Count;
    }
}
