using Xunit;

namespace Cocoar.Capabilities.Tests;

public class BasicCompositionTests
{
    [Fact]
    public void Build_WithRegistryDisabled_CompositionNotRegistered()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("s1");

        var composition = scope.For(subject, useRegistry: false)
            .Add(new TestCapability("A"))
            .Build(useRegistry: false);

        Assert.NotNull(composition);
        var testCaps = composition.GetAll<TestCapability>();
        Assert.Single(testCaps);
        Assert.Equal("A", testCaps[0].Name);

        var found = scope.Compositions.GetOrDefault(subject);
        Assert.Null(found);
    }

    [Fact]
    public void Build_WithOverrideEnablesRegistry_CompositionRegistered()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("s2");

        var composition = scope.For(subject, useRegistry: true)
            .Add(new TestCapability("B"))
            .Build(useRegistry: true);

        var found = scope.Compositions.GetOrDefault(subject);
        Assert.NotNull(found);
        Assert.Same(composition, found);
        Assert.Equal("B", found!.GetAll<TestCapability>()[0].Name);
    }

    [Fact]
    public void AddAs_WithTupleContracts_RegistersUnderBothContracts()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("contracts");
        var impl = new MultiContractImplementation("multi", "desc", 42);

        var composition = scope.For(subject)
            .AddAs<(ITestContract, IAlternateContract)>(impl)
            .Build();

        var testContracts = composition.GetAll<ITestContract>();
        var altContracts = composition.GetAll<IAlternateContract>();

        Assert.Single(testContracts);
        Assert.Single(altContracts);
        Assert.Same(testContracts[0], altContracts[0]);
        Assert.Equal("multi", ((MultiContractImplementation)testContracts[0]).Name);
    }

    [Fact]
    public void Build_WithMultiplePrimaryCapabilities_Throws()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("prim");
        var builder = scope.For(subject)
            .Add(new PrimaryTestCapability("P1"));

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Add(new AlternatePrimaryCapability("P2")));
        Assert.Contains("primary capability is already set", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
