namespace Cocoar.Capabilities.Tests;

public class TupleTypeExtractorNegativeTests
{
    private sealed record Subject(int Id);
    private sealed record PrimaryA(string Name) : IPrimaryCapability<Subject>;
    private sealed record PrimaryB(string Name) : IPrimaryCapability<Subject>;

    [Fact]
    public void TupleWithTwoPrimaryContracts_Throws()
    {
        using var scope = new CapabilityScope();
        var composer = scope.For(new Subject(42));
        // Register first primary normally
        composer.Add(new PrimaryA("P1"));
        // Adding tuple containing another primary should throw (duplicate primary detection)
        Assert.Throws<InvalidOperationException>(() => composer.AddAs<(IPrimaryCapability<Subject>, PrimaryB)>(new PrimaryB("P2")));
    }
}
