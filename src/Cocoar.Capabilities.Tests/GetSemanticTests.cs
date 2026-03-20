using Xunit;

namespace Cocoar.Capabilities.Tests;

/// <summary>
/// Tests that verify the semantic contract between Get (returns null), GetOrThrow (throws),
/// and TryGet (out-parameter pattern) across Owner and Anchors APIs.
///
/// These tests exist to prevent regression: Get must return null on failure,
/// GetOrThrow must throw on the same conditions. If Get starts throwing (or
/// GetOrThrow starts returning null), these tests catch it immediately.
/// </summary>
public class GetSemanticTests
{
    private class MyOwner { public string Name { get; init; } = ""; }
    private class MyAnchor { public int Value { get; init; } }
    private class WrongType { }

    // ─────────────────────────────────────────────────────────────
    //  Owner — Get vs GetOrThrow vs TryGet
    // ─────────────────────────────────────────────────────────────

    #region Owner: Not Set

    [Fact]
    public void Owner_NotSet_Get_ReturnsNull()
    {
        using var scope = new CapabilityScope();

        var result = scope.Owner.Get<MyOwner>();

        Assert.Null(result);
    }

    [Fact]
    public void Owner_NotSet_GetOrThrow_Throws()
    {
        using var scope = new CapabilityScope();

        Assert.Throws<InvalidOperationException>(() => scope.Owner.GetOrThrow<MyOwner>());
    }

    [Fact]
    public void Owner_NotSet_TryGet_ReturnsFalse()
    {
        using var scope = new CapabilityScope();

        Assert.False(scope.Owner.TryGet<MyOwner>(out var owner));
        Assert.Null(owner);
    }

    #endregion

    #region Owner: Wrong Type

    [Fact]
    public void Owner_WrongType_Get_ReturnsNull()
    {
        using var scope = new CapabilityScope();
        scope.Owner.Set(new MyOwner());

        var result = scope.Owner.Get<WrongType>();

        Assert.Null(result);
    }

    [Fact]
    public void Owner_WrongType_GetOrThrow_Throws()
    {
        using var scope = new CapabilityScope();
        scope.Owner.Set(new MyOwner());

        Assert.Throws<InvalidCastException>(() => scope.Owner.GetOrThrow<WrongType>());
    }

    [Fact]
    public void Owner_WrongType_TryGet_ReturnsFalse()
    {
        using var scope = new CapabilityScope();
        scope.Owner.Set(new MyOwner());

        Assert.False(scope.Owner.TryGet<WrongType>(out var result));
        Assert.Null(result);
    }

    #endregion

    #region Owner: Garbage Collected

    [Fact]
    public void Owner_GarbageCollected_Get_ReturnsNull()
    {
        using var scope = new CapabilityScope();
        SetOwnerAndForget(scope);
        ForceGC();

        var result = scope.Owner.Get<MyOwner>();

        Assert.Null(result);
    }

