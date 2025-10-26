namespace Cocoar.Capabilities.Tests;

public class CompositionUsingExtensionsTests
{
    private interface ITestCapability
    {
        void Execute();
        int GetValue();
    }

    private class TestCapability : ITestCapability
    {
        public int Value { get; }
        public bool WasExecuted { get; private set; }

        public TestCapability(int value)
        {
            Value = value;
        }

        public void Execute()
        {
            WasExecuted = true;
        }

        public int GetValue() => Value;
    }

    [Fact]
    public void UsingFirst_WithAction_ExecutesActionAndReturnsComposition()
    {
        using var scope = new CapabilityScope();
        var capability = new TestCapability(42);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(capability)
            .Build();

        var result = composition.UsingFirst<ITestCapability>(cap => cap.Execute());

        Assert.Same(composition, result);
        Assert.True(capability.WasExecuted);
    }

    [Fact]
    public void UsingFirst_WithFunc_ReturnsResult()
    {
        using var scope = new CapabilityScope();
        var capability = new TestCapability(42);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(capability)
            .Build();

        var result = composition.UsingFirst<ITestCapability, int>(cap => cap.GetValue());

        Assert.Equal(42, result);
    }

    [Fact]
    public void UsingFirst_WhenCapabilityMissing_Throws()
    {
        using var scope = new CapabilityScope();
        var composition = scope.Compose(new object()).Build();

        Assert.Throws<InvalidOperationException>(() =>
            composition.UsingFirst<ITestCapability>(cap => cap.Execute()));
    }

    [Fact]
    public void UsingFirst_IsChainable()
    {
        using var scope = new CapabilityScope();
        var cap1 = new TestCapability(1);
        var cap2 = new TestCapability(2);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(cap1)
            .AddAs<ITestCapability>(cap2)
            .Build();

        var result = composition
            .UsingFirst<ITestCapability>(cap => cap.Execute())
            .UsingLast<ITestCapability>(cap => cap.Execute());

        Assert.Same(composition, result);
        Assert.True(cap1.WasExecuted);
        Assert.True(cap2.WasExecuted);
    }

    [Fact]
    public void UsingFirstOrDefault_WhenCapabilityExists_ExecutesAction()
    {
        using var scope = new CapabilityScope();
        var capability = new TestCapability(42);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(capability)
            .Build();

        var result = composition.UsingFirstOrDefault<ITestCapability>(cap => cap.Execute());

        Assert.Same(composition, result);
        Assert.True(capability.WasExecuted);
    }

    [Fact]
    public void UsingFirstOrDefault_WhenCapabilityMissing_DoesNotThrow()
    {
        using var scope = new CapabilityScope();
        var composition = scope.Compose(new object()).Build();
        var executed = false;

        var result = composition.UsingFirstOrDefault<ITestCapability>(cap => executed = true);

        Assert.Same(composition, result);
        Assert.False(executed);
    }

    [Fact]
    public void UsingLast_WithAction_ExecutesActionAndReturnsComposition()
    {
        using var scope = new CapabilityScope();
        var capability = new TestCapability(42);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(capability)
            .Build();

        var result = composition.UsingLast<ITestCapability>(cap => cap.Execute());

        Assert.Same(composition, result);
        Assert.True(capability.WasExecuted);
    }

    [Fact]
    public void UsingLast_WithFunc_ReturnsResult()
    {
        using var scope = new CapabilityScope();
        var cap1 = new TestCapability(1);
        var cap2 = new TestCapability(2);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(cap1, order: 1)
            .AddAs<ITestCapability>(cap2, order: 2)
            .Build();

        var result = composition.UsingLast<ITestCapability, int>(cap => cap.GetValue());

        Assert.Equal(2, result);
    }

    [Fact]
    public void UsingLast_WhenCapabilityMissing_Throws()
    {
        using var scope = new CapabilityScope();
        var composition = scope.Compose(new object()).Build();

        Assert.Throws<InvalidOperationException>(() =>
            composition.UsingLast<ITestCapability>(cap => cap.Execute()));
    }

    [Fact]
    public void UsingLastOrDefault_WhenCapabilityExists_ExecutesAction()
    {
        using var scope = new CapabilityScope();
        var capability = new TestCapability(42);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(capability)
            .Build();

        var result = composition.UsingLastOrDefault<ITestCapability>(cap => cap.Execute());

        Assert.Same(composition, result);
        Assert.True(capability.WasExecuted);
    }

