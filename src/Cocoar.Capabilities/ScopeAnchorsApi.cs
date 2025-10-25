namespace Cocoar.Capabilities;

/// <summary>
/// API for managing anchors of a <see cref="CapabilityScope"/>.
/// </summary>
public sealed class ScopeAnchorsApi
{
    public CapabilityScope Scope { get; }
    private readonly Dictionary<Type, WeakReference<object>> _typedAnchors = new();
    private readonly Dictionary<string, WeakReference<object>> _namedAnchors = new();

    internal ScopeAnchorsApi(CapabilityScope scope)
    {
        Scope = scope;
    }

    /// <summary>
    /// Sets a typed anchor for this scope. Only one anchor per type is supported.
    /// The anchor is stored as a weak reference.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <param name="anchor">The anchor object.</param>
    /// <returns>The <see cref="CapabilityScope"/> for chaining.</returns>
    public ScopeAnchorsApi Set<T>(T anchor) where T : class
    {
        Scope.ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(anchor);
        _typedAnchors[typeof(T)] = new WeakReference<object>(anchor);
        return this;
    }

    /// <summary>
    /// Sets a named anchor for this scope.
    /// The anchor is stored as a weak reference.
    /// </summary>
    /// <param name="key">The key for the anchor.</param>
    /// <param name="anchor">The anchor object.</param>
    /// <returns>The <see cref="CapabilityScope"/> for chaining.</returns>
    public ScopeAnchorsApi Set(string key, object anchor)
    {
        Scope.ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(anchor);
        _namedAnchors[key] = new WeakReference<object>(anchor);
        return this;
    }

    /// <summary>
    /// Gets a typed anchor from this scope.
    /// Throws if the anchor is not set or has been garbage collected.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <returns>The anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor of the specified type is set or it has been collected.</exception>
    public T Get<T>() where T : class
    {
        Scope.ThrowIfDisposed();
        
        if (!_typedAnchors.TryGetValue(typeof(T), out var weakRef))
        {
            throw new InvalidOperationException($"No anchor of type {typeof(T).Name} has been set for this scope.");
        }

        if (!weakRef.TryGetTarget(out var target))
        {
            throw new InvalidOperationException($"The anchor of type {typeof(T).Name} has been garbage collected.");
        }

        return (T)target;
    }

    /// <summary>
    /// Gets a named anchor from this scope.
    /// Throws if the anchor is not set or has been garbage collected.
    /// </summary>
    /// <param name="key">The key of the anchor.</param>
    /// <returns>The anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor with the specified key is set or it has been collected.</exception>
    public object Get(string key)
    {
        Scope.ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(key);

        if (!_namedAnchors.TryGetValue(key, out var weakRef))
        {
            throw new InvalidOperationException($"No anchor with key '{key}' has been set for this scope.");
        }

        if (!weakRef.TryGetTarget(out var target))
        {
            throw new InvalidOperationException($"The anchor with key '{key}' has been garbage collected.");
        }

        return target;
    }

    /// <summary>
    /// Gets a typed anchor from this scope.
    /// Alias for <see cref="Get{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <returns>The anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor of the specified type is set or it has been collected.</exception>
    public T GetOrThrow<T>() where T : class => Get<T>();

    /// <summary>
    /// Gets a named anchor from this scope.
    /// Alias for <see cref="Get(string)"/>.
    /// </summary>
    /// <param name="key">The key of the anchor.</param>
    /// <returns>The anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor with the specified key is set or it has been collected.</exception>
    public object GetOrThrow(string key) => Get(key);

    /// <summary>
    /// Tries to get a typed anchor from this scope.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <param name="anchor">The anchor, if found.</param>
    /// <returns>True if the anchor was found; otherwise false.</returns>
    public bool TryGet<T>(out T? anchor) where T : class
    {
        Scope.ThrowIfDisposed();
        
        if (_typedAnchors.TryGetValue(typeof(T), out var weakRef) && weakRef.TryGetTarget(out var target))
        {
            anchor = (T)target;
            return true;
        }

        anchor = null;
        return false;
    }

    /// <summary>
    /// Tries to get a named anchor from this scope.
    /// </summary>
    /// <param name="key">The key of the anchor.</param>
    /// <param name="anchor">The anchor, if found.</param>
    /// <returns>True if the anchor was found; otherwise false.</returns>
    public bool TryGet(string key, out object? anchor)
    {
        Scope.ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(key);
        
        if (_namedAnchors.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var target))
        {
            anchor = target;
            return true;
        }

        anchor = null;
        return false;
    }    /// <summary>
    /// Creates a <see cref="Composer"/> for a typed anchor.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <param name="useRegistry">Optional. Whether to use the registry for this composition.</param>
    /// <returns>A <see cref="Composer"/> for the anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor of the specified type is set or it has been collected.</exception>
    public Composer Compose<T>(bool? useRegistry = null) where T : class
    {
        var anchor = Get<T>();
        return Scope.Compose(anchor, useRegistry);
    }

    /// <summary>
    /// Creates a <see cref="Composer"/> for a named anchor.
    /// </summary>
    /// <param name="key">The key of the anchor.</param>
    /// <param name="useRegistry">Optional. Whether to use the registry for this composition.</param>
    /// <returns>A <see cref="Composer"/> for the anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor with the specified key is set or it has been collected.</exception>
    public Composer Compose(string key, bool? useRegistry = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        var anchor = Get(key);
        return Scope.Compose(anchor, useRegistry);
    }

    /// <summary>
    /// Gets the composition for a typed anchor.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <returns>The composition for the anchor, or null if not found.</returns>
    public Composition? GetComposition<T>() where T : class
    {
        if (!TryGet<T>(out var anchor) || anchor is null)
        {
            return null;
        }

        return Scope.Compositions.GetOrDefault(anchor) as Composition;
    }

    /// <summary>
    /// Gets the composition for a named anchor.
    /// </summary>
    /// <param name="key">The key of the anchor.</param>
    /// <returns>The composition for the anchor, or null if not found.</returns>
    public Composition? GetComposition(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!TryGet(key, out var anchor) || anchor is null)
        {
            return null;
        }

        return Scope.Compositions.GetOrDefault(anchor) as Composition;
    }

    internal Dictionary<Type, WeakReference<object>> InternalTypedAnchors => _typedAnchors;
    internal Dictionary<string, WeakReference<object>> InternalNamedAnchors => _namedAnchors;
}