    [Fact]
    public void Owner_GarbageCollected_GetOrThrow_Throws()
    {
        using var scope = new CapabilityScope();
        SetOwnerAndForget(scope);
        ForceGC();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Owner.GetOrThrow<MyOwner>());
        Assert.Contains("garbage collected", ex.Message);
    }

    [Fact]
    public void Owner_GarbageCollected_TryGet_ReturnsFalse()
    {
        using var scope = new CapabilityScope();
        SetOwnerAndForget(scope);
        ForceGC();

        Assert.False(scope.Owner.TryGet<MyOwner>(out var result));
        Assert.Null(result);
    }

    #endregion

    #region Owner: Success

    [Fact]
    public void Owner_Set_AllThreeMethodsReturnSameInstance()
    {
        using var scope = new CapabilityScope();
        var owner = new MyOwner { Name = "test" };
        scope.Owner.Set(owner);

        var fromGet = scope.Owner.Get<MyOwner>();
        var fromGetOrThrow = scope.Owner.GetOrThrow<MyOwner>();
        scope.Owner.TryGet<MyOwner>(out var fromTryGet);

        Assert.Same(owner, fromGet);
        Assert.Same(owner, fromGetOrThrow);
        Assert.Same(owner, fromTryGet);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    //  Typed Anchors — Get vs GetOrThrow vs TryGet
    // ─────────────────────────────────────────────────────────────

    #region Typed Anchor: Not Set

    [Fact]
    public void TypedAnchor_NotSet_Get_ReturnsNull()
    {
        using var scope = new CapabilityScope();

        var result = scope.Anchors.Get<MyAnchor>();

        Assert.Null(result);
    }

    [Fact]
    public void TypedAnchor_NotSet_GetOrThrow_Throws()
    {
        using var scope = new CapabilityScope();

        Assert.Throws<InvalidOperationException>(() => scope.Anchors.GetOrThrow<MyAnchor>());
    }

    [Fact]
    public void TypedAnchor_NotSet_TryGet_ReturnsFalse()
    {
        using var scope = new CapabilityScope();

        Assert.False(scope.Anchors.TryGet<MyAnchor>(out var anchor));
        Assert.Null(anchor);
    }

    #endregion

    #region Typed Anchor: Garbage Collected

    [Fact]
    public void TypedAnchor_GarbageCollected_Get_ReturnsNull()
    {
        using var scope = new CapabilityScope();
        SetTypedAnchorAndForget(scope);
        ForceGC();

        var result = scope.Anchors.Get<MyAnchor>();

        Assert.Null(result);
    }

    [Fact]
    public void TypedAnchor_GarbageCollected_GetOrThrow_Throws()
    {
        using var scope = new CapabilityScope();
        SetTypedAnchorAndForget(scope);
        ForceGC();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Anchors.GetOrThrow<MyAnchor>());
        Assert.Contains("garbage collected", ex.Message);
    }

    [Fact]
    public void TypedAnchor_GarbageCollected_TryGet_ReturnsFalse()
    {
        using var scope = new CapabilityScope();
        SetTypedAnchorAndForget(scope);
        ForceGC();

        Assert.False(scope.Anchors.TryGet<MyAnchor>(out var result));
        Assert.Null(result);
    }

    #endregion

    #region Typed Anchor: Success

    [Fact]
    public void TypedAnchor_Set_AllThreeMethodsReturnSameInstance()
    {
        using var scope = new CapabilityScope();
        var anchor = new MyAnchor { Value = 42 };
        scope.Anchors.Set(anchor);

        var fromGet = scope.Anchors.Get<MyAnchor>();
        var fromGetOrThrow = scope.Anchors.GetOrThrow<MyAnchor>();
        scope.Anchors.TryGet<MyAnchor>(out var fromTryGet);

        Assert.Same(anchor, fromGet);
        Assert.Same(anchor, fromGetOrThrow);
        Assert.Same(anchor, fromTryGet);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    //  Named Anchors — Get vs GetOrThrow vs TryGet
    // ─────────────────────────────────────────────────────────────

    #region Named Anchor: Not Set

    [Fact]
    public void NamedAnchor_NotSet_Get_ReturnsNull()
    {
        using var scope = new CapabilityScope();

        var result = scope.Anchors.Get("missing-key");

        Assert.Null(result);
    }

    [Fact]
    public void NamedAnchor_NotSet_GetOrThrow_Throws()
    {
        using var scope = new CapabilityScope();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Anchors.GetOrThrow("missing-key"));
        Assert.Contains("missing-key", ex.Message);
    }

    [Fact]
    public void NamedAnchor_NotSet_TryGet_ReturnsFalse()
    {
        using var scope = new CapabilityScope();

        Assert.False(scope.Anchors.TryGet("missing-key", out var anchor));
        Assert.Null(anchor);
    }

    #endregion

    #region Named Anchor: Garbage Collected

    [Fact]
    public void NamedAnchor_GarbageCollected_Get_ReturnsNull()
    {
        using var scope = new CapabilityScope();
        SetNamedAnchorAndForget(scope, "gc-key");
        ForceGC();

        var result = scope.Anchors.Get("gc-key");

        Assert.Null(result);
    }

    [Fact]
    public void NamedAnchor_GarbageCollected_GetOrThrow_Throws()
    {
        using var scope = new CapabilityScope();
        SetNamedAnchorAndForget(scope, "gc-key");
        ForceGC();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Anchors.GetOrThrow("gc-key"));
        Assert.Contains("garbage collected", ex.Message);
    }

    [Fact]
    public void NamedAnchor_GarbageCollected_TryGet_ReturnsFalse()
    {
        using var scope = new CapabilityScope();
        SetNamedAnchorAndForget(scope, "gc-key");
        ForceGC();

        Assert.False(scope.Anchors.TryGet("gc-key", out var result));
        Assert.Null(result);
    }

    #endregion

    #region Named Anchor: Success

    [Fact]
    public void NamedAnchor_Set_AllThreeMethodsReturnSameInstance()
    {
        using var scope = new CapabilityScope();
        var anchor = new MyAnchor { Value = 99 };
        scope.Anchors.Set("key", anchor);

        var fromGet = scope.Anchors.Get("key");
        var fromGetOrThrow = scope.Anchors.GetOrThrow("key");
        scope.Anchors.TryGet("key", out var fromTryGet);

        Assert.Same(anchor, fromGet);
        Assert.Same(anchor, fromGetOrThrow);
        Assert.Same(anchor, fromTryGet);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────

    private static void SetOwnerAndForget(CapabilityScope scope)
        => scope.Owner.Set(new MyOwner { Name = "Temporary" });

    private static void SetTypedAnchorAndForget(CapabilityScope scope)
        => scope.Anchors.Set(new MyAnchor { Value = 999 });

    private static void SetNamedAnchorAndForget(CapabilityScope scope, string key)
        => scope.Anchors.Set(key, new MyAnchor { Value = 999 });

    private static void ForceGC()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
