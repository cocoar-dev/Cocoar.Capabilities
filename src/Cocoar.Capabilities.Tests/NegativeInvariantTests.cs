using Xunit;

namespace Cocoar.Capabilities.Tests;

public class NegativeInvariantTests
{
    private static CapabilityScope NewScope() => new(TestOptions.Disabled);

    [Fact]
    public void Build_Twice_Throws()
    {
        using var scope = NewScope();
        var subject = new StringSubject("dbl");
        var composer = scope.Compose(subject)
            .Add(new TestCapability("A"));
        var comp = composer.Build();
        Assert.NotNull(comp);
        var ex = Assert.Throws<InvalidOperationException>(() => composer.Build());
        Assert.Contains("Build() can only be called once", ex.Message);
    }

    [Fact]
    public void Add_AfterBuild_Throws()
    {
        using var scope = NewScope();
        var composer = scope.Compose(new StringSubject("after"))
            .Add(new TestCapability("X"));
        composer.Build();
        var ex = Assert.Throws<InvalidOperationException>(() => composer.Add(new TestCapability("Y")));
        Assert.Contains("builder is no longer usable", ex.Message);
    }

    [Fact]
    public void Build_AfterScopeDisposed_FromNewComposer_Throws()
    {
        var scope = NewScope();
        var subject = new StringSubject("disp");
        var composer = scope.Compose(subject).Add(new TestCapability("A"));
        scope.Dispose();
        // Existing composer can still build (current implementation) - treat as allowed invariant
        var comp = composer.Build();
        Assert.NotNull(comp);
        // But creating a new composer after dispose should throw
        Assert.Throws<ObjectDisposedException>(() => scope.Compose(new StringSubject("new")));
    }

    [Fact]
    public void For_NullSubject_Throws()
    {
        using var scope = NewScope();
        Assert.Throws<ArgumentNullException>(() => scope.Compose(null!));
    }

    [Fact]
    public void Add_NullCapability_Throws()
    {
        using var scope = NewScope();
        var composer = scope.Compose(new StringSubject("nullAdd"));
        Assert.Throws<ArgumentNullException>(() => composer.Add(null!));
    }

    [Fact]
    public void AddAs_NullCapability_Throws()
    {
        using var scope = NewScope();
        var composer = scope.Compose(new StringSubject("nullAddAs"));
        Assert.Throws<ArgumentNullException>(() => composer.AddAs<ITestContract>(null!));
    }

    [Fact]
    public void TryAdd_NullCapability_Throws()
    {
        using var scope = NewScope();
        var composer = scope.Compose(new StringSubject("nullTryAdd"));
        Assert.Throws<ArgumentNullException>(() => composer.TryAdd<TestCapability>(null!));
    }

    [Fact]
    public void TryAddAs_NullCapability_Throws()
    {
        using var scope = NewScope();
        var composer = scope.Compose(new StringSubject("nullTryAddAs"));
        Assert.Throws<ArgumentNullException>(() => composer.TryAddAs<ITestContract>(null!));
    }

    [Fact]
    public void WithPrimary_Null_AfterPrimary_Removes()
    {
        using var scope = NewScope();
        var comp = scope.Compose(new StringSubject("primNull"))
            .Add(new PrimaryTestCapability("P"))
            .WithPrimary(null)
            .Build();
        Assert.False(comp.HasPrimary());
    }

    [Fact]
    public void Composition_RemainsUsable_AfterScopeDisposed()
    {
        var scope = NewScope();
        var subject = new StringSubject("life");
        var comp = scope.Compose(subject)
            .Add(new TestCapability("A"))
            .Add(new TestCapability("B"))
            .Build();
        // Query before disposal
        Assert.Equal(2, comp.GetAll<TestCapability>().Count);
        scope.Dispose();
        // Snapshot still usable
        Assert.Equal(2, comp.GetAll<TestCapability>().Count);
        // Creating new composer should throw
        Assert.Throws<ObjectDisposedException>(() => scope.Compose(new StringSubject("after")));
    }
}
