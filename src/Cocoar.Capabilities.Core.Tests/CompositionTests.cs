namespace Cocoar.Capabilities.Core.Tests;

public class CompositionTests
{
    [Fact]
    public void Constructor_ValidSubject_SetsSubjectProperty()
    {
        
        var subject = new TestSubject { Name = "Test Subject" };
        
        
        var composition = Composer.For(subject).Build();
        
        
        Assert.Equal(subject, composition.Subject);
        Assert.Equal("Test Subject", composition.Subject.Name);
    }

    [Fact]
    public void Constructor_NullSubject_ThrowsArgumentNullException()
    {
        
        Assert.Throws<ArgumentNullException>(() => 
        {
            Composer.For<TestSubject>(null!);
        });
    }

    [Fact]
    public void TryGet_ExactTypeMatching_ReturnsCorrectCapability()
    {
        
        var subject = new TestSubject();
        var capability = new TestCapability("test-value");
        var composition = Composer.For(subject)
            .Add(capability)
            .Build();

        
        var success = composition.TryGet<TestCapability>(out var result);

        
        Assert.True(success);
        Assert.Equal(capability, result);
        Assert.Equal("test-value", result.Value);
    }

    [Fact]
    public void TryGet_MissingCapability_ReturnsFalse()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject).Build();

        
        var success = composition.TryGet<TestCapability>(out var result);

        
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void GetRequired_ExistingCapability_ReturnsCapability()
    {
        
        var subject = new TestSubject();
        var capability = new TestCapability("required-value");
        var composition = Composer.For(subject)
            .Add(capability)
            .Build();

        
        var result = composition.GetRequired<TestCapability>();

        
        Assert.Equal(capability, result);
        Assert.Equal("required-value", result.Value);
    }

    [Fact]
    public void GetRequired_MissingCapability_ThrowsWithClearMessage()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .Add(new AnotherTestCapability(42))
            .Build();

        
        var ex = Assert.Throws<InvalidOperationException>(() => 
        {
            composition.GetRequired<TestCapability>();
        });

        Assert.Contains("TestCapability", ex.Message);
        Assert.Contains("TestSubject", ex.Message);
        Assert.Contains("AnotherTestCapability", ex.Message);
    }

    [Fact]
    public void GetAll_MultipleCapabilities_ReturnsInOrder()
    {
        
        var subject = new TestSubject();
        var cap1 = new OrderedCapability(10, "Second");
        var cap2 = new OrderedCapability(5, "First");
        var cap3 = new OrderedCapability(15, "Third");
        
        var composition = Composer.For(subject)
            .Add(cap1)  // Added first but should be second
            .Add(cap2)  // Added second but should be first  
            .Add(cap3)  // Added third and should be third
            .Build();

        
        var results = composition.GetAll<OrderedCapability>();

        
        Assert.Equal(3, results.Count);
        Assert.Equal("First", results[0].Name);   // Order = 5
        Assert.Equal("Second", results[1].Name);  // Order = 10
        Assert.Equal("Third", results[2].Name);   // Order = 15
    }

    [Fact]
    public void Ordering_IOrderedCapability_LowerOrderFirst()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .Add(new OrderedCapability(100, "Last"))
            .Add(new OrderedCapability(0, "First"))
            .Add(new OrderedCapability(50, "Middle"))
            .Build();

        
        var results = composition.GetAll<OrderedCapability>();

        
        Assert.Equal(3, results.Count);
        Assert.Equal(0, results[0].Order);
        Assert.Equal(50, results[1].Order);
        Assert.Equal(100, results[2].Order);
    }

    [Fact]
    public void Ordering_SameOrder_InsertionOrderStable()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .Add(new OrderedCapability(10, "First-10"))
            .Add(new OrderedCapability(10, "Second-10"))
            .Add(new OrderedCapability(10, "Third-10"))
            .Build();

        
        var results = composition.GetAll<OrderedCapability>();

        
        Assert.Equal(3, results.Count);
        Assert.Equal("First-10", results[0].Name);
        Assert.Equal("Second-10", results[1].Name);
        Assert.Equal("Third-10", results[2].Name);
    }

    [Fact]
    public void Ordering_NonOrdered_TreatedAsOrderZero()
    {
        
        var subject = new TestSubject();
        var regularCap = new TestCapability("regular");
        var orderedCap = new OrderedCapability(10, "ordered");
        
        // Mix ordered and non-ordered capabilities
        var composition = Composer.For(subject)
            .AddAs<ICapability<TestSubject>>(orderedCap)
            .AddAs<ICapability<TestSubject>>(regularCap)
            .Build();

        
        var results = composition.GetAll<ICapability<TestSubject>>();

        
        Assert.Equal(2, results.Count);
        // Regular capability (Order = 0 implicit) should come first
        Assert.Equal(regularCap, results[0]);
        Assert.Equal(orderedCap, results[1]);
    }

    [Fact]
    public void Contains_ExistingCapability_ReturnsTrue()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .Add(new TestCapability("test"))
            .Build();

        
        Assert.True(composition.Has<TestCapability>());
    }

    [Fact]
    public void Contains_MissingCapability_ReturnsFalse()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject).Build();

        
        Assert.False(composition.Has<TestCapability>());
    }

    [Fact]
    public void Count_MultipleCapabilities_ReturnsCorrectCount()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .Add(new TestCapability("first"))
            .Add(new TestCapability("second"))
            .Add(new TestCapability("third"))
            .Build();

        
        Assert.Equal(3, composition.Count<TestCapability>());
    }

    [Fact]
    public void Count_NoCapabilities_ReturnsZero()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject).Build();

        
        Assert.Equal(0, composition.Count<TestCapability>());
    }

    [Fact]
    public void TotalCapabilityCount_MixedCapabilities_ReturnsCorrectTotal()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .Add(new TestCapability("test1"))
            .Add(new TestCapability("test2"))
            .Add(new AnotherTestCapability(1))
            .Add(new AnotherTestCapability(2))
            .Add(new AnotherTestCapability(3))
            .Build();

        
        Assert.Equal(5, composition.TotalCapabilityCount);
    }

    [Fact]
    public void GetAll_EmptyResult_ReturnsArrayEmpty()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject).Build();

        
        var results = composition.GetAll<TestCapability>();

        
        Assert.NotNull(results);
        Assert.Empty(results);
        // Verify it's actually Array.Empty (zero allocation)
        Assert.Same(Array.Empty<TestCapability>(), results);
    }
}
