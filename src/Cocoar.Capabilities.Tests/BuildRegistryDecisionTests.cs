using Xunit;

namespace Cocoar.Capabilities.Tests;

public class BuildRegistryDecisionTests
{
    private static CapabilityScope CreateScope(bool composerReg, bool compositionReg)
        => new(new CapabilityScopeOptions { UseComposerRegistry = composerReg, UseCompositionRegistry = compositionReg });

    [Fact]
    public void Build_NoRegistries_NoOverrides_NoRegistryCalls()
    {
        using var scope = CreateScope(false, false);
        var subject = new StringSubject("s1");
        var comp = scope.Compose(subject).Add(new TestCapability("A")).Build();
        Assert.Null(scope.Composers.GetOrDefault(subject));
        Assert.Null(scope.Compositions.GetOrDefault(subject));
        Assert.Same(subject, comp.Subject);
    }

    [Fact]
    public void Build_ComposerDisabled_CompositionEnabledViaOverride_RegistersComposition()
    {
        using var scope = CreateScope(false, false);
        var subject = new StringSubject("s2");
        var comp = scope.Compose(subject, useRegistry: false) // composer override false (explicit)
                        .Add(new TestCapability("B"))
                        .Build(useRegistry: true); // composition override true
        Assert.Null(scope.Composers.GetOrDefault(subject)); // composer not registered
        var found = scope.Compositions.GetOrDefault(subject);
        Assert.NotNull(found);
        Assert.Same(comp, found);
        Assert.Same(subject, comp.Subject);
    }

    [Fact]
    public void Build_ComposerEnabled_CompositionDisabled_RemovesComposerDoesNotRegisterComposition()
    {
        using var scope = CreateScope(true, true); // defaults both true
        var subject = new StringSubject("s3");
        var builder = scope.Compose(subject, useRegistry: true) // composer registered
                           .Add(new TestCapability("C"));
        var comp = builder.Build(useRegistry: false); // disable composition
        Assert.Null(scope.Compositions.GetOrDefault(subject)); // composition not registered
        Assert.Null(scope.Composers.GetOrDefault(subject)); // composer removed
        Assert.Same(subject, comp.Subject);
    }

    [Fact]
    public void Build_ComposerEnabled_CompositionEnabled_Transitions()
    {
        using var scope = CreateScope(true, true);
        var subject = new StringSubject("s4");
        var comp = scope.Compose(subject) // default true -> composer registered
                        .Add(new TestCapability("D"))
                        .Build(); // default true -> transition
        Assert.Null(scope.Composers.GetOrDefault(subject)); // composer transitioned away
        var composition = scope.Compositions.GetOrDefault(subject);
        Assert.NotNull(composition);
        Assert.Same(comp, composition);
        Assert.Same(subject, comp.Subject);
    }

    [Fact]
    public void Build_ComposerDisabled_CompositionDefaultTrue_DirectRegistration()
    {
        using var scope = CreateScope(false, true);
        var subject = new StringSubject("s5");
        var comp = scope.Compose(subject, useRegistry: false)
                        .Add(new TestCapability("E"))
                        .Build(); // composition default true
        Assert.Null(scope.Composers.GetOrDefault(subject));
        var found = scope.Compositions.GetOrDefault(subject);
        Assert.NotNull(found);
        Assert.Same(comp, found);
        Assert.Same(subject, comp.Subject);
    }

    [Fact]
    public void Build_ComposerOnlyEnabled_RemovesComposer_NoComposition()
    {
        using var scope = CreateScope(composerReg: true, compositionReg: false);
        var subject = new StringSubject("s6");
        var composer = scope.Compose(subject); // composer registered
        composer.Add(new TestCapability("X"));
        Assert.NotNull(scope.Composers.GetOrDefault(subject)); // pre-build composer present
        var composition = composer.Build(); // composition registry disabled -> no registration
        Assert.Null(scope.Compositions.GetOrDefault(subject));
        Assert.Null(scope.Composers.GetOrDefault(subject)); // composer removed
        Assert.Same(subject, composition.Subject);
    }

    [Fact]
    public void Build_ComposerAndCompositionEnabled_ComposerVisibleBeforeBuild_TransitionedAfter()
    {
        using var scope = CreateScope(true, true);
        var subject = new StringSubject("s7");
        var composer = scope.Compose(subject); // composer registered
        composer.Add(new TestCapability("Y"));
        var composerPre = scope.Composers.GetOrDefault(subject);
        Assert.NotNull(composerPre);
        Assert.Same(composer, composerPre);
        var composition = composer.Build(); // transition
        Assert.Null(scope.Composers.GetOrDefault(subject));
        var compositionPost = scope.Compositions.GetOrDefault(subject);
        Assert.NotNull(compositionPost);
        Assert.Same(composition, compositionPost);
        Assert.Same(subject, composition.Subject);
    }
}
