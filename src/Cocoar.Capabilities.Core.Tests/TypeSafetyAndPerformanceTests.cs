namespace Cocoar.Capabilities.Core.Tests;

public class TypeSafetyAndPerformanceTests
{
    [Fact]
    public void TryGet_TypeSafeArrayCasting_NeverThrows()
    {
        // This test verifies that the array casting approach is type-safe
        // and won't throw InvalidCastException at runtime
        
        
        var subject = new TestSubject();
        var bag = Composer.For(subject)
            .Add(new TestCapability("type-safe-1"))
            .Add(new TestCapability("type-safe-2"))
            .Add(new AnotherTestCapability(42))
            .AddAs<ITestContract>(new ConcreteTestCapability("contract-safe"))
            .Build();

        
        Assert.True(bag.TryGet<TestCapability>(out var testCap));
        Assert.Equal("type-safe-1", testCap.Value);
        
        Assert.True(bag.TryGet<AnotherTestCapability>(out var anotherCap));
        Assert.Equal(42, anotherCap.Number);
        
        Assert.True(bag.TryGet<ITestContract>(out var contractCap));
        Assert.Equal("contract-safe", contractCap.GetValue());
    }

    [Fact]
    public void GetAll_TypeSafeArrayCasting_NeverThrows()
    {
        
        var subject = new TestSubject();
        var bag = Composer.For(subject)
            .Add(new TestCapability("safe-1"))
            .Add(new TestCapability("safe-2"))
            .Add(new TestCapability("safe-3"))
            .Build();

        
        var capabilities = bag.GetAll<TestCapability>();
        
        Assert.Equal(3, capabilities.Count);
        Assert.All(capabilities, cap => Assert.IsType<TestCapability>(cap));
    }

    [Fact]
    public void GetAll_EmptyResult_ReturnsArrayEmpty_ZeroAllocation()
    {
        // This test verifies zero-allocation performance for empty results
        
        
        var subject = new TestSubject();
        var bag = Composer.For(subject).Build();

        
        var result1 = bag.GetAll<TestCapability>();
        var result2 = bag.GetAll<TestCapability>();

        
        Assert.Same(result1, result2);
        Assert.Same(Array.Empty<TestCapability>(), result1);
        Assert.Empty(result1);
    }

    [Fact]
    public void ExactTypeMatching_DoesNotFindBaseClassesOrInterfaces()
    {
        // This test verifies the exact-type matching invariant
        
        
        var subject = new TestSubject();
        var concreteCap = new ConcreteTestCapability("exact-match-test");
        
        // Add as concrete type (NOT as interface)
        var bag = Composer.For(subject)
            .Add(concreteCap)  // Registered as ConcreteTestCapability
            .Build();

        
        Assert.True(bag.TryGet<ConcreteTestCapability>(out _)); // Found by concrete type
        Assert.False(bag.TryGet<ITestContract>(out _)); // NOT found by interface
        Assert.False(bag.TryGet<ICapability<TestSubject>>(out _)); // NOT found by base interface
    }

    [Fact]
    public void MemoryUsage_MultipleCapabilities_EfficientStorage()
    {
        // This test verifies that the storage mechanism is memory efficient
        
        
        var subject = new TestSubject();
        var bag = Composer.For(subject)
            .Add(new TestCapability("mem-1"))
            .Add(new TestCapability("mem-2"))
            .Add(new AnotherTestCapability(1))
            .Add(new AnotherTestCapability(2))
            .Build();

        
        Assert.Equal(4, bag.TotalCapabilityCount);
        Assert.Equal(2, bag.Count<TestCapability>());
        Assert.Equal(2, bag.Count<AnotherTestCapability>());
        
        var testCaps = bag.GetAll<TestCapability>();
        var anotherCaps = bag.GetAll<AnotherTestCapability>();
        
        Assert.Equal(2, testCaps.Count);
        Assert.Equal(2, anotherCaps.Count);
    }

    [Fact]
    public void StabilityUnderLoad_ManyCapabilities_RemainsPerformant()
    {
        // Test with a larger number of capabilities to ensure stability
        
        
        var subject = new TestSubject();
        var builder = Composer.For(subject);
        
        // Add many capabilities of the same type
        for (var i = 0; i < 1000; i++)
        {
            builder.Add(new TestCapability($"load-test-{i}"));
        }
        
        var bag = builder.Build();

        
        Assert.Equal(1000, bag.Count<TestCapability>());
        Assert.Equal(1000, bag.TotalCapabilityCount);
        
        // First item should be accessible quickly
        Assert.True(bag.TryGet<TestCapability>(out var first));
        Assert.Equal("load-test-0", first.Value);
        
        // All items should be retrievable
        var allCaps = bag.GetAll<TestCapability>();
        Assert.Equal(1000, allCaps.Count);
        
        // Order should be preserved
        Assert.Equal("load-test-0", allCaps[0].Value);
        Assert.Equal("load-test-999", allCaps[999].Value);
    }
}
