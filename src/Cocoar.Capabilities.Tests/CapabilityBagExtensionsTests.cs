using Cocoar.Capabilities.Extensions;

namespace Cocoar.Capabilities.Tests;

public class CapabilityBagExtensionsTests
{
    [Fact]
    public void Use_ExistingCapability_ExecutesAction()
    {
        // Arrange
        var subject = new TestSubject();
        var capability = new TestCapability("use-test");
        var bag = Composer.For(subject).Add(capability).Build();
        
        string? capturedValue = null;
        
        // Act
        bag.Use<TestSubject, TestCapability>(cap => capturedValue = cap.Value);
        
        // Assert
        Assert.Equal("use-test", capturedValue);
    }

    [Fact]
    public void Use_MissingCapability_DoesNotExecuteAction()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject).Build();
        
        bool actionExecuted = false;
        
        // Act
        bag.Use<TestSubject, TestCapability>(_ => actionExecuted = true);
        
        // Assert
        Assert.False(actionExecuted);
    }

    [Fact]
    public void Use_NullBag_ThrowsArgumentNull()
    {
        // Arrange
        ICapabilityBag<TestSubject> bag = null!;
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            bag.Use<TestSubject, TestCapability>(_ => { }));
    }

    [Fact]
    public void Use_NullAction_ThrowsArgumentNull()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject).Build();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            bag.Use<TestSubject, TestCapability>(null!));
    }

    [Fact]
    public void Transform_ExistingCapability_ReturnsTransformedValue()
    {
        // Arrange
        var subject = new TestSubject();
        var capability = new TestCapability("transform-test");
        var bag = Composer.For(subject).Add(capability).Build();
        
        // Act
        var result = bag.Transform<TestSubject, TestCapability, string>(cap => cap.Value.ToUpper());
        
        // Assert
        Assert.Equal("TRANSFORM-TEST", result);
    }

    [Fact]
    public void Transform_MissingCapability_ReturnsDefault()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject).Build();
        
        // Act
        var result = bag.Transform<TestSubject, TestCapability, string>(cap => cap.Value.ToUpper());
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Transform_MissingCapability_ReturnsDefaultForValueType()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject).Build();
        
        // Act
        var result = bag.Transform<TestSubject, TestCapability, int>(cap => cap.Value.Length);
        
        // Assert
        Assert.Equal(0, result); // Default for int
    }

    [Fact]
    public void Transform_NullBag_ThrowsArgumentNull()
    {
        // Arrange
        ICapabilityBag<TestSubject> bag = null!;
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            bag.Transform<TestSubject, TestCapability, string>(cap => cap.Value));
    }

    [Fact]
    public void Transform_NullTransformer_ThrowsArgumentNull()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject).Build();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            bag.Transform<TestSubject, TestCapability, string>(null!));
    }
}