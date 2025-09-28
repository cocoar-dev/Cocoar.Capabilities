using Xunit;

namespace Cocoar.Capabilities.Tests;

public class StringSubjectValueSemanticsTests
{
    private record StringCapability(string Label) ;
    [Fact]
    public void DistinctEqualStringInstances_MapToSameComposition_WhenRegistered()
    {
        using var scope = new CapabilityScope(new CapabilityScopeOptions { UseComposerRegistry = false, UseCompositionRegistry = true });

        var s1 = new string("badword".ToCharArray()); // force new instance
        var s2 = new string("badword".ToCharArray()); // different instance, same contents

        var comp = scope.For(s1, useRegistry: true)
            .Add(new StringCapability("X"))
            .Build(useRegistry: true);

        Assert.True(scope.Compositions.TryGet(s2, out var found));
        Assert.Same(comp, found); // value-like semantics now
    }

    [Fact]
    public void Remove_UsingDifferentEqualInstance_RemovesComposition()
    {
        using var scope = new CapabilityScope(new CapabilityScopeOptions { UseComposerRegistry = false, UseCompositionRegistry = true });
        var s1 = new string("topic".ToCharArray());
        var s2 = new string("topic".ToCharArray());

        scope.For(s1, useRegistry: true)
            .Add(new StringCapability("T"))
            .Build(useRegistry: true);

        Assert.True(scope.Compositions.TryGet(s2, out _));
        Assert.True(scope.Compositions.Remove(s2));
        Assert.False(scope.Compositions.TryGet(s1, out _));
    }
}
