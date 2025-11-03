namespace Cocoar.Capabilities;

/// <summary>
/// A capability scope with a strongly-typed owner.
/// </summary>
/// <typeparam name="TOwner">The type of the owner.</typeparam>
public class CapabilityScope<TOwner> : CapabilityScope where TOwner : class
{
    /// <summary>
    /// Gets the strongly-typed owner API for this scope.
    /// </summary>
    public new ScopeOwnerApi<TOwner> Owner { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CapabilityScope{TOwner}"/> class with the specified owner.
    /// </summary>
    /// <param name="owner">The owner of the scope.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="owner"/> is null.</exception>
    public CapabilityScope(TOwner owner) : base()
    {
        ArgumentNullException.ThrowIfNull(owner);
        
        Owner = new ScopeOwnerApi<TOwner>(this, owner);
        
        // Also set the base owner for consistency with the dynamic API
        base.Owner.InternalOwnerReference = new WeakReference<object>(owner);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CapabilityScope{TOwner}"/> class with the specified owner and options.
    /// </summary>
    /// <param name="owner">The owner of the scope.</param>
    /// <param name="options">The options for the scope.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="owner"/> is null.</exception>
    public CapabilityScope(TOwner owner, CapabilityScopeOptions? options) : base(options)
    {
        ArgumentNullException.ThrowIfNull(owner);
        
        Owner = new ScopeOwnerApi<TOwner>(this, owner);
        
        // Also set the base owner for consistency with the dynamic API
        base.Owner.InternalOwnerReference = new WeakReference<object>(owner);
    }
}
