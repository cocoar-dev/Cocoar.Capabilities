namespace Cocoar.Capabilities.Core.Tests;

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

// Additional test classes for primary capabilities
public record PrimaryTestCapability(string Value) : ICapability<TestSubject>, IPrimaryCapability<TestSubject>;
public record SecondPrimaryTestCapability(string Value) : ICapability<TestSubject>, IPrimaryCapability<TestSubject>;

// Mock composition that implements IComposition<T> but is NOT the internal Composition<T> type
// This is used to test the defensive validation in the Recompose functionality
public class MockComposition : IComposition<TestSubject>
{
    private readonly TestSubject _subject;
    
    public MockComposition(TestSubject subject)
    {
        _subject = subject;
    }

    public TestSubject Subject => _subject;
    object IComposition.Subject => _subject;
    public int TotalCapabilityCount => 0;

    public bool HasPrimary() => false;
    public bool HasPrimary<TPrimaryCapability>() where TPrimaryCapability : class, IPrimaryCapability<TestSubject> => false;
    public bool TryGetPrimary(out IPrimaryCapability<TestSubject> primary)
    {
        primary = null!;
        return false;
    }
    public IPrimaryCapability<TestSubject>? GetPrimaryOrDefault() => null;
    public IPrimaryCapability<TestSubject> GetPrimary() => throw new InvalidOperationException();
    public bool TryGetPrimaryAs<TPrimaryCapability>(out TPrimaryCapability primary) where TPrimaryCapability : class, IPrimaryCapability<TestSubject>
    {
        primary = null!;
        return false;
    }
    public TPrimaryCapability? GetPrimaryOrDefaultAs<TPrimaryCapability>() where TPrimaryCapability : class, IPrimaryCapability<TestSubject> => null;
    public TPrimaryCapability GetRequiredPrimaryAs<TPrimaryCapability>() where TPrimaryCapability : class, IPrimaryCapability<TestSubject> => throw new InvalidOperationException();
    public IReadOnlyList<TCapability> GetAll<TCapability>() where TCapability : class, ICapability<TestSubject> => new List<TCapability>();
    public IReadOnlyList<ICapability<TestSubject>> GetAll() => new List<ICapability<TestSubject>>();
    public bool Has<TCapability>() where TCapability : class, ICapability<TestSubject> => false;
    public int Count<TCapability>() where TCapability : class, ICapability<TestSubject> => 0;
}