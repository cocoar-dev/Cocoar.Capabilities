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
    /// Returns null if the anchor is not set or has been garbage collected.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <returns>The anchor, or null if not available.</returns>
    public T? Get<T>() where T : class
    {
        Scope.ThrowIfDisposed();

        if (_typedAnchors.TryGetValue(typeof(T), out var weakRef) && weakRef.TryGetTarget(out var target))
        {
            return (T)target;
        }

        return null;
    }

    /// <summary>
    /// Gets a named anchor from this scope.
    /// Returns null if the anchor is not set or has been garbage collected.
    /// </summary>
    /// <param name="key">The key of the anchor.</param>
    /// <returns>The anchor, or null if not available.</returns>
    public object? Get(string key)
    {
        Scope.ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(key);

        if (_namedAnchors.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var target))
        {
            return target;
        }

        return null;
    }

    /// <summary>
    /// Gets a typed anchor from this scope.
    /// Throws if the anchor is not set or has been garbage collected.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <returns>The anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor of the specified type is set or it has been collected.</exception>
    public T GetOrThrow<T>() where T : class
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
    public object GetOrThrow(string key)
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
    }

    /// <summary>
    /// Creates a <see cref="Composer"/> for a typed anchor.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <param name="useRegistry">Optional. Whether to use the registry for this composition.</param>
    /// <returns>A <see cref="Composer"/> for the anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor of the specified type is set or it has been collected.</exception>
    public Composer ComposeFor<T>(bool? useRegistry = null) where T : class
    {
        var anchor = GetOrThrow<T>();
        return Scope.Compose(anchor, useRegistry);
    }

    /// <summary>
    /// Creates a <see cref="Composer"/> for a typed anchor.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <param name="useRegistry">Optional. Whether to use the registry for this composition.</param>
    /// <returns>A <see cref="Composer"/> for the anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor of the specified type is set or it has been collected.</exception>
    [Obsolete("Use ComposeFor<T>() instead for clarity. This method will be removed in a future version.")]
    public Composer Compose<T>(bool? useRegistry = null) where T : class
    {
        return ComposeFor<T>(useRegistry);
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
        var anchor = GetOrThrow(key);
        return Scope.Compose(anchor, useRegistry);
    }

    /// <summary>
    /// Gets the composition for a typed anchor.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <returns>The composition for the anchor, or null if not found.</returns>
    public Composition? GetCompositionFor<T>() where T : class
    {
        if (!TryGet<T>(out var anchor) || anchor is null)
        {
            return null;
        }

        return Scope.Compositions.GetOrDefault(anchor) as Composition;
    }

    /// <summary>
    /// Gets the composition for a typed anchor.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <returns>The composition for the anchor, or null if not found.</returns>
    [Obsolete("Use GetCompositionFor<T>() instead for clarity. This method will be removed in a future version.")]
    public Composition? GetComposition<T>() where T : class
    {
        return GetCompositionFor<T>();
    }

    /// <summary>
    /// Tries to get the composition for a typed anchor.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <param name="composition">The composition, if found.</param>
    /// <returns>True if a composition exists for the anchor; otherwise false.</returns>
    public bool TryGetCompositionFor<T>(out Composition? composition) where T : class
    {
        if (!TryGet<T>(out var anchor) || anchor is null)
        {
            composition = null;
            return false;
        }

        composition = Scope.Compositions.GetOrDefault(anchor) as Composition;
        return composition is not null;
    }

    /// <summary>
    /// Tries to get the composition for a typed anchor.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <param name="composition">The composition, if found.</param>
    /// <returns>True if a composition exists for the anchor; otherwise false.</returns>
    [Obsolete("Use TryGetCompositionFor<T>() instead for clarity. This method will be removed in a future version.")]
    public bool TryGetComposition<T>(out Composition? composition) where T : class
    {
        return TryGetCompositionFor<T>(out composition);
    }

    /// <summary>
    /// Gets the composition for a typed anchor.
    /// Throws if no anchor is set, the anchor has been garbage collected, or no composition exists.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <returns>The composition for the anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor is set, it has been collected, or no composition exists.</exception>
    public Composition GetRequiredCompositionFor<T>() where T : class
    {
        var anchor = GetOrThrow<T>();
        var composition = Scope.Compositions.GetOrDefault(anchor) as Composition;
        if (composition is null)
        {
            throw new InvalidOperationException($"No composition exists for the anchor of type {typeof(T).Name}.");
        }

        return composition;
    }

    /// <summary>
    /// Gets the composition for a typed anchor.
    /// Throws if no anchor is set, the anchor has been garbage collected, or no composition exists.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <returns>The composition for the anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor is set, it has been collected, or no composition exists.</exception>
    [Obsolete("Use GetRequiredCompositionFor<T>() instead for clarity. This method will be removed in a future version.")]
    public Composition GetRequiredComposition<T>() where T : class
    {
        return GetRequiredCompositionFor<T>();
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

    /// <summary>
    /// Tries to get the composition for a named anchor.
    /// </summary>
    /// <param name="key">The key of the anchor.</param>
    /// <param name="composition">The composition, if found.</param>
    /// <returns>True if a composition exists for the anchor; otherwise false.</returns>
    public bool TryGetComposition(string key, out Composition? composition)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!TryGet(key, out var anchor) || anchor is null)
        {
            composition = null;
            return false;
        }

        composition = Scope.Compositions.GetOrDefault(anchor) as Composition;
        return composition is not null;
    }

    /// <summary>
    /// Gets the composition for a named anchor.
    /// Throws if no anchor is set, the anchor has been garbage collected, or no composition exists.
    /// </summary>
    /// <param name="key">The key of the anchor.</param>
    /// <returns>The composition for the anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor is set, it has been collected, or no composition exists.</exception>
    public Composition GetRequiredComposition(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var anchor = GetOrThrow(key);
        var composition = Scope.Compositions.GetOrDefault(anchor) as Composition;
        if (composition is null)
        {
            throw new InvalidOperationException($"No composition exists for the anchor with key '{key}'.");
        }

        return composition;
    }

    /// <summary>
    /// Gets the composer from the registry for a typed anchor, or null if not found.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <returns>The composer for the anchor, or null if not found in the registry.</returns>
    public Composer? GetComposerFor<T>() where T : class
    {
        if (!TryGet<T>(out var anchor) || anchor is null)
        {
            return null;
        }

        return Scope.Composers.TryGet(anchor, out var composer) ? composer : null;
    }

    /// <summary>
    /// Gets the composer from the registry for a typed anchor, or null if not found.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <returns>The composer for the anchor, or null if not found in the registry.</returns>
    [Obsolete("Use GetComposerFor<T>() instead for clarity. This method will be removed in a future version.")]
    public Composer? GetComposer<T>() where T : class
    {
        return GetComposerFor<T>();
    }

    /// <summary>
    /// Tries to get the composer from the registry for a typed anchor.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <param name="composer">The composer, if found.</param>
    /// <returns>True if a composer exists for the anchor; otherwise false.</returns>
    public bool TryGetComposerFor<T>(out Composer? composer) where T : class
    {
        if (!TryGet<T>(out var anchor) || anchor is null)
        {
            composer = null;
            return false;
        }

        return Scope.Composers.TryGet(anchor, out composer);
    }

    /// <summary>
    /// Tries to get the composer from the registry for a typed anchor.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <param name="composer">The composer, if found.</param>
    /// <returns>True if a composer exists for the anchor; otherwise false.</returns>
    [Obsolete("Use TryGetComposerFor<T>() instead for clarity. This method will be removed in a future version.")]
    public bool TryGetComposer<T>(out Composer? composer) where T : class
    {
        return TryGetComposerFor<T>(out composer);
    }

    /// <summary>
    /// Gets the composer from the registry for a typed anchor.
    /// Throws if no anchor is set, the anchor has been garbage collected, or no composer exists in the registry.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <returns>The composer for the anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor is set, it has been collected, or no composer exists.</exception>
    public Composer GetRequiredComposerFor<T>() where T : class
    {
        var anchor = GetOrThrow<T>();
        if (!Scope.Composers.TryGet(anchor, out var composer) || composer is null)
        {
            throw new InvalidOperationException($"No composer exists in the registry for the anchor of type {typeof(T).Name}.");
        }

        return composer;
    }

    /// <summary>
    /// Gets the composer from the registry for a typed anchor.
    /// Throws if no anchor is set, the anchor has been garbage collected, or no composer exists in the registry.
    /// </summary>
    /// <typeparam name="T">The type of the anchor.</typeparam>
    /// <returns>The composer for the anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor is set, it has been collected, or no composer exists.</exception>
    [Obsolete("Use GetRequiredComposerFor<T>() instead for clarity. This method will be removed in a future version.")]
    public Composer GetRequiredComposer<T>() where T : class
    {
        return GetRequiredComposerFor<T>();
    }

    /// <summary>
    /// Gets the composer from the registry for a named anchor, or null if not found.
    /// </summary>
    /// <param name="key">The key of the anchor.</param>
    /// <returns>The composer for the anchor, or null if not found in the registry.</returns>
    public Composer? GetComposer(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!TryGet(key, out var anchor) || anchor is null)
        {
            return null;
        }

        return Scope.Composers.TryGet(anchor, out var composer) ? composer : null;
    }

    /// <summary>
    /// Tries to get the composer from the registry for a named anchor.
    /// </summary>
    /// <param name="key">The key of the anchor.</param>
    /// <param name="composer">The composer, if found.</param>
    /// <returns>True if a composer exists for the anchor; otherwise false.</returns>
    public bool TryGetComposer(string key, out Composer? composer)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!TryGet(key, out var anchor) || anchor is null)
        {
            composer = null;
            return false;
        }

        return Scope.Composers.TryGet(anchor, out composer);
    }

    /// <summary>
    /// Gets the composer from the registry for a named anchor.
    /// Throws if no anchor is set, the anchor has been garbage collected, or no composer exists in the registry.
    /// </summary>
    /// <param name="key">The key of the anchor.</param>
    /// <returns>The composer for the anchor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no anchor is set, it has been collected, or no composer exists.</exception>
    public Composer GetRequiredComposer(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var anchor = GetOrThrow(key);
        if (!Scope.Composers.TryGet(anchor, out var composer) || composer is null)
        {
            throw new InvalidOperationException($"No composer exists in the registry for the anchor with key '{key}'.");
        }

        return composer;
    }

    internal Dictionary<Type, WeakReference<object>> InternalTypedAnchors => _typedAnchors;
    internal Dictionary<string, WeakReference<object>> InternalNamedAnchors => _namedAnchors;
}
