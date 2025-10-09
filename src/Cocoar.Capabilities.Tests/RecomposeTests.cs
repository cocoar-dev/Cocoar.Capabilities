using Xunit;

namespace Cocoar.Capabilities.Tests;

public class RecomposeTests
{
    [Fact]
    public void Recompose_RegistryDisabled_NoComposerRegistration()
    {
        using var scope = new CapabilityScope(new CapabilityScopeOptions { UseComposerRegistry = false, UseCompositionRegistry = false });
        var subject = new StringSubject("r1");
        var baseComp = scope.For(subject).Add(new TestCapability("A")).Build(useRegistry: false);
        Assert.Null(scope.Composers.GetOrDefault(subject));
        Assert.Null(scope.Compositions.GetOrDefault(subject));

        var recomposer = scope.Recompose(baseComp, useRegistry: false);
        recomposer.Add(new TestCapability("B"));
        Assert.Null(scope.Composers.GetOrDefault(subject));
        var newComp = recomposer.Build(useRegistry: false);
        Assert.Null(scope.Composers.GetOrDefault(subject));
        Assert.Null(scope.Compositions.GetOrDefault(subject));
        Assert.Equal(2, newComp.GetAll<TestCapability>().Count);
    }

    [Fact]
    public void Recompose_ComposerEnabled_CompositionDisabled_ComposerRemovedAfterBuild()
    {
        using var scope = new CapabilityScope(new CapabilityScopeOptions { UseComposerRegistry = true, UseCompositionRegistry = false });
        var subject = new StringSubject("r2");
    var baseComp = scope.For(subject).Add(new TestCapability("A")).Build(useRegistry: false); // composition explicitly disabled
    // Composer registry true, composition disabled => composer removed, no composition stored
    Assert.Null(scope.Compositions.GetOrDefault(subject));
        Assert.Null(scope.Composers.GetOrDefault(subject));

        var recomposer = scope.Recompose(baseComp); // default composer reg true
        Assert.NotNull(scope.Composers.GetOrDefault(subject));
        recomposer.Add(new TestCapability("B"));
        var newComp = recomposer.Build(useRegistry: false); // disable composition registry explicitly
        Assert.Null(scope.Compositions.GetOrDefault(subject));
        Assert.Null(scope.Composers.GetOrDefault(subject));
        Assert.Equal(2, newComp.GetAll<TestCapability>().Count);
    }

    [Fact]
    public void Recompose_BothEnabled_TransitionOccurs()
    {
        using var scope = new CapabilityScope(new CapabilityScopeOptions { UseComposerRegistry = true, UseCompositionRegistry = true });
        var subject = new StringSubject("r3");
        var baseComp = scope.For(subject).Add(new TestCapability("A")).Build();
        var stored = scope.Compositions.GetOrDefault(subject);
        Assert.NotNull(stored);
        Assert.Same(baseComp, stored);

        var recomposer = scope.Recompose(baseComp); // composer registered
        Assert.NotNull(scope.Composers.GetOrDefault(subject));
        recomposer.Add(new TestCapability("B"));
        var newComp = recomposer.Build(); // transition
        Assert.Null(scope.Composers.GetOrDefault(subject));
        var storedAfter = scope.Compositions.GetOrDefault(subject);
        Assert.NotNull(storedAfter);
        Assert.Same(newComp, storedAfter);
        Assert.Equal(2, newComp.GetAll<TestCapability>().Count);
    }
}
