using System.Reflection;

namespace Cocoar.Capabilities.Tests;

public class CapabilityEntryTests
{
    private sealed record Subject(int Id);
    private sealed record Cap(string Name) : ICapability<Subject>;
    private sealed record Primary(string Name) : IPrimaryCapability<Subject>;

    [Fact]
    public void Registry_ObjectApis_SucceedForExisting()
    {
        using var scope = new CapabilityScope();
        var subject = new Subject(1);
        var comp = scope.For(subject).Add(new Cap("A")).WithPrimary(new Primary("P"))
            .Build(useRegistry: true);

        // Use object-based registry path (exercises CapabilityEntry.TryGetComposition(object&))
        Assert.True(scope.Compositions.TryFind(subject, out var typed));
        Assert.Same(comp, typed);

        object boxedSubject = subject;
        Assert.True(scope.Compositions.TryFind(boxedSubject, out IComposition boxedComp));
        Assert.Same(comp, boxedComp);
    }

    [Fact]
    public void Registry_ObjectApis_ReturnFalseWhenMissing()
    {
        using var scope = new CapabilityScope();
        object missing = new Subject(999);
        Assert.False(scope.Compositions.TryFind(missing, out IComposition? _));
    }

    private sealed class DisposableComposerCap<T> : ICapability<T>, IDisposable where T : notnull
    {
        public bool Disposed; public void Dispose() => Disposed = true;
    }

    [Fact]
    public void ScopeDispose_RegistryAccessThrows()
    {
        var scope = new CapabilityScope();
        var subject = new Subject(5);
        var composer = scope.For(subject).Add(new Cap("A"));
        var comp = composer.Build(useRegistry: true);
        // Recompose so registry entry transitions (ensures entry holds composition after composer removal/transition lifecycle)
        var recomposer = scope.Recompose(comp);
        recomposer.Build(useRegistry: true);
        scope.Dispose();
        Assert.Throws<ObjectDisposedException>(() => scope.Compositions.TryFind(subject, out _));
    }
}
