namespace Cocoar.Capabilities.Core.Tests;

public class InterfaceQueryTests
{
    public class TestSubject
    {
        public string Name { get; set; } = "Test";
    }

    public class TestCapability(string value) : ICapability<TestSubject>
    {
        public string Value { get; } = value;
    }

    public class OrderedCapability(int order, string value) : ICapability<TestSubject>, IOrderedCapability
    {
        public int Order { get; } = order;
        public string Value { get; } = value;
    }

    [Fact]
    public void Debug_AddAs_ICapability_Query()
    {
        
        var subject = new TestSubject();
        var regularCap = new TestCapability("regular");
        var orderedCap = new OrderedCapability(10, "ordered");
        
        Console.WriteLine($"RegularCap type: {regularCap.GetType().Name}");
        Console.WriteLine($"OrderedCap type: {orderedCap.GetType().Name}");
        
        
        var bag = Composer.For(subject)
            .AddAs<ICapability<TestSubject>>(orderedCap)
            .AddAs<ICapability<TestSubject>>(regularCap)
            .Build();

        // Debug queries
        var interfaceResults = bag.GetAll<ICapability<TestSubject>>();
        var regularResults = bag.GetAll<TestCapability>();
        var orderedResults = bag.GetAll<OrderedCapability>();

        Console.WriteLine($"Interface query count: {interfaceResults.Count}");
        Console.WriteLine($"Regular concrete query count: {regularResults.Count}");
        Console.WriteLine($"Ordered concrete query count: {orderedResults.Count}");

        foreach (var item in interfaceResults)
        {
            Console.WriteLine($"  Interface item: {item.GetType().Name}");
        }

        
        Assert.Equal(2, interfaceResults.Count);
        Assert.Equal(0, regularResults.Count); // Should be filtered from concrete queries
        Assert.Equal(0, orderedResults.Count); // Should be filtered from concrete queries
    }
}
