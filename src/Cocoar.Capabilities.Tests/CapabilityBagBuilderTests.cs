using Cocoar.Capabilities;

namespace Cocoar.Capabilities.Tests;

public class CapabilityBagBuilderTests
{
    [Fact]
    public void Build_CalledTwice_ThrowsInvalidOperation()
    {
        // Arrange
        var subject = new TestSubject();
        var builder = Composer.For(subject).Add(new TestCapability("test"));
        
        // Act - First call should succeed
        var bag1 = builder.Build();
        
        // Assert - Second call should throw
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("Build() can only be called once", ex.Message);
    }

    [Fact]
    public void Builder_AfterBuild_IsUnusable()
    {
        // Arrange
        var subject = new TestSubject();
        var builder = Composer.For(subject);
        var bag = builder.Build();
        
        // Act & Assert - All builder methods should throw after Build()
        Assert.Throws<InvalidOperationException>(() => 
            builder.Add(new TestCapability("test")));
            
        Assert.Throws<InvalidOperationException>(() => 
            builder.AddAs<ITestContract>(new ConcreteTestCapability("test")));
    }

    [Fact]
    public void Add_NullCapability_ThrowsArgumentNull()
    {
        // Arrange
        var subject = new TestSubject();
        var builder = Composer.For(subject);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            builder.Add<TestCapability>(null!));
    }

    [Fact]
    public void AddAs_NullCapability_ThrowsArgumentNull()
    {
        // Arrange
        var subject = new TestSubject();
        var builder = Composer.For(subject);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            builder.AddAs<ITestContract>(null!));
    }

    [Fact]
    public void AddAs_ContractRetrieval_WorksCorrectly()
    {
        // Arrange
        var subject = new TestSubject();
        var concreteCap = new ConcreteTestCapability("contract-test");
        
        var bag = Composer.For(subject)
            .AddAs<ITestContract>(concreteCap)
            .Build();

        // Act - Retrieve by contract interface
        var success = bag.TryGet<ITestContract>(out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(concreteCap, result);
        Assert.Equal("contract-test", result.GetValue());
    }

    [Fact]
    public void AddAs_ExactTypeMatching_OnlyFindsContractType()
    {
        // Arrange
        var subject = new TestSubject();
        var concreteCap = new ConcreteTestCapability("test");
        
        var bag = Composer.For(subject)
            .AddAs<ITestContract>(concreteCap)  // Register as interface
            .Build();

        // Act & Assert
        Assert.True(bag.TryGet<ITestContract>(out _));  // Found by contract
        Assert.False(bag.TryGet<ConcreteTestCapability>(out _)); // NOT found by concrete type
    }

    [Fact]
    public void Subject_Property_ReturnsCorrectSubject()
    {
        // Arrange
        var subject = new TestSubject { Name = "Builder Test" };
        var builder = Composer.For(subject);

        // Act & Assert
        Assert.Equal(subject, builder.Subject);
        Assert.Equal("Builder Test", builder.Subject.Name);
    }
}