namespace Cocoar.Capabilities.Core.Tests;

[Collection("Sequential")]
public class InterfaceContaminationTests
{
    [Fact]
    public void OldBehavior_DocumentedForReference_ContractQueriesUsedToLookupConcreteTypes()
    {
        var subject = "test-subject";
        
        var capability1 = new ContaminationTestCapability("A");
        var capability2 = new ContaminationTestCapability("B");
        
        var composition = Composer.For(subject)
            .Add(capability1)                              // Registered as ContaminationTestCapability only
            .AddAs<IContaminationTestContract>(capability2)             // Registered as IContaminationTestContract only (new behavior)
            .Build();
        
        // New behavior: Contract queries return only explicitly registered capabilities
        var contractCapabilities = composition.GetAll<IContaminationTestContract>();
        
        // NEW: Only capability2 is returned (the one explicitly registered with the interface)
        Assert.Single(contractCapabilities);
        Assert.DoesNotContain(capability1, contractCapabilities); // No longer contaminated!
        Assert.Contains(capability2, contractCapabilities); 
        
        // Both capabilities are queryable by concrete type
        var concreteCapabilities = composition.GetAll<ContaminationTestCapability>();
        Assert.Single(concreteCapabilities); // Only capability1 is registered for concrete type
        Assert.Contains(capability1, concreteCapabilities);
        Assert.DoesNotContain(capability2, concreteCapabilities); // capability2 is contract-only
    }
    
    [Fact]
    public void NewBehavior_IDBasedImplementation_OnlyExplicitlyRegisteredCapabilitiesAreQueryableByInterface()
    {
        var subject = "test-subject-2";
        
        var capability1 = new ContaminationTestCapability("A");
        var capability2 = new ContaminationTestCapability("B");
        
        var composition = Composer.For(subject)
            .Add(capability1)                              // Registered ONLY as ContaminationTestCapability
            .AddAs<IContaminationTestContract>(capability2)             // Registered as IContaminationTestContract only
            .Build();
        
        // New behavior: Only capability2 should be returned when querying by interface
        var contractCapabilities = composition.GetAll<IContaminationTestContract>();
        
        // This now PASSES - only explicitly registered capabilities are returned
        Assert.Single(contractCapabilities);
        Assert.DoesNotContain(capability1, contractCapabilities); // Correctly NOT here
        Assert.Contains(capability2, contractCapabilities);       // Only this one
        
        // Both capabilities are queryable by concrete type 
        var concreteCapabilities = composition.GetAll<ContaminationTestCapability>();
        Assert.Single(concreteCapabilities); // Only capability1 is registered for concrete type
        Assert.Contains(capability1, concreteCapabilities);
        Assert.DoesNotContain(capability2, concreteCapabilities); // capability2 is contract-only
    }
    
    [Fact]
    public void ExplicitTupleRegistration_ShouldAllowQueryingByBothTypes()
    {
        var subject = "test-subject-3";
        
        var capability = new ContaminationTestCapability("C");
        
        var composition = Composer.For(subject)
            .AddAs<(IContaminationTestContract, ContaminationTestCapability)>(capability)  // Explicitly register for both
            .Build();
        
        // Should be queryable by both interface and concrete type
        var contractCapabilities = composition.GetAll<IContaminationTestContract>();
        Assert.Single(contractCapabilities);
        Assert.Contains(capability, contractCapabilities);
        
        var concreteCapabilities = composition.GetAll<ContaminationTestCapability>();
        Assert.Single(concreteCapabilities);
        Assert.Contains(capability, concreteCapabilities);
    }
}

// Test types
public class ContaminationTestCapability : ICapability<string>, IContaminationTestContract
{
    public string Value { get; }
    
    public ContaminationTestCapability(string value)
    {
        Value = value;
    }
    
    public override bool Equals(object? obj)
    {
        return obj is ContaminationTestCapability other && Value == other.Value;
    }
    
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
    
    public override string ToString()
    {
        return $"ContaminationTestCapability({Value})";
    }
}

public interface IContaminationTestContract : ICapability<string>
{
}
