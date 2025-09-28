using Cocoar.Capabilities;
using Cocoar.Capabilities.Extensions;

namespace Cocoar.Capabilities.Tests;

// Test subject types
public class TestSubject
{
    public string Name { get; set; } = "Test";
}

public class DatabaseConfig
{
    public string ConnectionString { get; set; } = "Server=localhost;Database=Test;";
}

// Test capability types
public record TestCapability(string Value) : ICapability<TestSubject>;
public record AnotherTestCapability(int Number) : ICapability<TestSubject>;
public record OrderedCapability(int Order, string Name) : ICapability<TestSubject>, IOrderedCapability;
public record DatabaseCapability(string Type) : ICapability<DatabaseConfig>;

// Interface for testing AddAs<T>() functionality
public interface ITestContract : ICapability<TestSubject> 
{
    string GetValue();
}

public record ConcreteTestCapability(string Value) : ICapability<TestSubject>, ITestContract
{
    public string GetValue() => Value;
}

public class CapabilityBagTests
{
    [Fact]
    public void Constructor_ValidSubject_SetsSubjectProperty()
    {
        // Arrange
        var subject = new TestSubject { Name = "Test Subject" };
        
        // Act
        var bag = Composer.For(subject).Build();
        
        // Assert
        Assert.Equal(subject, bag.Subject);
        Assert.Equal("Test Subject", bag.Subject.Name);
    }

    [Fact]
    public void Constructor_NullSubject_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
        {
            Composer.For<TestSubject>(null!);
        });
    }

    [Fact]
    public void TryGet_ExactTypeMatching_ReturnsCorrectCapability()
    {
        // Arrange
        var subject = new TestSubject();
        var capability = new TestCapability("test-value");
        var bag = Composer.For(subject)
            .Add(capability)
            .Build();

        // Act
        var success = bag.TryGet<TestCapability>(out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(capability, result);
        Assert.Equal("test-value", result.Value);
    }

    [Fact]
    public void TryGet_MissingCapability_ReturnsFalse()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject).Build();

        // Act
        var success = bag.TryGet<TestCapability>(out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void GetRequired_ExistingCapability_ReturnsCapability()
    {
        // Arrange
        var subject = new TestSubject();
        var capability = new TestCapability("required-value");
        var bag = Composer.For(subject)
            .Add(capability)
            .Build();

        // Act
        var result = bag.GetRequired<TestCapability>();

        // Assert
        Assert.Equal(capability, result);
        Assert.Equal("required-value", result.Value);
    }

    [Fact]
    public void GetRequired_MissingCapability_ThrowsWithClearMessage()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject)
            .Add(new AnotherTestCapability(42))
            .Build();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
        {
            bag.GetRequired<TestCapability>();
        });

        // Verify the error message contains helpful information
        Assert.Contains("TestCapability", ex.Message);
        Assert.Contains("TestSubject", ex.Message);
        Assert.Contains("AnotherTestCapability", ex.Message);
    }

    [Fact]
    public void GetAll_MultipleCapabilities_ReturnsInOrder()
    {
        // Arrange
        var subject = new TestSubject();
        var cap1 = new OrderedCapability(10, "Second");
        var cap2 = new OrderedCapability(5, "First");
        var cap3 = new OrderedCapability(15, "Third");
        
        var bag = Composer.For(subject)
            .Add(cap1)  // Added first but should be second
            .Add(cap2)  // Added second but should be first  
            .Add(cap3)  // Added third and should be third
            .Build();

        // Act
        var results = bag.GetAll<OrderedCapability>();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("First", results[0].Name);   // Order = 5
        Assert.Equal("Second", results[1].Name);  // Order = 10
        Assert.Equal("Third", results[2].Name);   // Order = 15
    }

    [Fact]
    public void Ordering_IOrderedCapability_LowerOrderFirst()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject)
            .Add(new OrderedCapability(100, "Last"))
            .Add(new OrderedCapability(0, "First"))
            .Add(new OrderedCapability(50, "Middle"))
            .Build();

        // Act
        var results = bag.GetAll<OrderedCapability>();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(0, results[0].Order);
        Assert.Equal(50, results[1].Order);
        Assert.Equal(100, results[2].Order);
    }

    [Fact]
    public void Ordering_SameOrder_InsertionOrderStable()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject)
            .Add(new OrderedCapability(10, "First-10"))
            .Add(new OrderedCapability(10, "Second-10"))
            .Add(new OrderedCapability(10, "Third-10"))
            .Build();

        // Act
        var results = bag.GetAll<OrderedCapability>();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("First-10", results[0].Name);
        Assert.Equal("Second-10", results[1].Name);
        Assert.Equal("Third-10", results[2].Name);
    }

    [Fact]
    public void Ordering_NonOrdered_TreatedAsOrderZero()
    {
        // Arrange
        var subject = new TestSubject();
        var regularCap = new TestCapability("regular");
        var orderedCap = new OrderedCapability(10, "ordered");
        
        // Mix ordered and non-ordered capabilities
        var bag = Composer.For(subject)
            .AddAs<ICapability<TestSubject>>(orderedCap)
            .AddAs<ICapability<TestSubject>>(regularCap)
            .Build();

        // Act
        var results = bag.GetAll<ICapability<TestSubject>>();

        // Assert
        Assert.Equal(2, results.Count);
        // Regular capability (Order = 0 implicit) should come first
        Assert.Equal(regularCap, results[0]);
        Assert.Equal(orderedCap, results[1]);
    }

    [Fact]
    public void Contains_ExistingCapability_ReturnsTrue()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject)
            .Add(new TestCapability("test"))
            .Build();

        // Act & Assert
        Assert.True(bag.Contains<TestCapability>());
    }

    [Fact]
    public void Contains_MissingCapability_ReturnsFalse()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject).Build();

        // Act & Assert
        Assert.False(bag.Contains<TestCapability>());
    }

    [Fact]
    public void Count_MultipleCapabilities_ReturnsCorrectCount()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject)
            .Add(new TestCapability("first"))
            .Add(new TestCapability("second"))
            .Add(new TestCapability("third"))
            .Build();

        // Act & Assert
        Assert.Equal(3, bag.Count<TestCapability>());
    }

    [Fact]
    public void Count_NoCapabilities_ReturnsZero()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject).Build();

        // Act & Assert
        Assert.Equal(0, bag.Count<TestCapability>());
    }

    [Fact]
    public void TotalCapabilityCount_MixedCapabilities_ReturnsCorrectTotal()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject)
            .Add(new TestCapability("test1"))
            .Add(new TestCapability("test2"))
            .Add(new AnotherTestCapability(1))
            .Add(new AnotherTestCapability(2))
            .Add(new AnotherTestCapability(3))
            .Build();

        // Act & Assert
        Assert.Equal(5, bag.TotalCapabilityCount);
    }

    [Fact]
    public void GetAll_EmptyResult_ReturnsArrayEmpty()
    {
        // Arrange
        var subject = new TestSubject();
        var bag = Composer.For(subject).Build();

        // Act
        var results = bag.GetAll<TestCapability>();

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
        // Verify it's actually Array.Empty (zero allocation)
        Assert.Same(Array.Empty<TestCapability>(), results);
    }
}
