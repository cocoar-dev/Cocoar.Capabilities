using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Cocoar.Capabilities;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Cocoar.Capabilities.Benchmarks;

/// <summary>
/// Essential, readable benchmarks for Cocoar.Capabilities.
///
/// This curated suite answers a few concrete questions:
/// - Build cost: How expensive is creating a bag at our typical size (~20) vs a larger scale?
/// - Single-tag lookup: What does a common lookup cost on tiny vs large bags?
/// - Intersection: What does a multi-tag query cost on tiny vs large bags?
/// - Environment context: What runtime/hardware is being measured?
///
/// Notes:
/// - IndexMode is parameterized (None, Auto, Eager) so each chart shows the strategy impact.
/// - Only two bag sizes are exercised for lookups: 20 (typical) and 10,000 (stress).
/// - Keep the total number of benchmarks small and meaningful.
///
/// Run (short): dotnet run -c Release -- --job short --anyCategories Lookup|Build|Summary
/// </summary>
[SimpleJob]
[MemoryDiagnoser]
[MarkdownExporterAttribute.GitHub]
[HtmlExporter]
[CategoriesColumn]
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Benchmark names optimized for readability and filter matching")]
public class TaggedCapabilityBenchmarks
{
    // Test data and setup
    [Params(TagIndexingMode.None, TagIndexingMode.Auto, TagIndexingMode.Eager)]
    public TagIndexingMode IndexMode { get; set; }

    private ICapabilityBag<BenchmarkTestSubject> _tinyBag = null!;    // 20 capabilities (typical scale)
    private ICapabilityBag<BenchmarkTestSubject> _largeBag = null!;   // 10,000 capabilities (stress scale)

    // Tag instances for benchmarking
    private readonly string _stringTag = "Performance";
    // Minimal tag set for essential scenarios
    
    // Mixed tags for intersection tests
    private readonly object[] _mixedTags = ["Tag1", DIOperations.Configuration, typeof(LibraryB)];
    private readonly Consumer _consumer = new();

    [GlobalSetup]
    public void Setup()
    {
        var options = new CapabilityBagBuildOptions
        {
            TagIndexing = IndexMode,
            IndexMinFrequency = 2,
            AutoIndexThreshold = 64
        };

        // Tiny bag - 20 capabilities (matches Cocoar.Configuration scale)
        _tinyBag = CreateBag(20, options);
        // Large bag - 10,000 capabilities (stress)
        _largeBag = CreateBag(10000, options);
    }

    private static ICapabilityBag<BenchmarkTestSubject> CreateBag(int count, CapabilityBagBuildOptions options)
    {
        var builder = Composer.For(new BenchmarkTestSubject());
        
        for (int i = 0; i < count / 5; i++)
        {
            builder.Add(new StringTagCapability($"String_{i}"));
            builder.Add(new EnumTagCapability($"Enum_{i}"));
            builder.Add(new TypeTagCapability($"Type_{i}"));
            builder.Add(new MixedTagCapability($"Mixed_{i}"));
            builder.Add(new HeavyTagCapability($"Heavy_{i}"));
        }
        return builder.Build(options);
    }

    // ===== Essential Benchmarks =====

    // Build cost at typical scale
    [Benchmark(Description = "Build_Tiny_CreateBag_20")]
    [BenchmarkCategory("Build")]
    public ICapabilityBag<BenchmarkTestSubject> Build_Tiny_CreateBag_20()
    {
        var options = new CapabilityBagBuildOptions { TagIndexing = IndexMode, IndexMinFrequency = 2, AutoIndexThreshold = 64 };
        return CreateBag(20, options);
    }

    // Build cost at moderate/large scale (kept to 1000 to keep run time down but still show scaling)
    [Benchmark(Description = "Build_Large_CreateBag_1000")]
    [BenchmarkCategory("Build")]
    public ICapabilityBag<BenchmarkTestSubject> Build_Large_CreateBag_1000()
    {
        var options = new CapabilityBagBuildOptions { TagIndexing = IndexMode, IndexMinFrequency = 2, AutoIndexThreshold = 64 };
        return CreateBag(1000, options);
    }

