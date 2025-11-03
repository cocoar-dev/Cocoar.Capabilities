namespace Cocoar.Capabilities;

/// <summary>
/// API for managing the owner of a <see cref="CapabilityScope"/>.
/// </summary>
public sealed class ScopeOwnerApi
{
    public CapabilityScope Scope { get; }
    private WeakReference<object>? _owner;

    internal ScopeOwnerApi(CapabilityScope scope)
    {
        Scope = scope;
    }

    
    /// <summary>
    /// Sets the owner of the scope. The owner is stored as a weak reference.
    /// Throws if an owner has already been set.
    /// </summary>
    /// <param name="owner">The object to set as the owner.</param>
    /// <returns>The <see cref="ScopeOwnerApi"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if an owner has already been set.</exception>
    public ScopeOwnerApi Set(object owner)
    {
        Scope.ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(owner);
        
        if (_owner is not null && _owner.TryGetTarget(out _))
        {
            throw new InvalidOperationException("An owner has already been set for this scope. Use Replace() to replace an existing owner.");
        }
        
        _owner = new WeakReference<object>(owner);
        return this;
    }

    /// <summary>
    /// Replaces the owner of the scope, or sets it if no owner exists.
    /// The owner is stored as a weak reference.
    /// </summary>
    /// <param name="owner">The object to set as the owner.</param>
    /// <returns>The <see cref="ScopeOwnerApi"/> for chaining.</returns>
    public ScopeOwnerApi Replace(object owner)
    {
        Scope.ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(owner);
        _owner = new WeakReference<object>(owner);
        return this;
    }

    /// <summary>
    /// Gets the owner of the scope, cast to the specified type.
    /// Throws if the owner is not set, has been garbage collected, or is not of the expected type.
    /// </summary>
    /// <typeparam name="T">The expected type of the owner.</typeparam>
    /// <returns>The owner.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no owner is set or it has been collected.</exception>
    /// <exception cref="InvalidCastException">Thrown when the owner is not of type <typeparamref name="T"/>.</exception>
    public T Get<T>() where T : class
    {
        Scope.ThrowIfDisposed();
        
        if (_owner is null)
        {
            throw new InvalidOperationException("No owner has been set for this scope.");
        }

        if (!_owner.TryGetTarget(out var target))
        {
            throw new InvalidOperationException("The owner has been garbage collected.");
        }

        if (target is not T typedOwner)
        {
            throw new InvalidCastException($"Owner is of type {target.GetType().Name}, not {typeof(T).Name}");
        }

        return typedOwner;
    }

    /// <summary>
    /// Gets the owner of the scope, cast to the specified type.
    /// Alias for <see cref="Get{T}"/>.
    /// </summary>
    /// <typeparam name="T">The expected type of the owner.</typeparam>
    /// <returns>The owner.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no owner is set or it has been collected.</exception>
    /// <exception cref="InvalidCastException">Thrown when the owner is not of type <typeparamref name="T"/>.</exception>
    public T GetOrThrow<T>() where T : class => Get<T>();

    /// <summary>
    /// Tries to get the owner of the scope, cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The expected type of the owner.</typeparam>
    /// <param name="owner">The owner, if found and of the correct type.</param>
    /// <returns>True if the owner was found and is of type <typeparamref name="T"/>; otherwise false.</returns>
    public bool TryGet<T>(out T? owner) where T : class
    {
        Scope.ThrowIfDisposed();
        
        if (_owner is not null && _owner.TryGetTarget(out var target) && target is T typedOwner)
        {
            owner = typedOwner;
            return true;
        }

        owner = null;
        return false;
    }

    /// <summary>
    /// Creates a <see cref="Composer"/> for the owner of this scope.
    /// </summary>
    /// <param name="useRegistry">Optional. Whether to use the registry for this composition.</param>
    /// <returns>A <see cref="Composer"/> for the owner.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no owner is set or it has been collected.</exception>
    public Composer Compose(bool? useRegistry = null)
    {
        if (!TryGet<object>(out var owner) || owner is null)
        {
            throw new InvalidOperationException("No owner has been set for this scope or it has been garbage collected.");
        }

        return Scope.Compose(owner, useRegistry);
    }

