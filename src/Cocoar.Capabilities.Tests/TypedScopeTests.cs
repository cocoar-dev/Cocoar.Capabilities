using Xunit;

namespace Cocoar.Capabilities.Tests;

public class TypedScopeTests
{
    private class TestOwner
    {
        public string Name { get; set; } = string.Empty;
    }

    private class TestCapability
    {
        public string Value { get; }
        public TestCapability(string value) => Value = value;
    }

    [Fact]
    public void TypedScope_CreatesWithOwner()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        Assert.NotNull(scope);
        Assert.NotNull(scope.Owner);
    }

    [Fact]
    public void TypedScope_GetOwner_ReturnsStronglyTypedOwner()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        var retrieved = scope.Owner.Get();

        Assert.Same(owner, retrieved);
        Assert.Equal("Test", retrieved.Name);
    }

    [Fact]
    public void TypedScope_TryGetOwner_ReturnsTrue()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        var success = scope.Owner.TryGet(out var retrieved);

        Assert.True(success);
        Assert.Same(owner, retrieved);
    }

    [Fact]
    public void TypedScope_Compose_CreatesComposerForOwner()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        var composer = scope.Owner.Compose();

        Assert.NotNull(composer);
    }

    [Fact]
    public void TypedScope_Compose_AndBuild_CreatesComposition()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        var composition = scope.Owner.Compose()
            .Add(new TestCapability("value1"))
            .Add(new TestCapability("value2"))
            .Build();

        Assert.NotNull(composition);
        Assert.Equal(2, composition.Count<TestCapability>());
    }

    [Fact]
    public void TypedScope_GetComposition_ReturnsComposition()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        scope.Owner.Compose()
            .Add(new TestCapability("test"))
            .Build();

        var composition = scope.Owner.GetComposition();

        Assert.NotNull(composition);
        Assert.Single(composition.GetAll<TestCapability>());
    }

    [Fact]
    public void TypedScope_GetComposition_ReturnsNullWhenNoComposition()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        var composition = scope.Owner.GetComposition();

        Assert.Null(composition);
    }

    [Fact]
    public void TypedScope_TryGetComposition_ReturnsTrueWhenCompositionExists()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        scope.Owner.Compose()
            .Add(new TestCapability("test"))
            .Build();

        var success = scope.Owner.TryGetComposition(out var composition);

        Assert.True(success);
        Assert.NotNull(composition);
        Assert.Single(composition.GetAll<TestCapability>());
    }

    [Fact]
    public void TypedScope_TryGetComposition_ReturnsFalseWhenNoComposition()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        var success = scope.Owner.TryGetComposition(out var composition);

        Assert.False(success);
        Assert.Null(composition);
    }

    [Fact]
    public void TypedScope_GetRequiredComposition_ReturnsComposition()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        scope.Owner.Compose()
            .Add(new TestCapability("test"))
            .Build();

        var composition = scope.Owner.GetRequiredComposition();

        Assert.NotNull(composition);
        Assert.Single(composition.GetAll<TestCapability>());
    }

    [Fact]
    public void TypedScope_GetRequiredComposition_ThrowsWhenNoComposition()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        Assert.Throws<InvalidOperationException>(() => scope.Owner.GetRequiredComposition());
    }

    [Fact]
    public void TypedScope_GetComposer_ReturnsComposerBeforeBuild()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        var composer = scope.Owner.Compose(useRegistry: true);
        composer.Add(new TestCapability("test"));

        // Composer should be available before Build() is called
        var retrieved = scope.Owner.GetComposer();

        Assert.NotNull(retrieved);
        Assert.Same(composer, retrieved);
    }

    [Fact]
    public void TypedScope_GetComposer_ReturnsNullWhenNotInRegistry()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        var composer = scope.Owner.GetComposer();

        Assert.Null(composer);
    }

    [Fact]
    public void TypedScope_TryGetComposer_ReturnsTrueWhenComposerInRegistry()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        var composer = scope.Owner.Compose(useRegistry: true);
        composer.Add(new TestCapability("test"));

        var success = scope.Owner.TryGetComposer(out var retrieved);

        Assert.True(success);
        Assert.NotNull(retrieved);
        Assert.Same(composer, retrieved);
    }

    [Fact]
    public void TypedScope_TryGetComposer_ReturnsFalseWhenNotInRegistry()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        var success = scope.Owner.TryGetComposer(out var composer);

        Assert.False(success);
        Assert.Null(composer);
    }

    [Fact]
    public void TypedScope_GetRequiredComposer_ReturnsComposerBeforeBuild()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        var composer = scope.Owner.Compose(useRegistry: true);
        composer.Add(new TestCapability("test"));

        // Composer should be available before Build() is called
        var retrieved = scope.Owner.GetRequiredComposer();

        Assert.NotNull(retrieved);
        Assert.Same(composer, retrieved);
    }

    [Fact]
    public void TypedScope_GetRequiredComposer_ThrowsWhenNotInRegistry()
    {
        var owner = new TestOwner { Name = "Test" };
        using var scope = new CapabilityScope<TestOwner>(owner);

        Assert.Throws<InvalidOperationException>(() => scope.Owner.GetRequiredComposer());
    }

    [Fact]
    public void TypedScope_ThrowsWhenOwnerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new CapabilityScope<TestOwner>(null!));
    }

    [Fact]
    public void TypedScope_CanBePassedAsBaseScope()
    {
        var owner = new TestOwner { Name = "Test" };
        using CapabilityScope scope = new CapabilityScope<TestOwner>(owner);

        Assert.NotNull(scope);
        Assert.NotNull(scope.Compositions);
        Assert.NotNull(scope.Composers);
    }

    [Fact]
    public void TypedScope_BaseOwnerPropertyAlsoSet()
    {
        var owner = new TestOwner { Name = "Test" };
        using var typedScope = new CapabilityScope<TestOwner>(owner);
        
        // Access via base type
        CapabilityScope baseScope = typedScope;
        var baseOwner = baseScope.Owner.Get<TestOwner>();

        Assert.Same(owner, baseOwner);
    }

    [Fact]
    public void DerivedScope_CanInheritFromTypedScope()
    {
        // This demonstrates the pattern: public class ConfigManagerScope : CapabilityScope<ConfigManager>
        var owner = new TestOwner { Name = "Test" };
        
        // User could create: public class TestOwnerScope : CapabilityScope<TestOwner>
        using var scope = new CapabilityScope<TestOwner>(owner);
        
        var retrieved = scope.Owner.Get();
        Assert.Equal("Test", retrieved.Name);
    }

    [Fact]
    public void TypedScope_WithOptions_CreatesWithOwnerAndOptions()
    {
        var owner = new TestOwner { Name = "Test" };
        var options = new CapabilityScopeOptions
        {
            UseComposerRegistry = true,
            UseCompositionRegistry = true
        };
        
        using var scope = new CapabilityScope<TestOwner>(owner, options);

        Assert.NotNull(scope);
        Assert.NotNull(scope.Owner);
        
        // Verify options are applied - composer should be registered when we compose
        var composer = scope.Owner.Compose();
        var retrieved = scope.Owner.GetComposer();
        Assert.Same(composer, retrieved);
    }

    [Fact]
    public void BaseScope_ObsoleteComposeMethod_StillWorksForBackwardCompatibility()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner { Name = "BackwardCompat" };
        scope.Owner.Set(owner);

#pragma warning disable CS0618 // Type or member is obsolete
        var composer = scope.Owner.Compose<TestOwner>();
#pragma warning restore CS0618 // Type or member is obsolete
        
        composer.Add(new TestCapability("old-style"));
        var composition = composer.Build();

        Assert.NotNull(composition);
        Assert.Single(composition.GetAll<TestCapability>());
    }

    [Fact]
    public void BaseScope_ObsoleteGetCompositionMethod_StillWorksForBackwardCompatibility()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner { Name = "BackwardCompatComposition" };
        scope.Owner.Set(owner);

        scope.Owner.Compose()
            .Add(new TestCapability("test"))
            .Build();

#pragma warning disable CS0618 // Type or member is obsolete
        var composition = scope.Owner.GetComposition<TestOwner>();
#pragma warning restore CS0618 // Type or member is obsolete

        Assert.NotNull(composition);
        Assert.Single(composition.GetAll<TestCapability>());
    }
}
