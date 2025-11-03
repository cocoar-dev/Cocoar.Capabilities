namespace Cocoar.Capabilities;

/// <summary>
/// API for managing the strongly-typed owner of a <see cref="CapabilityScope{TOwner}"/>.
/// </summary>
/// <typeparam name="TOwner">The type of the owner.</typeparam>
public sealed class ScopeOwnerApi<TOwner> where TOwner : class
{
    public CapabilityScope Scope { get; }
    private readonly TOwner _owner;

    internal ScopeOwnerApi(CapabilityScope scope, TOwner owner)
    {
        Scope = scope;
        _owner = owner;
    }

    /// <summary>
    /// Gets the owner of the scope.
    /// </summary>
    /// <returns>The owner.</returns>
    public TOwner Get()
    {
        Scope.ThrowIfDisposed();
        return _owner;
    }

    /// <summary>
    /// Tries to get the owner of the scope.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <returns>Always returns true since the owner is guaranteed to exist.</returns>
    public bool TryGet(out TOwner? owner)
    {
        Scope.ThrowIfDisposed();
        owner = _owner;
        return true;
    }

    /// <summary>
    /// Creates a <see cref="Composer"/> for the owner of this scope.
    /// </summary>
    /// <param name="useRegistry">Optional. Whether to use the registry for this composition.</param>
    /// <returns>A <see cref="Composer"/> for the owner.</returns>
    public Composer Compose(bool? useRegistry = null)
    {
        return Scope.Compose(_owner, useRegistry);
    }

    /// <summary>
    /// Gets the composition for the owner of this scope.
    /// </summary>
    /// <returns>The composition for the owner, or null if not found.</returns>
    public Composition? GetComposition()
    {
        return Scope.Compositions.GetOrDefault(_owner) as Composition;
    }

    /// <summary>
    /// Tries to get the composition for the owner of this scope.
    /// </summary>
    /// <param name="composition">The composition, if found.</param>
    /// <returns>True if a composition exists for the owner; otherwise false.</returns>
    public bool TryGetComposition(out Composition? composition)
    {
        composition = Scope.Compositions.GetOrDefault(_owner) as Composition;
        return composition is not null;
    }

    /// <summary>
    /// Gets the composition for the owner of this scope.
    /// Throws if no composition exists for the owner.
    /// </summary>
    /// <returns>The composition for the owner.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no composition exists for the owner.</exception>
    public Composition GetRequiredComposition()
    {
        var composition = Scope.Compositions.GetOrDefault(_owner) as Composition;
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
        return Scope.Composers.TryGet(_owner, out var composer) ? composer : null;
    }

    /// <summary>
    /// Tries to get the composer for the owner of this scope from the registry.
    /// </summary>
    /// <param name="composer">The composer, if found.</param>
    /// <returns>True if a composer exists in the registry for the owner; otherwise false.</returns>
    public bool TryGetComposer(out Composer? composer)
    {
        return Scope.Composers.TryGet(_owner, out composer);
    }

    /// <summary>
    /// Gets the composer for the owner of this scope from the registry.
    /// Throws if no composer exists in the registry for the owner.
    /// </summary>
    /// <returns>The composer for the owner.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no composer exists in the registry for the owner.</exception>
    public Composer GetRequiredComposer()
    {
        if (!Scope.Composers.TryGet(_owner, out var composer) || composer is null)
        {
            throw new InvalidOperationException("No composer exists in the registry for the owner of this scope.");
        }

        return composer;
    }
}
