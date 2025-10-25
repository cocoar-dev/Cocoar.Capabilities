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
    /// Creates a <see cref="Composer"/> for the owner of this scope, cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The expected type of the owner.</typeparam>
    /// <param name="useRegistry">Optional. Whether to use the registry for this composition.</param>
    /// <returns>A <see cref="Composer"/> for the owner.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no owner is set or it has been collected.</exception>
    /// <exception cref="InvalidCastException">Thrown when the owner is not of type <typeparamref name="T"/>.</exception>
    public Composer Compose<T>(bool? useRegistry = null) where T : class
    {
        var owner = Get<T>();
        return Scope.Compose(owner, useRegistry);
    }

    /// <summary>
    /// Gets the composition for the owner of this scope.
    /// </summary>
    /// <typeparam name="T">The expected type of the owner.</typeparam>
    /// <returns>The composition for the owner, or null if not found.</returns>
    public Composition? GetComposition<T>() where T : class
    {
        if (!TryGet<T>(out var owner) || owner is null)
        {
            return null;
        }

        return Scope.Compositions.GetOrDefault(owner) as Composition;
    }

    internal WeakReference<object>? InternalOwnerReference
    {
        get => _owner;
        set => _owner = value;
    }
}
