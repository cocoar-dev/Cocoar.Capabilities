namespace Cocoar.Capabilities.Core.Tests;

public class RecomposeTests : IDisposable
{
    private record TestSubject(string Name);
    
    private class TestCapability(string value) : ICapability<TestSubject>
    {
        public string Value { get; } = value;
    }
    
    private class AnotherCapability(int number) : ICapability<TestSubject>
    {
        public int Number { get; } = number;
    }
    
    private class PrimaryTestCapability(string name) : IPrimaryCapability<TestSubject>
    {
        public string Name { get; } = name;
    }
    
    private interface ITestContract : ICapability<TestSubject>
    {
    }
    
    private class ContractCapability(string data) : ITestContract
    {
        public string Data { get; } = data;
    }

    public void Dispose()
    {
        // No cleanup needed for Core tests
    }

    [Fact]
    public void Recompose_ShouldPreserveAllCapabilities()
    {
        
        var subject = new TestSubject("test");
        var originalComposition = Composer.For(subject)
            .Add(new TestCapability("original"))
            .Add(new AnotherCapability(42))
            .Build();
        
        
        var recomposedComposition = Composer.Recompose(originalComposition)
            .Build();
        
        
        Assert.Equal(2, recomposedComposition.TotalCapabilityCount);
        
        var testCaps = recomposedComposition.GetAll<TestCapability>();
        Assert.Single(testCaps);
        Assert.Equal("original", testCaps[0].Value);
        
        var anotherCaps = recomposedComposition.GetAll<AnotherCapability>();
        Assert.Single(anotherCaps);
        Assert.Equal(42, anotherCaps[0].Number);
    }

    [Fact]
    public void Recompose_ShouldAllowAddingNewCapabilities()
    {
        
        var subject = new TestSubject("test");
        var originalComposition = Composer.For(subject)
            .Add(new TestCapability("original"))
            .Build();
        
        
        var recomposedComposition = Composer.Recompose(originalComposition)
            .Add(new TestCapability("new"))
            .Add(new AnotherCapability(99))
            .Build();
        
        
        Assert.Equal(3, recomposedComposition.TotalCapabilityCount);
        
        var testCaps = recomposedComposition.GetAll<TestCapability>();
        Assert.Equal(2, testCaps.Count);
        Assert.Contains(testCaps, c => c.Value == "original");
        Assert.Contains(testCaps, c => c.Value == "new");
        
        var anotherCaps = recomposedComposition.GetAll<AnotherCapability>();
        Assert.Single(anotherCaps);
        Assert.Equal(99, anotherCaps[0].Number);
    }

    [Fact]
    public void Recompose_ShouldPreservePrimaryCapability()
    {
        
        var subject = new TestSubject("test");
        var primaryCap = new PrimaryTestCapability("primary");
        var originalComposition = Composer.For(subject)
            .Add(new TestCapability("test"))
            .WithPrimary(primaryCap)
            .Build();
        
        
        var recomposedComposition = Composer.Recompose(originalComposition)
            .Add(new AnotherCapability(123))
            .Build();
        
        
        Assert.True(recomposedComposition.HasPrimary());
        var primary = recomposedComposition.GetPrimary();
        Assert.Equal(primaryCap, primary);
        Assert.Equal("primary", ((PrimaryTestCapability)primary).Name);
    }

    [Fact]
    public void Recompose_ShouldAllowChangingPrimary()
    {
        
        var subject = new TestSubject("test");
        var originalPrimary = new PrimaryTestCapability("original");
        var newPrimary = new PrimaryTestCapability("new");
        
        var originalComposition = Composer.For(subject)
            .Add(new TestCapability("test"))
            .WithPrimary(originalPrimary)
            .Build();
        
        
        var recomposedComposition = Composer.Recompose(originalComposition)
            .WithPrimary(newPrimary)
            .Build();
        
        
        Assert.True(recomposedComposition.HasPrimary());
        var primary = recomposedComposition.GetPrimary();
        Assert.Equal(newPrimary, primary);
        Assert.Equal("new", ((PrimaryTestCapability)primary).Name);
        Assert.NotEqual(originalPrimary, primary);
    }

