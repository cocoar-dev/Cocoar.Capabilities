using Xunit;

namespace Cocoar.Capabilities.Tests;

public class ValueTypeRegistryTests
{
    private static CapabilityScope NewScope() => new(new CapabilityScopeOptions
    {
        UseComposerRegistry = false,
        UseCompositionRegistry = true
    });

    private record IntTestCapability(string Name) ;

    [Fact]
    public void ValueType_Compositions_AreRetrievable()
    {
        using var scope = NewScope();
        var c1 = scope.For(42, useRegistry: true)
            .Add(new IntTestCapability("answer"))
            .Build(useRegistry: true);
        var c2 = scope.For(7, useRegistry: true)
            .Add(new IntTestCapability("seven"))
            .Build(useRegistry: true);

        Assert.True(scope.Compositions.TryGet(42, out var found1));
        Assert.True(scope.Compositions.TryGet(7, out var found2));
        Assert.Same(c1, found1);
        Assert.Same(c2, found2);
    }

    [Fact]
    public void ValueType_Composition_Removal_Works()
    {
        using var scope = NewScope();
        scope.For(100, useRegistry: true).Add(new IntTestCapability("hundred")).Build(useRegistry: true);
        scope.For(200, useRegistry: true).Add(new IntTestCapability("two")) .Build(useRegistry: true);
        Assert.True(scope.Compositions.Remove(100));
        Assert.False(scope.Compositions.TryGet(100, out _));
        Assert.True(scope.Compositions.TryGet(200, out _));
    }

    [Fact]
    public void ValueType_NotRegistered_WhenUseRegistryFalse()
    {
        using var scope = NewScope();
        var comp = scope.For(5, useRegistry: false)
            .Add(new IntTestCapability("five"))
            .Build(useRegistry: false);
        Assert.False(scope.Compositions.TryGet(5, out _));
        Assert.NotNull(comp);
    }
}
