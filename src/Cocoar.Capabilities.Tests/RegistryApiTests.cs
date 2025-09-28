namespace Cocoar.Capabilities.Tests;

public class RegistryApiTests
{
    private sealed record Subject(int Id);
    private sealed record Cap(string Name) ;

    [Fact]
    public void CompositionRegistry_GetRequired_ThrowsWhenMissing()
    {
        using var scope = new CapabilityScope();
        var subject = new Subject(1);
        Assert.Throws<InvalidOperationException>(() => scope.Compositions.GetRequired(subject));
    }

    [Fact]
    public void ComposerRegistry_GetRequired_ThrowsWhenMissing()
    {
        using var scope = new CapabilityScope();
        var subject = new Subject(2);
        Assert.Throws<InvalidOperationException>(() => scope.Composers.GetRequired(subject));
    }

    [Fact]
    public void Remove_ReturnsFalseWhenAbsent()
    {
        using var scope = new CapabilityScope();
        var subject = new Subject(3);
        Assert.False(scope.Compositions.Remove(subject));
    }

    [Fact]
    public void Remove_ReturnsTrueWhenPresent()
    {
        using var scope = new CapabilityScope();
        var subject = new Subject(4);
        scope.For(subject).Add(new Cap("X")).Build(useRegistry: true);
        Assert.True(scope.Compositions.Remove(subject));
        Assert.False(scope.Compositions.TryGet(subject, out _));
    }
}
