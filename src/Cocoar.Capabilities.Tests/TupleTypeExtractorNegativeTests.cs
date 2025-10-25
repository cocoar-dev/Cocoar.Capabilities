namespace Cocoar.Capabilities.Tests;

public class TupleTypeExtractorNegativeTests
{
    private sealed record Subject(int Id);
    private sealed record PrimaryA(string Name) : IPrimaryCapability;
    private sealed record PrimaryB(string Name) : IPrimaryCapability;

    [Fact]
    public void TupleWithTwoPrimaryContracts_Throws()
    {
        using var scope = new CapabilityScope();
        var composer = scope.Compose(new Subject(42));
        // Register first primary normally
        composer.Add(new PrimaryA("P1"));
        // Adding tuple containing another primary should throw (duplicate primary detection)
        Assert.Throws<InvalidOperationException>(() => composer.AddAs<(IPrimaryCapability, PrimaryB)>(new PrimaryB("P2")));
    }
}