    /// <summary>
    /// Creates a <see cref="Composer"/> for the owner of this scope, if the owner is of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected type of the owner.</typeparam>
    /// <param name="useRegistry">Optional. Whether to use the registry for this composition.</param>
    /// <returns>A <see cref="Composer"/> for the owner.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no owner is set or it has been collected.</exception>
    /// <exception cref="InvalidCastException">Thrown when the owner is not of type <typeparamref name="T"/>.</exception>
    public Composer ComposeFor<T>(bool? useRegistry = null) where T : class
    {
        var owner = Get<T>();
        return Scope.Compose(owner, useRegistry);
    }

    /// <summary>
    /// Creates a <see cref="Composer"/> for the owner of this scope, if the owner is of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected type of the owner.</typeparam>
    /// <param name="useRegistry">Optional. Whether to use the registry for this composition.</param>
    /// <returns>A <see cref="Composer"/> for the owner.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no owner is set or it has been collected.</exception>
    /// <exception cref="InvalidCastException">Thrown when the owner is not of type <typeparamref name="T"/>.</exception>
    [Obsolete("Use ComposeFor<T>() instead. This method will be removed in the next major version.")]
    public Composer Compose<T>(bool? useRegistry = null) where T : class => ComposeFor<T>(useRegistry);

    /// <summary>
    /// Gets the composition for the owner of this scope.
    /// </summary>
    /// <returns>The composition for the owner, or null if not found.</returns>
    public Composition? GetComposition()
    {
        if (!TryGet<object>(out var owner) || owner is null)
        {
            return null;
        }

        return Scope.Compositions.GetOrDefault(owner) as Composition;
    }

    /// <summary>
    /// Tries to get the composition for the owner of this scope.
    /// </summary>
    /// <param name="composition">The composition, if found.</param>
    /// <returns>True if a composition exists for the owner; otherwise false.</returns>
    public bool TryGetComposition(out Composition? composition)
    {
        if (!TryGet<object>(out var owner) || owner is null)
        {
            composition = null;
            return false;
        }

        composition = Scope.Compositions.GetOrDefault(owner) as Composition;
        return composition is not null;
    }

    /// <summary>
    /// Gets the composition for the owner of this scope, if the owner is of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected type of the owner.</typeparam>
    /// <returns>The composition for the owner, or null if the owner is not of type <typeparamref name="T"/> or not found.</returns>
    public Composition? GetCompositionFor<T>() where T : class
    {
        if (!TryGet<T>(out var owner) || owner is null)
        {
            return null;
        }

        return Scope.Compositions.GetOrDefault(owner) as Composition;
    }

    /// <summary>
    /// Gets the composition for the owner of this scope, if the owner is of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected type of the owner.</typeparam>
    /// <returns>The composition for the owner, or null if the owner is not of type <typeparamref name="T"/> or not found.</returns>
    [Obsolete("Use GetCompositionFor<T>() instead. This method will be removed in the next major version.")]
    public Composition? GetComposition<T>() where T : class => GetCompositionFor<T>();

    /// <summary>
    /// Gets the composition for the owner of this scope.
    /// Throws if no owner is set, the owner has been garbage collected, or no composition exists for the owner.
    /// </summary>
    /// <returns>The composition for the owner.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no owner is set, it has been collected, or no composition exists.</exception>
    public Composition GetRequiredComposition()
    {
        if (!TryGet<object>(out var owner) || owner is null)
        {
            throw new InvalidOperationException("No owner has been set for this scope or it has been garbage collected.");
        }

        var composition = Scope.Compositions.GetOrDefault(owner) as Composition;
        if (composition is null)
        {
            throw new InvalidOperationException("No composition exists for the owner of this scope.");
        }

        return composition;
    }

