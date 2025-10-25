using Xunit;

namespace Cocoar.Capabilities.Tests;

public sealed class SingleTestApproach : IDisposable
{
    private CapabilityScope? _scope;

    public void Dispose()
    {
        _scope?.Dispose();
    }

    [Fact]
    public void Test01_DisabledScope_EnableCompositionRegistry_ShouldWork()
    {
        // Arrange: Create scope with composition registry disabled
        _scope = new CapabilityScope(new CapabilityScopeOptions 
        { 
            UseComposerRegistry = false, 
            UseCompositionRegistry = false 
        });
        
        var subject = new StringSubject("test");

        // Act: Build with explicit override to enable composition registry
        var composition = _scope.Compose(subject)
            .Build(useRegistry: true);

        // Assert: Composition should be findable in registry
        var foundComposition = _scope.Compositions.GetOrDefault(subject);
        
        // For now, let's just see what happens
        var isFound = foundComposition != null;
        var isSameInstance = foundComposition == composition;
        
        // Debug info
    Assert.True(isFound, "Composition not found after enabling registry override at build time.");
        
        if (isFound)
        {
            Assert.True(isSameInstance, "Found composition should be same instance");
        }
    }
}
