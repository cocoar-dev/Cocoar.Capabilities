using Xunit;

namespace Cocoar.Capabilities.Tests;

public class GetLastTests
{
    private record TestCapability(string Value);
    private record OtherCapability(int Number);

    [Fact]
    public void GetLastOrDefault_WithNoCapabilities_ReturnsNull()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var composition = scope.For(subject).Build();

        var result = composition.GetLastOrDefault<TestCapability>();

        Assert.Null(result);
    }

    [Fact]
    public void GetLastOrDefault_WithOneCapability_ReturnsThatCapability()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var capability = new TestCapability("only");
        var composition = scope.For(subject)
            .Add(capability)
            .Build();

        var result = composition.GetLastOrDefault<TestCapability>();

        Assert.Same(capability, result);
    }

    [Fact]
    public void GetLastOrDefault_WithMultipleCapabilities_ReturnsLastInOrder()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var first = new TestCapability("first");
        var second = new TestCapability("second");
        var third = new TestCapability("third");
        
        var composition = scope.For(subject)
            .Add(first)
            .Add(second)
            .Add(third)
            .Build();

        var result = composition.GetLastOrDefault<TestCapability>();

        Assert.Same(third, result);
        Assert.Equal("third", result!.Value);
    }

    [Fact]
    public void GetLastOrDefault_WithDifferentTypes_ReturnsCorrectType()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var testCap = new TestCapability("test");
        var otherCap = new OtherCapability(42);
        
        var composition = scope.For(subject)
            .Add(testCap)
            .Add(otherCap)
            .Build();

        var result = composition.GetLastOrDefault<OtherCapability>();

        Assert.Same(otherCap, result);
        Assert.Equal(42, result!.Number);
    }

    [Fact]
    public void GetRequiredLast_WithNoCapabilities_ThrowsInvalidOperationException()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var composition = scope.For(subject).Build();

        var ex = Assert.Throws<InvalidOperationException>(
            () => composition.GetRequiredLast<TestCapability>());
        
        Assert.Contains("TestCapability", ex.Message);
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void GetRequiredLast_WithOneCapability_ReturnsThatCapability()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var capability = new TestCapability("only");
        var composition = scope.For(subject)
            .Add(capability)
            .Build();

        var result = composition.GetRequiredLast<TestCapability>();

        Assert.Same(capability, result);
    }

    [Fact]
    public void GetRequiredLast_WithMultipleCapabilities_ReturnsLastInOrder()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var first = new TestCapability("first");
        var second = new TestCapability("second");
        var third = new TestCapability("third");
        
        var composition = scope.For(subject)
            .Add(first)
            .Add(second)
            .Add(third)
            .Build();

        var result = composition.GetRequiredLast<TestCapability>();

        Assert.Same(third, result);
        Assert.Equal("third", result.Value);
    }

    [Fact]
    public void GetRequiredLast_WithDifferentTypes_ReturnsCorrectType()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var testCap = new TestCapability("test");
        var otherCap = new OtherCapability(42);
        
        var composition = scope.For(subject)
            .Add(testCap)
            .Add(otherCap)
            .Build();

        var result = composition.GetRequiredLast<OtherCapability>();

        Assert.Same(otherCap, result);
        Assert.Equal(42, result.Number);
    }

    [Fact]
    public void TryGetLast_WithNoCapabilities_ReturnsFalse()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var composition = scope.For(subject).Build();

        var result = composition.TryGetLast<TestCapability>(out var capability);

        Assert.False(result);
        Assert.Null(capability);
    }

    [Fact]
    public void TryGetLast_WithOneCapability_ReturnsTrueAndCapability()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var expected = new TestCapability("only");
        var composition = scope.For(subject)
            .Add(expected)
            .Build();

        var result = composition.TryGetLast<TestCapability>(out var capability);

        Assert.True(result);
        Assert.Same(expected, capability);
    }

    [Fact]
    public void TryGetLast_WithMultipleCapabilities_ReturnsTrueAndLastCapability()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var first = new TestCapability("first");
        var second = new TestCapability("second");
        var third = new TestCapability("third");
        
        var composition = scope.For(subject)
            .Add(first)
            .Add(second)
            .Add(third)
            .Build();

        var result = composition.TryGetLast<TestCapability>(out var capability);

        Assert.True(result);
        Assert.Same(third, capability);
        Assert.Equal("third", capability!.Value);
    }

    [Fact]
    public void TryGetLast_WithDifferentTypes_ReturnsCorrectType()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var testCap = new TestCapability("test");
        var otherCap = new OtherCapability(42);
        
        var composition = scope.For(subject)
            .Add(testCap)
            .Add(otherCap)
            .Build();

        var result = composition.TryGetLast<OtherCapability>(out var capability);

        Assert.True(result);
        Assert.Same(otherCap, capability);
        Assert.Equal(42, capability!.Number);
    }

    [Fact]
    public void GetLast_WithOrdering_RespectsOrderConfiguration()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");
        
        var low = new TestCapability("low-priority");
        var medium = new TestCapability("medium-priority");
        var high = new TestCapability("high-priority");
        
        var composition = scope.For(subject)
            .Add(medium, order: 5)
            .Add(high, order: 10)
            .Add(low, order: 1)
            .Build();

        // Last should be the highest order value
        var result = composition.GetLastOrDefault<TestCapability>();

        Assert.Same(high, result);
        Assert.Equal("high-priority", result!.Value);
    }
}
