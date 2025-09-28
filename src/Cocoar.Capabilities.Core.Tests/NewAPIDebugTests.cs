namespace Cocoar.Capabilities.Core.Tests;

public class NewAPIDebugTests
{
    [Fact]
    public void Debug_GetAll_ReturnsExpectedResults()
    {
        
        var config = new DatabaseConfig();
        var bag = Composer.For(config)
            .Add(new SingletonLifetimeCapability())
            .Build();

        // Debug: Check what we have
        var allCapabilities = bag.GetAll();
        var specificCapabilities = bag.GetAll<SingletonLifetimeCapability>();
        
        // More detailed debugging
        Console.WriteLine($"Total capability count: {bag.TotalCapabilityCount}");
        Console.WriteLine($"GetAll() returned: {allCapabilities.Count}");
        Console.WriteLine($"GetAll<SingletonLifetimeCapability>() returned: {specificCapabilities.Count}");
        
        
        Assert.True(bag.TotalCapabilityCount > 0, $"Bag should have capabilities, but TotalCapabilityCount is {bag.TotalCapabilityCount}");
        Assert.True(specificCapabilities.Count > 0, $"GetAll<SingletonLifetimeCapability>() returned {specificCapabilities.Count} capabilities");
        Assert.True(allCapabilities.Count > 0, $"GetAll() returned {allCapabilities.Count} capabilities");
        
        // Verify that our new GetAll() returns the same capability
        Assert.Equal(specificCapabilities[0], allCapabilities[0]);
    }
    
    [Fact]
    public void Debug_HasPrimary_ChecksImplementation()
    {
        
        var config = new DatabaseConfig();
        var primary = new TestPrimaryCapability("test");
        var bag = Composer.For(config)
            .Add(primary) // Register as concrete type, not as interface
            .Build();

        // Debug: Check what we have
        var hasAnyPrimary = bag.HasPrimary();
        var hasSpecificPrimary = bag.HasPrimary<TestPrimaryCapability>();
        
        var primaryCapabilities = bag.GetAll<IPrimaryCapability<DatabaseConfig>>();
        var testPrimaryCapabilities = bag.GetAll<TestPrimaryCapability>();
        
        
        Assert.True(hasAnyPrimary, "HasPrimary() should return true");
        Assert.True(testPrimaryCapabilities.Count > 0, $"Should have {testPrimaryCapabilities.Count} TestPrimaryCapability capabilities");
        Assert.True(hasSpecificPrimary, $"HasPrimary<TestPrimaryCapability>() should return true. TestPrimary count: {testPrimaryCapabilities.Count}");
    }
}