    /// <summary>
    /// Gets the composition for the owner of this scope, if the owner is of type <typeparamref name="T"/>.
    /// Throws if the owner is not of type <typeparamref name="T"/>, not set, has been garbage collected, or no composition exists.
    /// </summary>
    /// <typeparam name="T">The expected type of the owner.</typeparam>
    /// <returns>The composition for the owner.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no owner is set, it has been collected, or no composition exists.</exception>
    /// <exception cref="InvalidCastException">Thrown when the owner is not of type <typeparamref name="T"/>.</exception>
    public Composition GetRequiredCompositionFor<T>() where T : class
    {
        var owner = Get<T>(); // Throws if wrong type or not found

        var composition = Scope.Compositions.GetOrDefault(owner) as Composition;
        if (composition is null)
        {
            throw new InvalidOperationException("No composition exists for the owner of this scope.");
        }

        return composition;
    }

    /// <summary>
    /// Gets the composer for the owner of this scope from the registry.
    /// </summary>
    /// <returns>The composer for the owner, or null if not found in the registry.</returns>
    public Composer? GetComposer()
    {
        if (!TryGet<object>(out var owner) || owner is null)
        {
            return null;
        }

        return Scope.Composers.TryGet(owner, out var composer) ? composer : null;
    }

    /// <summary>
    /// Tries to get the composer for the owner of this scope from the registry.
    /// </summary>
    /// <param name="composer">The composer, if found.</param>
    /// <returns>True if a composer exists in the registry for the owner; otherwise false.</returns>
    public bool TryGetComposer(out Composer? composer)
    {
        if (!TryGet<object>(out var owner) || owner is null)
        {
            composer = null;
            return false;
        }

        return Scope.Composers.TryGet(owner, out composer);
    }

    /// <summary>
    /// Gets the composer for the owner of this scope from the registry, if the owner is of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected type of the owner.</typeparam>
    /// <returns>The composer for the owner, or null if the owner is not of type <typeparamref name="T"/> or not found in the registry.</returns>
    public Composer? GetComposerFor<T>() where T : class
    {
        if (!TryGet<T>(out var owner) || owner is null)
        {
            return null;
        }

        return Scope.Composers.TryGet(owner, out var composer) ? composer : null;
    }

    /// <summary>
    /// Gets the composer for the owner of this scope from the registry.
    /// Throws if no owner is set, the owner has been garbage collected, or no composer exists in the registry for the owner.
    /// </summary>
    /// <returns>The composer for the owner.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no owner is set, it has been collected, or no composer exists in the registry.</exception>
    public Composer GetRequiredComposer()
    {
        if (!TryGet<object>(out var owner) || owner is null)
        {
            throw new InvalidOperationException("No owner has been set for this scope or it has been garbage collected.");
        }

        if (!Scope.Composers.TryGet(owner, out var composer) || composer is null)
        {
            throw new InvalidOperationException("No composer exists in the registry for the owner of this scope.");
        }

        return composer;
    }

    /// <summary>
    /// Gets the composer for the owner of this scope from the registry, if the owner is of type <typeparamref name="T"/>.
    /// Throws if the owner is not of type <typeparamref name="T"/>, not set, has been garbage collected, or no composer exists in the registry.
    /// </summary>
    /// <typeparam name="T">The expected type of the owner.</typeparam>
    /// <returns>The composer for the owner.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no owner is set, it has been collected, or no composer exists in the registry.</exception>
    /// <exception cref="InvalidCastException">Thrown when the owner is not of type <typeparamref name="T"/>.</exception>
    public Composer GetRequiredComposerFor<T>() where T : class
    {
        var owner = Get<T>(); // Throws if wrong type or not found

        if (!Scope.Composers.TryGet(owner, out var composer) || composer is null)
        {
            throw new InvalidOperationException("No composer exists in the registry for the owner of this scope.");
        }

        return composer;
    }

    internal WeakReference<object>? InternalOwnerReference
    {
        get => _owner;
        set => _owner = value;
    }
}
