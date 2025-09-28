namespace Cocoar.Capabilities.Core.Tests;

public class ValueTypeTests
{
    private record struct TestStruct(int Id, string Name);
    
    private class StructCapability(TestStruct subject) : ICapability<TestStruct>
    {
        public TestStruct Subject { get; } = subject;
    }
    
    private class IntCapability(int subject) : ICapability<int>
    {
        public int Subject { get; } = subject;
    }
    
    private class StringCapability(string subject) : ICapability<string>
    {
        public string Subject { get; } = subject;
    }

    [Fact]
    public void CanComposeCapabilitiesForIntegerValues()
    {
        
        var number = 42;
        var capability = new IntCapability(number);
        
        
        var composer = Composer.For(number);
        composer.Add(capability);
        var composition = composer.Build();
        
        
        Assert.Equal(number, composition.Subject);
        Assert.Equal(1, composition.TotalCapabilityCount);
        var capabilities = composition.GetAll<IntCapability>();
        Assert.Single(capabilities);
        Assert.Equal(number, capabilities[0].Subject);
    }
    
    [Fact]
    public void CanComposeCapabilitiesForStringValues()
    {
        
        var text = "special";
        var capability = new StringCapability(text);
        
        
        var composer = Composer.For(text);
        composer.Add(capability);
        var composition = composer.Build();
        
        
        Assert.Equal(text, composition.Subject);
        var capabilities = composition.GetAll<StringCapability>();
        Assert.Single(capabilities);
        Assert.Equal(text, capabilities[0].Subject);
    }
    
    [Fact]
    public void CanComposeCapabilitiesForStructValues()
    {
        
        var structValue = new TestStruct(1, "Test");
        var capability = new StructCapability(structValue);
        
        
        var composer = Composer.For(structValue);
        composer.Add(capability);
        var composition = composer.Build();
        
        
        Assert.Equal(structValue, composition.Subject);
        var capabilities = composition.GetAll<StructCapability>();
        Assert.Single(capabilities);
        Assert.Equal(structValue, capabilities[0].Subject);
    }
}