    [Fact]
    public void UsingLastOrDefault_WhenCapabilityMissing_DoesNotThrow()
    {
        using var scope = new CapabilityScope();
        var composition = scope.Compose(new object()).Build();
        var executed = false;

        var result = composition.UsingLastOrDefault<ITestCapability>(cap => executed = true);

        Assert.Same(composition, result);
        Assert.False(executed);
    }

    [Fact]
    public void UsingEach_WithAction_ExecutesForEachCapability()
    {
        using var scope = new CapabilityScope();
        var cap1 = new TestCapability(1);
        var cap2 = new TestCapability(2);
        var cap3 = new TestCapability(3);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(cap1)
            .AddAs<ITestCapability>(cap2)
            .AddAs<ITestCapability>(cap3)
            .Build();

        var result = composition.UsingEach<ITestCapability>(cap => cap.Execute());

        Assert.Same(composition, result);
        Assert.True(cap1.WasExecuted);
        Assert.True(cap2.WasExecuted);
        Assert.True(cap3.WasExecuted);
    }

    [Fact]
    public void UsingEach_WithFunc_CollectsResults()
    {
        using var scope = new CapabilityScope();
        var cap1 = new TestCapability(1);
        var cap2 = new TestCapability(2);
        var cap3 = new TestCapability(3);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(cap1, order: 1)
            .AddAs<ITestCapability>(cap2, order: 2)
            .AddAs<ITestCapability>(cap3, order: 3)
            .Build();

        var results = composition.UsingEach<ITestCapability, int>(cap => cap.GetValue());

        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0]);
        Assert.Equal(2, results[1]);
        Assert.Equal(3, results[2]);
    }

    [Fact]
    public void UsingEach_WithAction_WhenEmpty_DoesNotThrow()
    {
        using var scope = new CapabilityScope();
        var composition = scope.Compose(new object()).Build();
        var executed = false;

        var result = composition.UsingEach<ITestCapability>(cap => executed = true);

        Assert.Same(composition, result);
        Assert.False(executed);
    }

    [Fact]
    public void UsingEach_WithFunc_WhenEmpty_ReturnsEmptyList()
    {
        using var scope = new CapabilityScope();
        var composition = scope.Compose(new object()).Build();

        var results = composition.UsingEach<ITestCapability, int>(cap => cap.GetValue());

        Assert.Empty(results);
    }

    [Fact]
    public void UsingEach_IsChainable()
    {
        using var scope = new CapabilityScope();
        var cap1 = new TestCapability(1);
        var cap2 = new TestCapability(2);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(cap1)
            .AddAs<ITestCapability>(cap2)
            .Build();

        var result = composition
            .UsingEach<ITestCapability>(cap => cap.Execute())
            .UsingFirst<ITestCapability>(cap => { });

        Assert.Same(composition, result);
        Assert.True(cap1.WasExecuted);
        Assert.True(cap2.WasExecuted);
    }

    [Fact]
    public void UsingAll_WithAction_ReceivesFullCollection()
    {
        using var scope = new CapabilityScope();
        var cap1 = new TestCapability(1);
        var cap2 = new TestCapability(2);
        var cap3 = new TestCapability(3);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(cap1)
            .AddAs<ITestCapability>(cap2)
            .AddAs<ITestCapability>(cap3)
            .Build();

        IReadOnlyList<ITestCapability>? receivedCollection = null;

        var result = composition.UsingAll<ITestCapability>(caps =>
        {
            receivedCollection = caps;
        });

        Assert.Same(composition, result);
        Assert.NotNull(receivedCollection);
        Assert.Equal(3, receivedCollection.Count);
    }

    [Fact]
    public void UsingAll_WithFunc_ReturnsAggregatedResult()
    {
        using var scope = new CapabilityScope();
        var cap1 = new TestCapability(1);
        var cap2 = new TestCapability(2);
        var cap3 = new TestCapability(3);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(cap1)
            .AddAs<ITestCapability>(cap2)
            .AddAs<ITestCapability>(cap3)
            .Build();

        var sum = composition.UsingAll<ITestCapability, int>(caps =>
            caps.Sum(c => c.GetValue()));

        Assert.Equal(6, sum);
    }

    [Fact]
    public void UsingAll_WithAction_WhenEmpty_ExecutesWithEmptyCollection()
    {
        using var scope = new CapabilityScope();
        var composition = scope.Compose(new object()).Build();
        IReadOnlyList<ITestCapability>? receivedCollection = null;

        var result = composition.UsingAll<ITestCapability>(caps =>
        {
            receivedCollection = caps;
        });

        Assert.Same(composition, result);
        Assert.NotNull(receivedCollection);
        Assert.Empty(receivedCollection);
    }

    [Fact]
    public void UsingAll_WithFunc_WhenEmpty_ExecutesWithEmptyCollection()
    {
        using var scope = new CapabilityScope();
        var composition = scope.Compose(new object()).Build();

        var count = composition.UsingAll<ITestCapability, int>(caps => caps.Count);

        Assert.Equal(0, count);
    }

    [Fact]
    public void UsingFirst_NullComposition_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IComposition)null!).UsingFirst<ITestCapability>(cap => { }));
    }

    [Fact]
    public void UsingFirst_NullAction_Throws()
    {
        using var scope = new CapabilityScope();
        var composition = scope.Compose(new object()).Build();

        Assert.Throws<ArgumentNullException>(() =>
            composition.UsingFirst<ITestCapability>(null!));
    }

    [Fact]
    public void UsingEach_NullComposition_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IComposition)null!).UsingEach<ITestCapability>(cap => { }));
    }

    [Fact]
    public void UsingEach_NullAction_Throws()
    {
        using var scope = new CapabilityScope();
        var composition = scope.Compose(new object()).Build();

        Assert.Throws<ArgumentNullException>(() =>
            composition.UsingEach<ITestCapability>((Action<ITestCapability>)null!));
    }

    [Fact]
    public void UsingAll_NullComposition_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IComposition)null!).UsingAll<ITestCapability>(caps => { }));
    }

    [Fact]
    public void UsingAll_NullAction_Throws()
    {
        using var scope = new CapabilityScope();
        var composition = scope.Compose(new object()).Build();

        Assert.Throws<ArgumentNullException>(() =>
            composition.UsingAll<ITestCapability>((Action<IReadOnlyList<ITestCapability>>)null!));
    }

    [Fact]
    public void ComplexChain_MixedUsingMethods_WorksCorrectly()
    {
        using var scope = new CapabilityScope();
        var cap1 = new TestCapability(1);
        var cap2 = new TestCapability(2);
        var cap3 = new TestCapability(3);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(cap1, order: 1)
            .AddAs<ITestCapability>(cap2, order: 2)
            .AddAs<ITestCapability>(cap3, order: 3)
            .Build();

        var executionLog = new List<string>();

        var result = composition
            .UsingFirst<ITestCapability>(cap => executionLog.Add($"First: {cap.GetValue()}"))
            .UsingEach<ITestCapability>(cap => executionLog.Add($"Each: {cap.GetValue()}"))
            .UsingLast<ITestCapability>(cap => executionLog.Add($"Last: {cap.GetValue()}"))
            .UsingAll<ITestCapability>(caps => executionLog.Add($"All count: {caps.Count}"));

        Assert.Same(composition, result);
        Assert.Equal(6, executionLog.Count);
        Assert.Equal("First: 1", executionLog[0]);
        Assert.Equal("Each: 1", executionLog[1]);
        Assert.Equal("Each: 2", executionLog[2]);
        Assert.Equal("Each: 3", executionLog[3]);
        Assert.Equal("Last: 3", executionLog[4]);
        Assert.Equal("All count: 3", executionLog[5]);
    }

    [Fact]
    public void ChainWithOrDefault_WhenMissing_ContinuesChain()
    {
        using var scope = new CapabilityScope();
        var cap = new TestCapability(42);
        var composition = scope.Compose(new object())
            .AddAs<ITestCapability>(cap)
            .Build();

        var executed = false;

        var result = composition
            .UsingFirstOrDefault<object>(obj => { })
            .UsingFirst<ITestCapability>(c => executed = true);

        Assert.Same(composition, result);
        Assert.True(executed);
    }
}
