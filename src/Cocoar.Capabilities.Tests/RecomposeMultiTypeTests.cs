using Xunit;

namespace Cocoar.Capabilities.Tests;

public class RecomposeMultiTypeTests
{
    public interface IMyInterface { }
    public record LoggingCap(int Value) : IMyInterface;
    public record DatabaseCap(int Value) : IMyInterface;
    public record TestSubject(int Id);

    [Fact]
    public void Recompose_WithMultiTypeRegistration_ShouldNotCreateDuplicates()
    {
        using var scope = new CapabilityScope();
        var subject = new TestSubject(1);
        
        // Register capabilities under multiple types with explicit order
        var composer = scope.For(subject);
        composer.AddAs<(IMyInterface, LoggingCap)>(new LoggingCap(10), order: 10);
        composer.AddAs<(IMyInterface, DatabaseCap)>(new DatabaseCap(5), order: 5);
        composer.AddAs<(IMyInterface, LoggingCap)>(new LoggingCap(1), order: 1);
        composer.AddAs<(IMyInterface, DatabaseCap)>(new DatabaseCap(2), order: 2);
        var initialComposition = composer.Build();
        
        // Verify initial composition
        var initialByInterface = initialComposition.GetAll<IMyInterface>();
        Assert.Equal(4, initialByInterface.Count);
        Assert.Equal(1, ((LoggingCap)initialByInterface[0]).Value);
        Assert.Equal(2, ((DatabaseCap)initialByInterface[1]).Value);
        Assert.Equal(5, ((DatabaseCap)initialByInterface[2]).Value);
        Assert.Equal(10, ((LoggingCap)initialByInterface[3]).Value);
        
        // Recompose
        var recomposer = scope.Recompose(initialComposition);
        var recomposedComposition = recomposer.Build();
        
        // Check all type queries
        var recomposedByInterface = recomposedComposition.GetAll<IMyInterface>();
        var recomposedByLogging = recomposedComposition.GetAll<LoggingCap>();
        var recomposedByDatabase = recomposedComposition.GetAll<DatabaseCap>();
        
        // Debug output
        System.Diagnostics.Debug.WriteLine($"IMyInterface count: {recomposedByInterface.Count}");
        System.Diagnostics.Debug.WriteLine($"LoggingCap count: {recomposedByLogging.Count}");
        System.Diagnostics.Debug.WriteLine($"DatabaseCap count: {recomposedByDatabase.Count}");
        System.Diagnostics.Debug.WriteLine($"Total: {recomposedComposition.TotalCapabilityCount}");
        
        // SHOULD NOT have duplicates
        Assert.Equal(4, recomposedByInterface.Count); // Expected 4
        Assert.Equal(2, recomposedByLogging.Count); // Expected 2
        Assert.Equal(2, recomposedByDatabase.Count); // Expected 2
        Assert.Equal(4, recomposedComposition.TotalCapabilityCount); // Expected 4 unique capabilities
        
        // SHOULD preserve order
        Assert.Equal(1, ((LoggingCap)recomposedByInterface[0]).Value);
        Assert.Equal(2, ((DatabaseCap)recomposedByInterface[1]).Value);
        Assert.Equal(5, ((DatabaseCap)recomposedByInterface[2]).Value);
        Assert.Equal(10, ((LoggingCap)recomposedByInterface[3]).Value);
    }
}
