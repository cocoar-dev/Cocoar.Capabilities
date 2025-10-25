using Xunit;

namespace Cocoar.Capabilities.Tests;

public class OwnerAndAnchorTests
{
    private class TestOwner
    {
        public string Name { get; init; } = string.Empty;
    }

    private class TestAnchor
    {
        public int Value { get; init; }
    }

    private class AnotherAnchor
    {
        public bool Flag { get; init; }
    }

    private record TestCapability(string Name);

    #region Owner Tests

    [Fact]
    public void SetOwner_SetsOwnerSuccessfully()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner { Name = "TestOwner" };

        var result = scope.Owner.Set(owner);

        Assert.Same(scope, result.Scope); // Fluent API
        Assert.True(scope.Owner.TryGet<TestOwner>(out var retrieved));
        Assert.Same(owner, retrieved);
    }

    [Fact]
    public void SetOwner_ThrowsOnNull()
    {
        using var scope = new CapabilityScope();
        
        Assert.Throws<ArgumentNullException>(() => scope.Owner.Set(null!));
    }

    [Fact]
    public void SetOwner_ThrowsWhenOwnerAlreadySet()
    {
        using var scope = new CapabilityScope();
        var owner1 = new TestOwner { Name = "First" };
        var owner2 = new TestOwner { Name = "Second" };
        
        scope.Owner.Set(owner1);
        
        var ex = Assert.Throws<InvalidOperationException>(() => scope.Owner.Set(owner2));
        Assert.Contains("already been set", ex.Message);
        Assert.Contains("Replace()", ex.Message);
    }

    [Fact]
    public void SetOwner_AllowsSettingAfterGarbageCollected()
    {
        using var scope = new CapabilityScope();
        SetOwnerAndCollect(scope);
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Should allow setting a new owner after the old one was GC'd
        var newOwner = new TestOwner { Name = "NewOwner" };
        scope.Owner.Set(newOwner);
        
        Assert.Same(newOwner, scope.Owner.Get<TestOwner>());
    }

    [Fact]
    public void ReplaceOwner_ReplacesExistingOwner()
    {
        using var scope = new CapabilityScope();
        var owner1 = new TestOwner { Name = "First" };
        var owner2 = new TestOwner { Name = "Second" };
        
        scope.Owner.Set(owner1);
        scope.Owner.Replace(owner2);
        
        Assert.Same(owner2, scope.Owner.Get<TestOwner>());
    }

    [Fact]
    public void ReplaceOwner_WorksWhenNoOwnerSet()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner { Name = "Owner" };
        
        scope.Owner.Replace(owner);
        
        Assert.Same(owner, scope.Owner.Get<TestOwner>());
    }

    [Fact]
    public void ReplaceOwner_ThrowsOnNull()
    {
        using var scope = new CapabilityScope();
        
        Assert.Throws<ArgumentNullException>(() => scope.Owner.Replace(null!));
    }

    [Fact]
    public void Owner_Generic_ReturnsCorrectType()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner { Name = "Owner1" };
        scope.Owner.Set(owner);

        var retrieved = scope.Owner.Get<TestOwner>();

        Assert.Same(owner, retrieved);
        Assert.Equal("Owner1", retrieved.Name);
    }

    [Fact]
    public void Owner_Generic_ThrowsWhenNotSet()
    {
        using var scope = new CapabilityScope();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Owner.Get<TestOwner>());
        Assert.Contains("No owner has been set", ex.Message);
    }

    [Fact]
    public void Owner_Generic_ThrowsWhenWrongType()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner();
        scope.Owner.Set(owner);

        var ex = Assert.Throws<InvalidCastException>(() => scope.Owner.Get<TestAnchor>());
        Assert.Contains("TestOwner", ex.Message);
        Assert.Contains("TestAnchor", ex.Message);
    }

    [Fact]
    public void Owner_Generic_ThrowsWhenDisposed()
    {
        var scope = new CapabilityScope();
        scope.Owner.Set(new TestOwner());
        scope.Dispose();

        Assert.Throws<ObjectDisposedException>(() => scope.Owner.Get<TestOwner>());
    }

    [Fact]
    public void TryGetOwner_ReturnsTrueWhenSet()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner { Name = "Owner2" };
        scope.Owner.Set(owner);

        var result = scope.Owner.TryGet<TestOwner>(out var retrieved);

        Assert.True(result);
        Assert.Same(owner, retrieved);
    }

    [Fact]
    public void TryGetOwner_ReturnsFalseWhenNotSet()
    {
        using var scope = new CapabilityScope();

        var result = scope.Owner.TryGet<TestOwner>(out var retrieved);

        Assert.False(result);
        Assert.Null(retrieved);
    }

    [Fact]
    public void TryGetOwner_ReturnsFalseWhenWrongType()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner();
        scope.Owner.Set(owner);

        var result = scope.Owner.TryGet<TestAnchor>(out var retrieved);

        Assert.False(result);
        Assert.Null(retrieved);
    }

    [Fact]
    public void TryGetOwner_ThrowsWhenDisposed()
    {
        var scope = new CapabilityScope();
        scope.Owner.Set(new TestOwner());
        scope.Dispose();

        Assert.Throws<ObjectDisposedException>(() => scope.Owner.TryGet<TestOwner>(out _));
    }

    [Fact]
    public void Owner_ThrowsWhenCollected()
    {
        using var scope = new CapabilityScope();
        SetOwnerAndCollect(scope);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Owner.Get<TestOwner>());
        Assert.Contains("garbage collected", ex.Message);
    }

    [Fact]
    public void TryGetOwner_ReturnsFalseWhenCollected()
    {
        using var scope = new CapabilityScope();
        SetOwnerAndCollect(scope);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var result = scope.Owner.TryGet<TestOwner>(out var retrieved);

        Assert.False(result);
        Assert.Null(retrieved);
    }

    // Helper method to ensure owner is not kept alive by the stack
    private static void SetOwnerAndCollect(CapabilityScope scope)
    {
        scope.Owner.Set(new TestOwner { Name = "Temporary" });
    }

    #endregion

    #region Typed Anchor Tests

    [Fact]
    public void SetAnchor_Generic_SetsAnchorSuccessfully()
    {
        using var scope = new CapabilityScope();
        var anchor = new TestAnchor { Value = 42 };

        var result = scope.Anchors.Set(anchor);

        Assert.Same(scope, result.Scope); // Fluent API
        Assert.True(scope.Anchors.TryGet<TestAnchor>(out var retrieved));
        Assert.Same(anchor, retrieved);
    }

    [Fact]
    public void SetAnchor_Generic_ThrowsOnNull()
    {
        using var scope = new CapabilityScope();
        
        Assert.Throws<ArgumentNullException>(() => scope.Anchors.Set<TestAnchor>(null!));
    }

    [Fact]
    public void SetAnchor_Generic_ThrowsWhenDisposed()
    {
        var scope = new CapabilityScope();
        scope.Dispose();
        
        Assert.Throws<ObjectDisposedException>(() => scope.Anchors.Set(new TestAnchor()));
    }

    [Fact]
    public void SetAnchor_Generic_SupportsMultipleTypes()
    {
        using var scope = new CapabilityScope();
        var anchor1 = new TestAnchor { Value = 42 };
        var anchor2 = new AnotherAnchor { Flag = true };

        scope.Anchors.Set(anchor1);
        scope.Anchors.Set(anchor2);

        Assert.True(scope.Anchors.TryGet<TestAnchor>(out var retrieved1));
        Assert.True(scope.Anchors.TryGet<AnotherAnchor>(out var retrieved2));
        Assert.Same(anchor1, retrieved1);
        Assert.Same(anchor2, retrieved2);
    }

    [Fact]
    public void SetAnchor_Generic_ReplacesExistingAnchorOfSameType()
    {
        using var scope = new CapabilityScope();
        var anchor1 = new TestAnchor { Value = 1 };
        var anchor2 = new TestAnchor { Value = 2 };

        scope.Anchors.Set(anchor1);
        scope.Anchors.Set(anchor2);

        var retrieved = scope.Anchors.GetOrThrow<TestAnchor>();
        Assert.Same(anchor2, retrieved);
        Assert.Equal(2, retrieved.Value);
    }

    [Fact]
    public void GetAnchorOrThrow_Generic_ReturnsCorrectType()
    {
        using var scope = new CapabilityScope();
        var anchor = new TestAnchor { Value = 100 };
        scope.Anchors.Set(anchor);

        var retrieved = scope.Anchors.GetOrThrow<TestAnchor>();

        Assert.Same(anchor, retrieved);
        Assert.Equal(100, retrieved.Value);
    }

    [Fact]
    public void GetAnchorOrThrow_Generic_ThrowsWhenNotSet()
    {
        using var scope = new CapabilityScope();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Anchors.GetOrThrow<TestAnchor>());
        Assert.Contains("No anchor of type TestAnchor", ex.Message);
    }

    [Fact]
    public void GetAnchorOrThrow_Generic_ThrowsWhenDisposed()
    {
        var scope = new CapabilityScope();
        scope.Dispose();

        Assert.Throws<ObjectDisposedException>(() => scope.Anchors.GetOrThrow<TestAnchor>());
    }

    [Fact]
    public void TryGetAnchor_Generic_ReturnsTrueWhenSet()
    {
        using var scope = new CapabilityScope();
        var anchor = new TestAnchor { Value = 200 };
        scope.Anchors.Set(anchor);

        var result = scope.Anchors.TryGet<TestAnchor>(out var retrieved);

        Assert.True(result);
        Assert.Same(anchor, retrieved);
    }

    [Fact]
    public void TryGetAnchor_Generic_ReturnsFalseWhenNotSet()
    {
        using var scope = new CapabilityScope();

        var result = scope.Anchors.TryGet<TestAnchor>(out var retrieved);

        Assert.False(result);
        Assert.Null(retrieved);
    }

    [Fact]
    public void TryGetAnchor_Generic_ThrowsWhenDisposed()
    {
        var scope = new CapabilityScope();
        scope.Dispose();

        Assert.Throws<ObjectDisposedException>(() => scope.Anchors.TryGet<TestAnchor>(out _));
    }

    [Fact]
    public void GetAnchorOrThrow_Generic_ThrowsWhenCollected()
    {
        using var scope = new CapabilityScope();
        SetAnchorAndCollect(scope);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Anchors.GetOrThrow<TestAnchor>());
        Assert.Contains("garbage collected", ex.Message);
    }

    [Fact]
    public void TryGetAnchor_Generic_ReturnsFalseWhenCollected()
    {
        using var scope = new CapabilityScope();
        SetAnchorAndCollect(scope);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var result = scope.Anchors.TryGet<TestAnchor>(out var retrieved);

        Assert.False(result);
        Assert.Null(retrieved);
    }

    private static void SetAnchorAndCollect(CapabilityScope scope)
    {
        scope.Anchors.Set(new TestAnchor { Value = 999 });
    }

    #endregion

    #region Named Anchor Tests

    [Fact]
    public void SetAnchor_Named_SetsAnchorSuccessfully()
    {
        using var scope = new CapabilityScope();
        var anchor = new TestAnchor { Value = 42 };

        var result = scope.Anchors.Set("test-key", anchor);

        Assert.Same(scope, result.Scope); // Fluent API
        Assert.True(scope.Anchors.TryGet("test-key", out var retrieved));
        Assert.Same(anchor, retrieved);
    }

    [Fact]
    public void SetAnchor_Named_ThrowsOnNullKey()
    {
        using var scope = new CapabilityScope();
        
        Assert.Throws<ArgumentNullException>(() => scope.Anchors.Set(null!, new TestAnchor()));
    }

    [Fact]
    public void SetAnchor_Named_ThrowsOnNullAnchor()
    {
        using var scope = new CapabilityScope();
        
        Assert.Throws<ArgumentNullException>(() => scope.Anchors.Set("key", null!));
    }

    [Fact]
    public void SetAnchor_Named_ThrowsWhenDisposed()
    {
        var scope = new CapabilityScope();
        scope.Dispose();
        
        Assert.Throws<ObjectDisposedException>(() => scope.Anchors.Set("key", new TestAnchor()));
    }

    [Fact]
    public void SetAnchor_Named_SupportsMultipleKeys()
    {
        using var scope = new CapabilityScope();
        var anchor1 = new TestAnchor { Value = 1 };
        var anchor2 = new AnotherAnchor { Flag = true };
        var anchor3 = new TestOwner { Name = "test" };

        scope.Anchors.Set("key1", anchor1);
        scope.Anchors.Set("key2", anchor2);
        scope.Anchors.Set("tenant:acme", anchor3);

        Assert.True(scope.Anchors.TryGet("key1", out var retrieved1));
        Assert.True(scope.Anchors.TryGet("key2", out var retrieved2));
        Assert.True(scope.Anchors.TryGet("tenant:acme", out var retrieved3));
        Assert.Same(anchor1, retrieved1);
        Assert.Same(anchor2, retrieved2);
        Assert.Same(anchor3, retrieved3);
    }

    [Fact]
    public void SetAnchor_Named_ReplacesExistingAnchorWithSameKey()
    {
        using var scope = new CapabilityScope();
        var anchor1 = new TestAnchor { Value = 1 };
        var anchor2 = new TestAnchor { Value = 2 };

        scope.Anchors.Set("mykey", anchor1);
        scope.Anchors.Set("mykey", anchor2);

        var retrieved = scope.Anchors.GetOrThrow("mykey");
        Assert.Same(anchor2, retrieved);
    }

    [Fact]
    public void GetAnchorOrThrow_Named_ReturnsCorrectAnchor()
    {
        using var scope = new CapabilityScope();
        var anchor = new TestAnchor { Value = 100 };
        scope.Anchors.Set("environment", anchor);

        var retrieved = scope.Anchors.GetOrThrow("environment");

        Assert.Same(anchor, retrieved);
    }

    [Fact]
    public void GetAnchorOrThrow_Named_ThrowsOnNullKey()
    {
        using var scope = new CapabilityScope();
        
        Assert.Throws<ArgumentNullException>(() => scope.Anchors.GetOrThrow(null!));
    }

    [Fact]
    public void GetAnchorOrThrow_Named_ThrowsWhenNotSet()
    {
        using var scope = new CapabilityScope();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Anchors.GetOrThrow("missing-key"));
        Assert.Contains("No anchor with key 'missing-key'", ex.Message);
    }

    [Fact]
    public void GetAnchorOrThrow_Named_ThrowsWhenDisposed()
    {
        var scope = new CapabilityScope();
        scope.Dispose();

        Assert.Throws<ObjectDisposedException>(() => scope.Anchors.GetOrThrow("key"));
    }

    [Fact]
    public void TryGetAnchor_Named_ReturnsTrueWhenSet()
    {
        using var scope = new CapabilityScope();
        var anchor = new TestAnchor { Value = 200 };
        scope.Anchors.Set("pipeline", anchor);

        var result = scope.Anchors.TryGet("pipeline", out var retrieved);

        Assert.True(result);
        Assert.Same(anchor, retrieved);
    }

    [Fact]
    public void TryGetAnchor_Named_ReturnsFalseWhenNotSet()
    {
        using var scope = new CapabilityScope();

        var result = scope.Anchors.TryGet("nonexistent", out var retrieved);

        Assert.False(result);
        Assert.Null(retrieved);
    }

    [Fact]
    public void TryGetAnchor_Named_ThrowsOnNullKey()
    {
        using var scope = new CapabilityScope();
        
        Assert.Throws<ArgumentNullException>(() => scope.Anchors.TryGet(null!, out _));
    }

    [Fact]
    public void TryGetAnchor_Named_ThrowsWhenDisposed()
    {
        var scope = new CapabilityScope();
        scope.Dispose();

        Assert.Throws<ObjectDisposedException>(() => scope.Anchors.TryGet("key", out _));
    }

    [Fact]
    public void GetAnchorOrThrow_Named_ThrowsWhenCollected()
    {
        using var scope = new CapabilityScope();
        SetNamedAnchorAndCollect(scope, "temp-key");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Anchors.GetOrThrow("temp-key"));
        Assert.Contains("garbage collected", ex.Message);
    }

    [Fact]
    public void TryGetAnchor_Named_ReturnsFalseWhenCollected()
    {
        using var scope = new CapabilityScope();
        SetNamedAnchorAndCollect(scope, "temp-key2");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var result = scope.Anchors.TryGet("temp-key2", out var retrieved);

        Assert.False(result);
        Assert.Null(retrieved);
    }

    private static void SetNamedAnchorAndCollect(CapabilityScope scope, string key)
    {
        scope.Anchors.Set(key, new TestAnchor { Value = 999 });
    }

    #endregion

    #region Fluent Chaining Tests

    [Fact]
    public void FluentChaining_SetOwnerAndMultipleAnchors()
    {
        using var scope = new CapabilityScope();
        var owner = new TestOwner { Name = "MainOwner" };
        var anchor1 = new TestAnchor { Value = 42 };
        var anchor2 = new AnotherAnchor { Flag = true };

        scope.Owner.Set(owner)
            .Scope
            .Anchors
             .Set(anchor1)
             .Set(anchor2)
             .Set("environment", new TestOwner { Name = "Env" });

        Assert.Same(owner, scope.Owner.Get<TestOwner>());
        Assert.Same(anchor1, scope.Anchors.GetOrThrow<TestAnchor>());
        Assert.Same(anchor2, scope.Anchors.GetOrThrow<AnotherAnchor>());
        Assert.NotNull(scope.Anchors.GetOrThrow("environment"));
    }

    #endregion
}
