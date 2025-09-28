using Xunit;

namespace Cocoar.Capabilities.Tests;

public class GetFirstTests
{
    [Fact]
    public void GetFirstOrDefault_WithNoCapabilities_ReturnsNull()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var composition = scope.For(subject).Build();

        var result = composition.GetFirstOrDefault<TestCapability>();

        Assert.Null(result);
    }

    [Fact]
    public void GetFirstOrDefault_WithOneCapability_ReturnsThatCapability()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var capability = new TestCapability("First");
        var composition = scope.For(subject)
            .Add(capability)
            .Build();

        var result = composition.GetFirstOrDefault<TestCapability>();

        Assert.NotNull(result);
        Assert.Same(capability, result);
        Assert.Equal("First", result.Name);
    }

    [Fact]
    public void GetFirstOrDefault_WithMultipleCapabilities_ReturnsFirstInOrder()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var composition = scope.For(subject)
            .Add(new TestCapability("Third"), order: 30)
            .Add(new TestCapability("First"), order: 10)
            .Add(new TestCapability("Second"), order: 20)
            .Build();

        var result = composition.GetFirstOrDefault<TestCapability>();

        Assert.NotNull(result);
        Assert.Equal("First", result!.Name);
    }

    [Fact]
    public void TryGetFirst_WithNoCapabilities_ReturnsFalse()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var composition = scope.For(subject).Build();

        var found = composition.TryGetFirst<TestCapability>(out var result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void TryGetFirst_WithOneCapability_ReturnsTrueAndCapability()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var capability = new TestCapability("Only");
        var composition = scope.For(subject)
            .Add(capability)
            .Build();

        var found = composition.TryGetFirst<TestCapability>(out var result);

        Assert.True(found);
        Assert.NotNull(result);
        Assert.Same(capability, result);
        Assert.Equal("Only", result.Name);
    }

    [Fact]
    public void TryGetFirst_WithMultipleCapabilities_ReturnsTrueAndFirstInOrder()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var composition = scope.For(subject)
            .Add(new TestCapability("C"), order: 3)
            .Add(new TestCapability("A"), order: 1)
            .Add(new TestCapability("B"), order: 2)
            .Build();

        var found = composition.TryGetFirst<TestCapability>(out var result);

        Assert.True(found);
        Assert.NotNull(result);
        Assert.Equal("A", result!.Name);
    }

    [Fact]
    public void GetFirstOrDefault_WithDifferentTypes_ReturnsCorrectType()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var composition = scope.For(subject)
            .Add(new TestCapability("TestCap"))
            .Add(new DocumentCapability("DocType", "Content"))
            .Build();

        var testResult = composition.GetFirstOrDefault<TestCapability>();
        var docResult = composition.GetFirstOrDefault<DocumentCapability>();

        Assert.NotNull(testResult);
        Assert.Equal("TestCap", testResult!.Name);

        Assert.NotNull(docResult);
        Assert.Equal("DocType", docResult!.Type);
    }

    [Fact]
    public void TryGetFirst_WithRegistry_WorksCorrectly()
    {
        var options = new CapabilityScopeOptions
        {
            UseCompositionRegistry = true
        };
        using var scope = new CapabilityScope(options);
        var subject = new StringSubject("registry-test");

        var composition = scope.For(subject, useRegistry: true)
            .Add(new TestCapability("First"))
            .Add(new TestCapability("Second"))
            .Build(useRegistry: true);

        var retrieved = scope.Compositions.GetOrDefault(subject);
        Assert.NotNull(retrieved);

        var found = retrieved!.TryGetFirst<TestCapability>(out var result);

        Assert.True(found);
        Assert.Equal("First", result!.Name);
    }

    [Fact]
    public void GetFirstOrDefault_WithNoOrderSpecified_ReturnsFirstAdded()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var composition = scope.For(subject)
            .Add(new TestCapability("FirstAdded"))
            .Add(new TestCapability("SecondAdded"))
            .Add(new TestCapability("ThirdAdded"))
            .Build();

        var result = composition.GetFirstOrDefault<TestCapability>();

        Assert.NotNull(result);
        Assert.Equal("FirstAdded", result!.Name);
    }

    [Fact]
    public void GetRequiredFirst_WithNoCapabilities_ThrowsInvalidOperationException()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var composition = scope.For(subject).Build();

        var ex = Assert.Throws<InvalidOperationException>(() => 
            composition.GetRequiredFirst<TestCapability>());
        
        Assert.Contains("Capability of type 'TestCapability' not found", ex.Message);
    }

    [Fact]
    public void GetRequiredFirst_WithOneCapability_ReturnsThatCapability()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var capability = new TestCapability("Required");
        var composition = scope.For(subject)
            .Add(capability)
            .Build();

        var result = composition.GetRequiredFirst<TestCapability>();

        Assert.NotNull(result);
        Assert.Same(capability, result);
        Assert.Equal("Required", result.Name);
    }

    [Fact]
    public void GetRequiredFirst_WithMultipleCapabilities_ReturnsFirstInOrder()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var composition = scope.For(subject)
            .Add(new TestCapability("Z"), order: 30)
            .Add(new TestCapability("A"), order: 10)
            .Add(new TestCapability("M"), order: 20)
            .Build();

        var result = composition.GetRequiredFirst<TestCapability>();

        Assert.NotNull(result);
        Assert.Equal("A", result.Name);
    }

    [Fact]
    public void GetRequiredFirst_WithDifferentTypes_ReturnsCorrectType()
    {
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        var composition = scope.For(subject)
            .Add(new TestCapability("TestCap"))
            .Add(new DocumentCapability("DocType", "Content"))
            .Build();

        var testResult = composition.GetRequiredFirst<TestCapability>();
        var docResult = composition.GetRequiredFirst<DocumentCapability>();

        Assert.NotNull(testResult);
        Assert.Equal("TestCap", testResult.Name);

        Assert.NotNull(docResult);
        Assert.Equal("DocType", docResult.Type);
    }
}
