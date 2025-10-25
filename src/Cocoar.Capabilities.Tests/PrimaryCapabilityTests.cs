using Xunit;

namespace Cocoar.Capabilities.Tests;

public class PrimaryCapabilityTests
{
    private static CapabilityScope NewScope(bool composer = false, bool composition = false)
        => new(new CapabilityScopeOptions { UseComposerRegistry = composer, UseCompositionRegistry = composition });

    [Fact]
    public void HasPrimary_False_WhenNoneAdded()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p0");
        var comp = scope.Compose(subject).Add(new TestCapability("A")).Build();
        Assert.False(comp.HasPrimary());
    }

    [Fact]
    public void HasPrimary_True_WhenPrimaryAdded()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p1");
        var comp = scope.Compose(subject)
                        .Add(new PrimaryTestCapability("P"))
                        .Add(new TestCapability("X"))
                        .Build();
        Assert.True(comp.HasPrimary());
        Assert.True(comp.HasPrimary<PrimaryTestCapability>());
    }

    [Fact]
    public void TryGetPrimary_ReturnsFalse_WhenNone()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p2");
        var comp = scope.Compose(subject).Add(new TestCapability("A")).Build();
        Assert.False(comp.TryGetPrimary(out var _));
    }

    [Fact]
    public void TryGetPrimary_ReturnsTrue_WhenExists()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p3");
        var comp = scope.Compose(subject)
            .Add(new PrimaryTestCapability("P"))
            .Add(new TestCapability("A"))
            .Build();
        Assert.True(comp.TryGetPrimary(out var primary));
        Assert.NotNull(primary);
    }

    [Fact]
    public void GetPrimary_ReturnsInstance_WhenExists()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p4");
        var comp = scope.Compose(subject).Add(new PrimaryTestCapability("P")).Build();
        var primary = comp.GetPrimary();
        Assert.Equal("P", ((PrimaryTestCapability)primary).Name);
    }

    [Fact]
    public void GetPrimary_Throws_WhenMissing()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p5");
        var comp = scope.Compose(subject).Add(new TestCapability("X")).Build();
        var ex = Assert.Throws<InvalidOperationException>(() => comp.GetPrimary());
        Assert.Contains("Primary capability not found", ex.Message);
    }

    [Fact]
    public void TryGetPrimaryAs_False_WhenWrongType()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p6");
        var comp = scope.Compose(subject)
                        .Add(new PrimaryTestCapability("P"))
                        .Build();
        Assert.False(comp.TryGetPrimaryAs<AlternatePrimaryCapability>(out _));
    }

    [Fact]
    public void TryGetPrimaryAs_True_WhenMatchingSubtype()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p7");
        var comp = scope.Compose(subject)
                        .Add(new PrimaryTestCapability("P"))
                        .Build();
    Assert.True(comp.TryGetPrimaryAs<PrimaryTestCapability>(out var primary));
    Assert.Equal("P", primary.Name);
    }

    [Fact]
    public void GetPrimaryOrDefault_ReturnsNull_WhenMissing()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p8");
        var comp = scope.Compose(subject).Add(new TestCapability("X")).Build();
        Assert.Null(comp.GetPrimaryOrDefault());
    }

    [Fact]
    public void GetPrimaryOrDefaultAs_ReturnsPrimary_WhenPresent()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p9");
        var comp = scope.Compose(subject).Add(new PrimaryTestCapability("P")).Build();
    var p = comp.GetPrimaryOrDefaultAs<PrimaryTestCapability>();
    Assert.NotNull(p);
    Assert.Equal("P", p!.Name);
    }

    [Fact]
    public void GetRequiredPrimaryAs_ReturnsInstance_WhenPresent()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p10");
        var comp = scope.Compose(subject).Add(new PrimaryTestCapability("P")).Build();
    var p = comp.GetRequiredPrimaryAs<PrimaryTestCapability>();
    Assert.Equal("P", p.Name);
    }

    [Fact]
    public void GetRequiredPrimaryAs_Throws_WhenMissing()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p11");
        var comp = scope.Compose(subject).Add(new TestCapability("Z")).Build();
        var ex = Assert.Throws<InvalidOperationException>(() => comp.GetRequiredPrimaryAs<PrimaryTestCapability>());
        Assert.Contains("Primary capability of type", ex.Message);
    }

    [Fact]
    public void WithPrimary_ReplacesExistingPrimary()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p12");
        var builder = scope.Compose(subject)
                           .Add(new PrimaryTestCapability("First"))
                           .WithPrimary(new AlternatePrimaryCapability("Second"));
        var comp = builder.Build();
        Assert.True(comp.HasPrimary());
        Assert.True(comp.TryGetPrimary(out var primary));
        Assert.IsType<AlternatePrimaryCapability>(primary);
        Assert.Equal("Second", ((AlternatePrimaryCapability)primary).Value);
    }

    [Fact]
    public void WithPrimary_Null_RemovesExistingPrimary()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p13");
        var comp = scope.Compose(subject)
                        .Add(new PrimaryTestCapability("First"))
                        .WithPrimary(null)
                        .Build();
        Assert.False(comp.HasPrimary());
        Assert.False(comp.TryGetPrimary(out _));
    }

    [Fact]
    public void Add_DuplicatePrimary_Throws()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p14");
        var builder = scope.Compose(subject)
                           .Add(new PrimaryTestCapability("First"));
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Add(new AlternatePrimaryCapability("Second")));
        Assert.Contains("primary capability is already set", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddAs_PrimaryDuplicate_Throws()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p15");
        var builder = scope.Compose(subject)
                           .AddAs<IPrimaryCapability>(new PrimaryTestCapability("First"));
        var ex = Assert.Throws<InvalidOperationException>(() => builder.AddAs<IPrimaryCapability>(new AlternatePrimaryCapability("Second")));
        Assert.Contains("primary capability is already set", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private record TuplePrimaryCapability(string Name) : IPrimaryCapability, ITestContract;
    private record SecondTuplePrimary(string Name) : IPrimaryCapability, ITestContract;

    [Fact]
    public void AddAs_TupleContainingPrimary_WhenAlreadyPresent_Throws()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p16");
        var builder = scope.Compose(subject)
                           .Add(new PrimaryTestCapability("First"));
        var ex = Assert.Throws<InvalidOperationException>(() => builder.AddAs<(IPrimaryCapability, ITestContract)>(new TuplePrimaryCapability("Second")));
        Assert.Contains("primary capability is already set", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddAs_TupleWithTwoPrimaryContracts_Throws()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p17");
        var builder = scope.Compose(subject);
        var ex = Assert.Throws<InvalidOperationException>(() => builder.AddAs<(IPrimaryCapability, IPrimaryCapability)>(new TuplePrimaryCapability("Both")));
        Assert.Contains("Multiple primary capability contracts", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryAdd_DoesNotReplaceExistingPrimary()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p18");
        var builder = scope.Compose(subject)
                           .Add(new PrimaryTestCapability("Original"));

        // Should silently ignore because a primary implementing PrimaryTestCapability already exists
        builder.TryAdd(new AlternatePrimaryCapability("Ignored"));

        var comp = builder.Build();
        var primary = comp.GetRequiredPrimaryAs<PrimaryTestCapability>();
        Assert.Equal("Original", primary.Name);
    }

    [Fact]
    public void TryAddAs_DoesNotReplaceExistingPrimary()
    {
        using var scope = NewScope();
        var subject = new StringSubject("p19");
        var builder = scope.Compose(subject)
                           .AddAs<IPrimaryCapability>(new PrimaryTestCapability("Original"));

        // Should no-op, not throw, not replace
        builder.TryAddAs<IPrimaryCapability>(new AlternatePrimaryCapability("Ignored"));

        var comp = builder.Build();
        var primary = comp.GetRequiredPrimaryAs<PrimaryTestCapability>();
        Assert.Equal("Original", primary.Name);
    }
}
