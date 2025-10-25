namespace Cocoar.Capabilities.Tests;

public class ComposerPrimaryNegativeTests
{
    private sealed record Subject(int Id);
    private sealed record PrimaryA(string Name) : IPrimaryCapability;
    private sealed record PrimaryB(string Name) : IPrimaryCapability;
    private sealed record Regular(string Name) ;

    [Fact]
    public void Add_DuplicatePrimary_Throws()
    {
        using var scope = new CapabilityScope();
        var composer = scope.Compose(new Subject(1));
        composer.Add(new PrimaryA("P1"));
        Assert.Throws<InvalidOperationException>(() => composer.Add(new PrimaryB("P2")));
    }

    [Fact]
    public void AddAs_PrimaryContract_Duplicate_Throws()
    {
        using var scope = new CapabilityScope();
        var composer = scope.Compose(new Subject(2));
        composer.AddAs<IPrimaryCapability>(new PrimaryA("P1"));
        Assert.Throws<InvalidOperationException>(() => composer.AddAs<IPrimaryCapability>(new PrimaryB("P2")));
    }

    [Fact]
    public void AddTuple_WithPrimaryThenDuplicatePrimary_Throws()
    {
        using var scope = new CapabilityScope();
        var composer = scope.Compose(new Subject(3));
        composer.AddAs<(IPrimaryCapability, PrimaryA)>(new PrimaryA("P1"));
        Assert.Throws<InvalidOperationException>(() => composer.AddAs<(IPrimaryCapability, PrimaryB)>(new PrimaryB("P2")));
    }
}
