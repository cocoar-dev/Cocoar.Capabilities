using Xunit;

namespace Cocoar.Capabilities.Tests;

public class OwnerAndAnchorExtensionTests
{
    private class TestOwner
    {
        public string Name { get; init; } = string.Empty;
    }

    private class TestAnchor
    {
        public int Value { get; init; }
    }

    private record TestCapability(string Data);

    #region ComposeOwner Tests

    [Fact]
    public void ComposeOwner_ReturnsComposerForOwner()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner { Name = "Owner1" };
        scope.Owner.Set(owner);

        var composer = scope.Owner.Compose();

        Assert.NotNull(composer);

        // Verify we can build a composition
        var composition = composer.Add(new TestCapability("test")).Build();
        Assert.NotNull(composition);
        Assert.Single(composition.GetAll<TestCapability>());
    }

    [Fact]
    public void ComposeOwner_ThrowsWhenNoOwnerSet()
    {
        using var scope = new CapabilityScope();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Owner.Compose());
        Assert.Contains("No owner has been set", ex.Message);
    }

    [Fact]
    public void ComposeOwner_Generic_ReturnsComposerForTypedOwner()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner { Name = "TypedOwner" };
        scope.Owner.Set(owner);

        var composer = scope.Owner.Compose<TestOwner>();

        Assert.NotNull(composer);

        // Verify composition works
        var composition = composer.Add(new TestCapability("typed")).Build();
        Assert.NotNull(composition);
        Assert.Single(composition.GetAll<TestCapability>());
    }

    [Fact]
    public void ComposeOwner_Generic_ThrowsWhenNoOwnerSet()
    {
        using var scope = new CapabilityScope();

        Assert.Throws<InvalidOperationException>(() => scope.Owner.Compose<TestOwner>());
    }

    [Fact]
    public void ComposeOwner_Generic_ThrowsWhenWrongType()
    {
        using var scope = new CapabilityScope();
        scope.Owner.Set(new TestOwner());

        Assert.Throws<InvalidCastException>(() => scope.Owner.Compose<TestAnchor>());
    }

    [Fact]
    public void ComposeOwner_WithUseRegistry_PassesParameterThrough()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner { Name = "RegistryTest" };
        scope.Owner.Set(owner);

        var composer = scope.Owner.Compose(useRegistry: false);
        var composition = composer.Add(new TestCapability("reg-test")).Build(useRegistry: false);

        // Verify composition was not registered
        var found = scope.Compositions.GetOrDefault(owner);
        Assert.Null(found);
    }

    #endregion

    #region ComposeAnchor Typed Tests

    [Fact]
    public void ComposeAnchor_Generic_ReturnsComposerForAnchor()
    {
        using var scope = new CapabilityScope();
        var anchor = new TestAnchor { Value = 42 };
        scope.Anchors.Set(anchor);

        var composer = scope.Anchors.Compose<TestAnchor>();

        Assert.NotNull(composer);

        // Verify composition works
        var composition = composer.Add(new TestCapability("anchor-test")).Build();
        Assert.NotNull(composition);
        Assert.Single(composition.GetAll<TestCapability>());
    }

    [Fact]
    public void ComposeAnchor_Generic_ThrowsWhenAnchorNotSet()
    {
        using var scope = new CapabilityScope();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Anchors.Compose<TestAnchor>());
        Assert.Contains("No anchor of type TestAnchor", ex.Message);
    }

    [Fact]
    public void ComposeAnchor_Generic_WithUseRegistry_PassesParameterThrough()
    {
        using var scope = new CapabilityScope();
        var anchor = new TestAnchor { Value = 100 };
        scope.Anchors.Set(anchor);

        var composer = scope.Anchors.Compose<TestAnchor>(useRegistry: false);
        var composition = composer.Add(new TestCapability("no-reg")).Build(useRegistry: false);

        // Verify composition was not registered
        var found = scope.Compositions.GetOrDefault(anchor);
        Assert.Null(found);
    }

    #endregion

    #region ComposeAnchor Named Tests

    [Fact]
    public void ComposeAnchor_Named_ReturnsComposerForAnchor()
    {
        using var scope = new CapabilityScope();
        var anchor = new TestAnchor { Value = 999 };
        scope.Anchors.Set("environment", anchor);

        var composer = scope.Anchors.Compose("environment");

        Assert.NotNull(composer);

        // Verify composition works
        var composition = composer.Add(new TestCapability("env-test")).Build();
        Assert.NotNull(composition);
        Assert.Single(composition.GetAll<TestCapability>());
    }

    [Fact]
    public void ComposeAnchor_Named_ThrowsWhenAnchorNotSet()
    {
        using var scope = new CapabilityScope();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Anchors.Compose("missing-key"));
        Assert.Contains("No anchor with key 'missing-key'", ex.Message);
    }

    [Fact]
    public void ComposeAnchor_Named_ThrowsOnNullKey()
    {
        using var scope = new CapabilityScope();

        Assert.Throws<ArgumentNullException>(() => scope.Anchors.Compose(null!));
    }

    [Fact]
    public void ComposeAnchor_Named_WithUseRegistry_PassesParameterThrough()
    {
        using var scope = new CapabilityScope();
        var anchor = new TestAnchor { Value = 777 };
        scope.Anchors.Set("pipeline", anchor);

        var composer = scope.Anchors.Compose("pipeline", useRegistry: false);
        var composition = composer.Add(new TestCapability("pipeline-test")).Build(useRegistry: false);

        // Verify composition was not registered
        var found = scope.Compositions.GetOrDefault(anchor);
        Assert.Null(found);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_OwnerAndAnchors_CanComposeSeparately()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner { Name = "MainOwner" };
        var anchor1 = new TestAnchor { Value = 1 };
        var anchor2 = new TestAnchor { Value = 2 };

        scope.Owner.Set(owner);
        scope.Anchors
            .Set(anchor1)
            .Set("secondary", anchor2);

        // Compose for owner
        var ownerComposition = scope.Owner.Compose()
            .Add(new TestCapability("owner-cap"))
            .Build();

        // Compose for typed anchor
        var anchor1Composition = scope.Anchors.Compose<TestAnchor>()
            .Add(new TestCapability("anchor1-cap"))
            .Build();

        // Compose for named anchor
        var anchor2Composition = scope.Anchors.Compose("secondary")
            .Add(new TestCapability("anchor2-cap"))
            .Build();

        // Verify all compositions are distinct
        Assert.NotSame(ownerComposition, anchor1Composition);
        Assert.NotSame(ownerComposition, anchor2Composition);
        Assert.NotSame(anchor1Composition, anchor2Composition);

        // Verify capabilities
        Assert.Equal("owner-cap", ownerComposition.GetAll<TestCapability>()[0].Data);
        Assert.Equal("anchor1-cap", anchor1Composition.GetAll<TestCapability>()[0].Data);
        Assert.Equal("anchor2-cap", anchor2Composition.GetAll<TestCapability>()[0].Data);
    }

    [Fact]
    public void Integration_MultipleCapabilitiesForOwner()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner { Name = "MultiCapOwner" };
        scope.Owner.Set(owner);

        var composition = scope.Owner.Compose()
            .Add(new TestCapability("cap1"))
            .Add(new TestCapability("cap2"))
            .Add(new TestCapability("cap3"))
            .Build();

        var capabilities = composition.GetAll<TestCapability>();
        Assert.Equal(3, capabilities.Count);
        Assert.Contains(capabilities, c => c.Data == "cap1");
        Assert.Contains(capabilities, c => c.Data == "cap2");
        Assert.Contains(capabilities, c => c.Data == "cap3");
    }

    [Fact]
    public void Integration_RecomposingOwnerComposition()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner { Name = "RecomposeOwner" };
        scope.Owner.Set(owner);

        // Initial composition
        var composition1 = scope.Owner.Compose()
            .Add(new TestCapability("initial"))
            .Build();

        // Recompose
        var composition2 = scope.Recompose(composition1)
            .Add(new TestCapability("added"))
            .Build();

        var capabilities = composition2.GetAll<TestCapability>();
        Assert.Equal(2, capabilities.Count);
        Assert.Contains(capabilities, c => c.Data == "initial");
        Assert.Contains(capabilities, c => c.Data == "added");
    }

    [Fact]
    public void Integration_OwnerAndAnchorsSameObject()
    {
        using var scope = new CapabilityScope();
        var obj = new TestOwner { Name = "SharedObject" };

        // Set same object as owner and typed anchor
        scope.Owner.Set(obj)
            .Scope
            .Anchors.Set(obj);

        // Both should return composers for the same subject
        var ownerComposer = scope.Owner.Compose<TestOwner>();
        var anchorComposer = scope.Anchors.Compose<TestOwner>();

        // Build separate compositions
        var comp1 = ownerComposer.Add(new TestCapability("from-owner")).Build();
        var comp2 = anchorComposer.Add(new TestCapability("from-anchor")).Build();

        // They should be different compositions but for same subject
        Assert.NotSame(comp1, comp2);

        // But the subject should be the same
        Assert.Same(comp1.Subject, comp2.Subject);
    }

    #endregion
}
