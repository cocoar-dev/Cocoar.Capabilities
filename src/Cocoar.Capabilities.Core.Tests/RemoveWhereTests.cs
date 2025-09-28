namespace Cocoar.Capabilities.Core.Tests;

[Collection("Sequential")]
public class RemoveWhereTests
{
    [Fact]
    public void RemoveWhere_ShouldRemoveCapabilitiesMatchingPredicate()
    {
        var subject = "test-subject";
        
        var cap1 = new SimpleTestCapability("Keep");
        var cap2 = new SimpleTestCapability("Remove");
        var cap3 = new SimpleTestCapability("Keep");
        
        var composer = Composer.For(subject)
            .Add(cap1)
            .Add(cap2)
            .Add(cap3);
        
        // Remove capabilities with "Remove" in their name
        composer.RemoveWhere(cap => cap is SimpleTestCapability simple && simple.Name == "Remove");
        
        // Build and verify removal worked
        var composition = composer.Build();
        var finalCaps = composition.GetAll<SimpleTestCapability>();
        Assert.Equal(2, finalCaps.Count);
        Assert.Contains(cap1, finalCaps);
        Assert.DoesNotContain(cap2, finalCaps);
        Assert.Contains(cap3, finalCaps);
    }
    
    [Fact]
    public void AddAs_ShouldOnlyRegisterUnderSpecifiedContract()
    {
        var subject = "test-addas";
        
        var cap1 = new SimpleTestCapability("ConcreteOnly");
        var cap2 = new SimpleTestCapability("InterfaceOnly"); 
        
        var composition = Composer.For(subject)
            .Add(cap1)                                    // Register under concrete type only
            .AddAs<ISimpleContract>(cap2)                 // Register under interface only
            .Build();
        
        // Query by interface should only return cap2
        var interfaceCaps = composition.GetAll<ISimpleContract>();
        Assert.Single(interfaceCaps);
        Assert.Contains(cap2, interfaceCaps);
        Assert.DoesNotContain(cap1, interfaceCaps); // This should NOT be here!
        
        // Query by concrete type should only return cap1 (cap2 is contract-only)
        var concreteCaps = composition.GetAll<SimpleTestCapability>();
        Assert.Single(concreteCaps);
        Assert.Contains(cap1, concreteCaps);
        Assert.DoesNotContain(cap2, concreteCaps); // cap2 is contract-only
    }
    
    [Fact]
    public void RemoveWhere_ShouldWorkWithContractOnlyCapabilities()
    {
        var subject = "test-contract-removal";
        
        var cap1 = new SimpleTestCapability("Regular");
        var cap2 = new SimpleTestCapability("ContractOnly");
        var cap3 = new SimpleTestCapability("Regular2");
        
        var composer = Composer.For(subject)
            .Add(cap1)                                    // Regular registration
            .AddAs<ISimpleContract>(cap2)                 // Contract-only registration  
            .Add(cap3);                                   // Regular registration
        
        var tempBag = Composer.For("temp")
            .Add(cap1)
            .AddAs<ISimpleContract>(cap2)  
            .Add(cap3)
            .Build();
        Assert.Equal(3, tempBag.TotalCapabilityCount);
        
        // Remove contract-only capabilities using pattern matching
        composer.RemoveWhere(cap => cap is SimpleTestCapability simple && simple.Name == "ContractOnly");
        
        // Build and verify results
        var composition = composer.Build();
        
        // Interface query should be empty (contract-only capability was removed)
        var interfaceCaps = composition.GetAll<ISimpleContract>();
        Assert.Empty(interfaceCaps);
        
        // Concrete query should return the remaining regular capabilities
        var concreteCaps = composition.GetAll<SimpleTestCapability>();
        Assert.Equal(2, concreteCaps.Count);
        Assert.Contains(cap1, concreteCaps);
        Assert.Contains(cap3, concreteCaps);
    }
}

public class SimpleTestCapability : ICapability<string>, ISimpleContract
{
    public string Name { get; }
    
    public SimpleTestCapability(string name)
    {
        Name = name;
    }
    
    public override string ToString() => $"SimpleTestCapability({Name})";
}

public interface ISimpleContract : ICapability<string>
{
}
