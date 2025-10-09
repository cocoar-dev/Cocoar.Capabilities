using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;

namespace Cocoar.Capabilities.Benchmarks;

/// <summary>
/// Benchmarks the incremental overhead introduced when at least one capability implements IOrderedCapability.
/// Key goals:
/// 1. Measure build-time delta between unordered vs ordered capability sets.
/// 2. Capture effect of initial ordering complexity (random, already sorted, reverse, duplicated order values).
/// 3. Differentiate fresh build vs recomposition (identity-preserving) cost.
///
/// NOTE: Only the first build (or recomposition) for a capability set pays the ordering cost. Lookups / enumeration afterwards reuse the pre-sorted array.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class OrderingBenchmarks : IDisposable
{
    public record Subject(int Id, string Name);

    public interface ITestCap  { }
    
    public interface IOrderedCapability
    {
        int Order { get; }
    }

    public sealed record PlainCap(string Name) : ITestCap; // Unordered

    public sealed record OrderedCap(string Name, int Priority) : ITestCap, IOrderedCapability
    {
        public int Order => Priority;
    }

    private Subject _subject = null!;
    private CapabilityScope _scope = null!;

    // Cached compositions for recomposition measurements
    private IComposition _unorderedBase = null!;
    private IComposition _orderedRandomBase = null!;

    [Params(50, 200, 500)]
    public int Count { get; set; }

    private int[] _randomOrder = Array.Empty<int>();
    private int[] _reverseOrder = Array.Empty<int>();
    private int[] _duplicateOrder = Array.Empty<int>();

    [GlobalSetup]
    public void Setup()
    {
        _subject = new Subject(1, "OrderedBenchSubject");
        _scope = new CapabilityScope(new CapabilityScopeOptions { UseComposerRegistry = false, UseCompositionRegistry = false });

        var rnd = new Random(17);
        _randomOrder = Enumerable.Range(0, Count).Select(_ => rnd.Next(0, Count)).ToArray();
        _reverseOrder = Enumerable.Range(0, Count).Reverse().ToArray();
        // Duplicate order groups of 5 (stress stable secondary ordering path)
        _duplicateOrder = Enumerable.Range(0, Count).Select(i => i / 5).ToArray();

        // Seed base compositions for recomposition tests (unordered + random ordered)
        _unorderedBase = BuildUnorderedInternal();
        _orderedRandomBase = BuildOrderedRandomInternal();
    }

    #region Build (Fresh)

    [Benchmark(Description = "Build: Unordered")] 
    public IComposition Build_Unordered() => BuildUnorderedInternal();

    [Benchmark(Description = "Build: Ordered (Random)")] 
    public IComposition Build_Ordered_Random() => BuildOrderedRandomInternal();

    [Benchmark(Description = "Build: Ordered (Already Sorted)")] 
    public IComposition Build_Ordered_AlreadySorted()
    {
        var composer = _scope.For(new Subject(2, "Sorted"));
        for (int i = 0; i < Count; i++)
        {
            composer.Add(new OrderedCap($"C{i}", i));
        }
        return composer.Build();
    }

    [Benchmark(Description = "Build: Ordered (Reverse -> Worst Case)")] 
    public IComposition Build_Ordered_Reverse()
    {
        var composer = _scope.For(new Subject(3, "Reverse"));
        for (int i = 0; i < Count; i++)
        {
            composer.Add(new OrderedCap($"C{i}", _reverseOrder[i]));
        }
        return composer.Build();
    }

    [Benchmark(Description = "Build: Ordered (Duplicate Priorities)")] 
    public IComposition Build_Ordered_Duplicates()
    {
        var composer = _scope.For(new Subject(4, "Duplicates"));
        for (int i = 0; i < Count; i++)
        {
            composer.Add(new OrderedCap($"C{i}", _duplicateOrder[i]));
        }
        return composer.Build();
    }

    #endregion

    #region Recompose (In-place Update)

    [Benchmark(Description = "Recompose: Unordered (No Change)")] 
    public IComposition Recompose_Unordered_NoChange()
    {
        var composer = _scope.Recompose(_unorderedBase);
        return composer.Build(); // identity preserved
    }

    [Benchmark(Description = "Recompose: Ordered Random (No Change)")] 
    public IComposition Recompose_Ordered_NoChange()
    {
        var composer = _scope.Recompose(_orderedRandomBase);
        return composer.Build();
    }

    [Benchmark(Description = "Recompose: Ordered Add One")] 
    public IComposition Recompose_Ordered_Add()
    {
        var composer = _scope.Recompose(_orderedRandomBase);
        composer.Add(new OrderedCap("NewLast", Count + 10));
        return composer.Build();
    }

    #endregion

    #region Enumeration (GetAll)

    private IComposition _enumerationOrdered = null!;
    private IComposition _enumerationUnordered = null!;

    [IterationSetup(Targets = new[]{ nameof(Enumerate_All_Ordered), nameof(Enumerate_All_Unordered) })]
    public void IterationSetupEnumeration()
    {
        // Build once per iteration group to avoid JIT / GC interference across param sets
        _enumerationUnordered = BuildUnorderedInternal();
        _enumerationOrdered = BuildOrderedRandomInternal();
    }

    [Benchmark(Description = "Enumerate: Unordered (GetAll)")] 
    public int Enumerate_All_Unordered()
    {
        int sum = 0;
        foreach (var c in _enumerationUnordered.GetAll<ITestCap>())
        {
            // cheap side-effect to prevent elimination
            var obj = Unsafe.As<ITestCap, object?>(ref Unsafe.AsRef(in c));
            if (obj != null) sum += obj.GetHashCode();
        }
        return sum;
    }

    [Benchmark(Description = "Enumerate: Ordered (GetAll)")] 
    public int Enumerate_All_Ordered()
    {
        int sum = 0;
        foreach (var c in _enumerationOrdered.GetAll<ITestCap>())
        {
            var obj = Unsafe.As<ITestCap, object?>(ref Unsafe.AsRef(in c));
            if (obj != null) sum += obj.GetHashCode();
        }
        return sum;
    }

    #endregion

    private IComposition BuildUnorderedInternal()
    {
        var composer = _scope.For(new Subject(10, "Unordered"));
        for (int i = 0; i < Count; i++) composer.Add(new PlainCap($"C{i}"));
        return composer.Build();
    }

    private IComposition BuildOrderedRandomInternal()
    {
        var composer = _scope.For(new Subject(11, "OrderedRandom"));
        for (int i = 0; i < Count; i++) composer.Add(new OrderedCap($"C{i}", _randomOrder[i]));
        return composer.Build();
    }
        public void Dispose()
        {
            _scope.Dispose();
            GC.SuppressFinalize(this);
        }
}
