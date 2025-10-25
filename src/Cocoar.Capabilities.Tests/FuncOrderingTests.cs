using Xunit;

namespace Cocoar.Capabilities.Tests;

public class FuncOrderingTests
{
    private class HasOrder
    {
        public int Order { get; set; }
    }

    private class Subject { }

    [Fact]
    public void Add_WithFuncOrderSelector_UsesComputedOrder()
    {
        // Arrange
        var scope = new CapabilityScope();
        var subject = new Subject();
        var cap1 = new HasOrder { Order = 100 };
        var cap2 = new HasOrder { Order = 50 };
        var cap3 = new HasOrder { Order = 200 };

        // Act
        var composition = scope.Compose(subject)
            .Add(cap1, c => ((HasOrder)c).Order)
            .Add(cap2, c => ((HasOrder)c).Order)
            .Add(cap3, c => ((HasOrder)c).Order)
            .Build();

        // Assert
        var all = composition.GetAll<HasOrder>();
        Assert.Collection(all,
            c => Assert.Equal(50, c.Order),
            c => Assert.Equal(100, c.Order),
            c => Assert.Equal(200, c.Order));
    }

    [Fact]
    public void AddAs_WithFuncOrderSelector_UsesComputedOrder()
    {
        // Arrange
        var scope = new CapabilityScope();
        var subject = new Subject();
        var cap1 = new HasOrder { Order = 30 };
        var cap2 = new HasOrder { Order = 10 };
        var cap3 = new HasOrder { Order = 20 };

        // Act
        var composition = scope.Compose(subject)
            .AddAs<object>(cap1, c => ((HasOrder)c).Order)
            .AddAs<object>(cap2, c => ((HasOrder)c).Order)
            .AddAs<object>(cap3, c => ((HasOrder)c).Order)
            .Build();

        // Assert
        var all = composition.GetAll<object>();
        Assert.Collection(all,
            c => Assert.Equal(10, ((HasOrder)c).Order),
            c => Assert.Equal(20, ((HasOrder)c).Order),
            c => Assert.Equal(30, ((HasOrder)c).Order));
    }

    [Fact]
    public void TryAdd_WithFuncOrderSelector_UsesComputedOrder()
    {
        // Arrange
        var scope = new CapabilityScope();
        var subject = new Subject();
        var cap1 = new HasOrder { Order = 5 };

        // Act - Only first TryAdd succeeds since type already exists
        var composition = scope.Compose(subject)
            .TryAdd(cap1, c => ((HasOrder)c).Order)
            .Build();

        // Assert - Only cap1 was added
        var all = composition.GetAll<HasOrder>();
        Assert.Collection(all,
            c => Assert.Equal(5, c.Order));
    }

    [Fact]
    public void TryAddAs_WithFuncOrderSelector_UsesComputedOrder()
    {
        // Arrange
        var scope = new CapabilityScope();
        var subject = new Subject();
        var cap1 = new HasOrder { Order = 40 };

        // Act - Only first TryAddAs succeeds since type already exists
        var composition = scope.Compose(subject)
            .TryAddAs<object>(cap1, c => ((HasOrder)c).Order)
            .Build();

        // Assert - Only cap1 was added
        var all = composition.GetAll<object>();
        Assert.Collection(all,
            c => Assert.Equal(40, ((HasOrder)c).Order));
    }

    [Fact]
    public void FuncOrderSelector_CanUseCustomLogic()
    {
        // Arrange
        var scope = new CapabilityScope();
        var subject = new Subject();
        var cap1 = new HasOrder { Order = 1 };
        var cap2 = new HasOrder { Order = 2 };
        var cap3 = new HasOrder { Order = 3 };

        // Act - reverse the order
        var composition = scope.Compose(subject)
            .Add(cap1, c => -((HasOrder)c).Order)
            .Add(cap2, c => -((HasOrder)c).Order)
            .Add(cap3, c => -((HasOrder)c).Order)
            .Build();

        // Assert - should be reversed
        var all = composition.GetAll<HasOrder>();
        Assert.Collection(all,
            c => Assert.Equal(3, c.Order),
            c => Assert.Equal(2, c.Order),
            c => Assert.Equal(1, c.Order));
    }
}