    // Single-tag lookup (count-only) at tiny scale
    [Benchmark(Description = "Lookup_Tiny_SingleTag_CountOnly")]
    [BenchmarkCategory("Lookup", "Tiny")]
    public int Lookup_Tiny_SingleTag_CountOnly()
    {
        var items = _tinyBag.GetAllByTag<StringTagCapability>(_stringTag);
        int count = 0; foreach (var _ in items) count++; return count;
    }

    // Single-tag lookup (count-only) at large scale
    [Benchmark(Description = "Lookup_Large_SingleTag_CountOnly")]
    [BenchmarkCategory("Lookup", "Large")]
    public int Lookup_Large_SingleTag_CountOnly()
    {
        var items = _largeBag.GetAllByTag<StringTagCapability>(_stringTag);
        int count = 0; foreach (var _ in items) count++; return count;
    }

    // Multi-tag intersection (count-only) at tiny scale
    [Benchmark(Description = "Intersection_Tiny_CountOnly")]
    [BenchmarkCategory("Lookup", "Intersection", "Tiny")]
    public int Intersection_Tiny_CountOnly()
    {
        var items = _tinyBag.GetAllByTags<MixedTagCapability>(_mixedTags);
        int count = 0; foreach (var _ in items) count++; return count;
    }

    // Multi-tag intersection (count-only) at large scale
    [Benchmark(Description = "Intersection_Large_CountOnly")]
    [BenchmarkCategory("Lookup", "Intersection", "Large")]
    public int Intersection_Large_CountOnly()
    {
        var items = _largeBag.GetAllByTags<MixedTagCapability>(_mixedTags);
        int count = 0; foreach (var _ in items) count++; return count;
    }

    // Environment info row removed: BenchmarkDotNet already includes a comprehensive environment header.
}

// ===== Test Capability Classes for Benchmarking =====

public class BenchmarkTestSubject;

public enum DIOperations
{
    Registration,
    Configuration,
    Validation
}

public class LibraryA;
public class LibraryB;

public class StringTagCapability(string name) : ITaggedCapability<BenchmarkTestSubject>
{
    public string Name { get; } = name;
    public IReadOnlyCollection<object> Tags { get; } = ["StringTag", "Performance", $"String_{name}"];
    public int Order => 0;
}

// Keep only the minimal capability types necessary for the essential scenarios
public class EnumTagCapability(string name) : ITaggedCapability<BenchmarkTestSubject>
{
    public string Name { get; } = name;
    public IReadOnlyCollection<object> Tags { get; } = [DIOperations.Registration, DIOperations.Configuration, $"Enum_{name}"];
    public int Order => 1;
}

public class TypeTagCapability(string name) : ITaggedCapability<BenchmarkTestSubject>
{
    public string Name { get; } = name;
    public IReadOnlyCollection<object> Tags { get; } = [typeof(LibraryA), typeof(LibraryB), $"Type_{name}"];
    public int Order => 2;
}

public class MixedTagCapability(string name) : ITaggedCapability<BenchmarkTestSubject>
{
    public string Name { get; } = name;
    public IReadOnlyCollection<object> Tags { get; } = ["Tag1", DIOperations.Configuration, typeof(LibraryB), 100, $"Mixed_{name}"];
    public int Order => 3;
}

public class HeavyTagCapability(string name) : ITaggedCapability<BenchmarkTestSubject>
{
    public string Name { get; } = name;
    public IReadOnlyCollection<object> Tags { get; } = 
    [
        "Tag1", "Tag2", "Tag3", "HeavyTag", 
        42, 100, 200, 
        typeof(LibraryA), typeof(BenchmarkTestSubject),
        DIOperations.Registration, DIOperations.Validation,
        Assembly.GetExecutingAssembly(),
        $"Heavy_{name}"
    ];
    public int Order => 4;
}