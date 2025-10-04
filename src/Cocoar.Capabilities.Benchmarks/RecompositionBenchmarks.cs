using BenchmarkDotNet.Attributes;

namespace Cocoar.Capabilities.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class RecompositionBenchmarks : IDisposable
{
    public record TestSubject(int Id, string Name);
    public record Cap(string Name) : ICapability<TestSubject>;
    public record Primary(string Name) : IPrimaryCapability<TestSubject>;

    private CapabilityScope _scope = null!;
    private IComposition<TestSubject> _baseComposition = null!;
    private TestSubject _subject = null!;

    [GlobalSetup]
    public void Setup()
    {
        _scope = new CapabilityScope();
        _subject = new TestSubject(1, "RecomposeSubject");
        var composer = _scope.For(_subject);
        for (int i = 0; i < 50; i++) composer.Add(new Cap($"C{i}"));
        composer.WithPrimary(new Primary("P0"));
        _baseComposition = composer.Build(useRegistry: true);
    }

    [Benchmark(Description = "Recompose: no changes")] 
    public IComposition<TestSubject> Recompose_NoChange()
    {
        var composer = _scope.Recompose(_baseComposition, useRegistry: true);
        return composer.Build(useRegistry: true); // identity preserved
    }

    [Benchmark(Description = "Recompose: add capability")] 
    public IComposition<TestSubject> Recompose_AddCapability()
    {
        var composer = _scope.Recompose(_baseComposition, useRegistry: true);
        composer.Add(new Cap("NewCap"));
        return composer.Build(useRegistry: true);
    }

    [Benchmark(Description = "Recompose: replace primary")] 
    public IComposition<TestSubject> Recompose_ReplacePrimary()
    {
        var composer = _scope.Recompose(_baseComposition, useRegistry: true);
        composer.WithPrimary(new Primary("P1"));
        return composer.Build(useRegistry: true);
    }

    public void Dispose()
    {
        _scope?.Dispose();
        GC.SuppressFinalize(this);
    }
}