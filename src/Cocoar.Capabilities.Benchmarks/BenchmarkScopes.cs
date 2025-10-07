namespace Cocoar.Capabilities.Benchmarks;

public static class BenchmarkScopes
{
    public static CapabilityScope Shared { get; } = new CapabilityScope();
    public static CapabilityScope SharedLightweight { get; } = new CapabilityScope(new CapabilityScopeOptions
    {
        UseComposerRegistry = false,
        UseCompositionRegistry = false
    });
    public static CapabilityScope CreateDedicated() => new CapabilityScope();
    public static CapabilityScope CreateLightweight() => new CapabilityScope(new CapabilityScopeOptions
    {
        UseComposerRegistry = false,
        UseCompositionRegistry = false
    });
}
