using BenchmarkDotNet.Attributes;

namespace Cocoar.Capabilities.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class CanonicalizationBenchmarks : IDisposable
{
    public record struct ValueSubject(int Id);
    public record StringCapability(string Name) ;
    public record ValueCapability(string Name) ;

    private CapabilityScope _stringScope = null!;
    private CapabilityScope _valueScope = null!;
    private string[] _stringSubjects = null!;
    private ValueSubject[] _valueSubjects = null!;

    [Params(10, 100)] public int SubjectCount { get; set; }
    [Params(5, 20)] public int CapabilitiesPerSubject { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _stringScope = new CapabilityScope();
        _valueScope = new CapabilityScope();
        _stringSubjects = Enumerable.Range(0, 1000).Select(i => $"Subject_{i}").ToArray();
        _valueSubjects = Enumerable.Range(0, 1000).Select(i => new ValueSubject(i)).ToArray();
    }

    [Benchmark(Description = "Build (string subjects)")]
    public void Build_StringSubjects()
    {
        for (int i = 0; i < SubjectCount; i++)
        {
            var subject = _stringSubjects[i];
            var composer = _stringScope.For(subject);
            for (int c = 0; c < CapabilitiesPerSubject; c++) composer.Add(new StringCapability($"C{c}"));
            composer.Build(useRegistry: true);
        }
    }

    [Benchmark(Description = "Build (value subjects)")]
    public void Build_ValueSubjects()
    {
        for (int i = 0; i < SubjectCount; i++)
        {
            var subject = _valueSubjects[i];
            var composer = _valueScope.For(subject);
            for (int c = 0; c < CapabilitiesPerSubject; c++) composer.Add(new ValueCapability($"C{c}"));
            composer.Build(useRegistry: true);
        }
    }

    [Benchmark(Description = "Lookup (string subjects)")]
    public int Lookup_StringSubjects()
    {
        int total = 0;
        for (int i = 0; i < SubjectCount; i++)
        {
            _stringScope.Compositions.TryGet(_stringSubjects[i], out var comp);
            if (comp != null) total += comp.TotalCapabilityCount;
        }
        return total;
    }

    [Benchmark(Description = "Lookup (value subjects)")]
    public int Lookup_ValueSubjects()
    {
        int total = 0;
        for (int i = 0; i < SubjectCount; i++)
        {
            _valueScope.Compositions.TryGet(_valueSubjects[i], out var comp);
            if (comp != null) total += comp.TotalCapabilityCount;
        }
        return total;
    }

    public void Dispose()
    {
        _stringScope?.Dispose();
        _valueScope?.Dispose();
        GC.SuppressFinalize(this);
    }
}