    [Fact]
    public void Recompose_ShouldPreserveContractOnlyRegistrations()
    {
        
        var subject = new TestSubject("test");
        var contractCap = new ContractCapability("contract-data");
        
        var originalComposition = Composer.For(subject)
            .Add(new TestCapability("test"))
            .AddAs<ITestContract>(contractCap)
            .Build();
        
        
        var recomposedComposition = Composer.Recompose(originalComposition)
            .Add(new AnotherCapability(456))
            .Build();
        
        
        Assert.True(recomposedComposition.Has<ITestContract>());
        var contractCaps = recomposedComposition.GetAll<ITestContract>();
        Assert.Single(contractCaps);
        Assert.Equal("contract-data", ((ContractCapability)contractCaps[0]).Data);
        
        // Should not be available via concrete type query since it was registered as contract-only
        Assert.False(recomposedComposition.Has<ContractCapability>());
    }



    [Fact]
    public void Recompose_ShouldNotMutateOriginalComposition()
    {
        
        var subject = new TestSubject("test");
        var originalComposition = Composer.For(subject)
            .Add(new TestCapability("original"))
            .Add(new AnotherCapability(42))
            .Build();
        
        var originalCapCount = originalComposition.TotalCapabilityCount;
        var originalTestCaps = originalComposition.GetAll<TestCapability>();
        
        
        var recomposedComposition = Composer.Recompose(originalComposition)
            .Add(new TestCapability("new"))
            .Add(new AnotherCapability(99))
            .Build();
        
        
        Assert.Equal(originalCapCount, originalComposition.TotalCapabilityCount);
        Assert.Equal(originalTestCaps.Count, originalComposition.GetAll<TestCapability>().Count);
        Assert.Single(originalComposition.GetAll<TestCapability>());
        Assert.Equal("original", originalComposition.GetAll<TestCapability>()[0].Value);
        
        // New composition should have additional capabilities
        Assert.Equal(4, recomposedComposition.TotalCapabilityCount);
        Assert.Equal(2, recomposedComposition.GetAll<TestCapability>().Count);
    }

    [Fact]
    public void Recompose_WithDifferentSubject_ShouldWork()
    {
        
        var subject = new TestSubject("different");
        var testCap = new TestCapability("different-subject");
        var anotherCap = new AnotherCapability(100);
        
        var originalComposition = Composer.For(subject)
            .Add(testCap)
            .Build();
        
        
        var recomposedComposition = Composer.Recompose(originalComposition)
            .Add(anotherCap)
            .Build();
        
        
        Assert.Equal("different", recomposedComposition.Subject.Name);
        Assert.Equal(2, recomposedComposition.TotalCapabilityCount);
    }

    [Fact]
    public void Recompose_MultipleChaining_ShouldWork()
    {
        
        var subject = new TestSubject("chaining");
        var baseComposition = Composer.For(subject)
            .Add(new TestCapability("base"))
            .Build();
        
        
        var step1 = Composer.Recompose(baseComposition)
            .Add(new TestCapability("step1"))
            .Build();
        
        var step2 = Composer.Recompose(step1)
            .Add(new AnotherCapability(1))
            .Build();
        
        var final = Composer.Recompose(step2)
            .Add(new TestCapability("final"))
            .Build();
        
        
        Assert.Equal(4, final.TotalCapabilityCount);
        
        var testCaps = final.GetAll<TestCapability>();
        Assert.Equal(3, testCaps.Count);
        Assert.Contains(testCaps, c => c.Value == "base");
        Assert.Contains(testCaps, c => c.Value == "step1");
        Assert.Contains(testCaps, c => c.Value == "final");
        
        var anotherCaps = final.GetAll<AnotherCapability>();
        Assert.Single(anotherCaps);
        Assert.Equal(1, anotherCaps[0].Number);
    }

    [Fact]
    public void Recompose_FluentStyle_AllowsChaining()
    {
        
        var subject = new TestSubject("fluent");
        var originalComposition = Composer.For(subject)
            .Add(new TestCapability("original"))
            .Build();

        
        var firstComposition = Composer.Recompose(originalComposition)
            .Add(new TestCapability("added"))
            .Build();

        // Verify composition properties
        Assert.Equal(2, firstComposition.TotalCapabilityCount);

        
        var secondComposition = Composer.Recompose(originalComposition)
            .Add(new TestCapability("second"))
            .Build();

        // Should be different compositions
        Assert.Equal(2, secondComposition.TotalCapabilityCount);
        Assert.NotEqual(firstComposition, secondComposition);
    }
}
