namespace Cocoar.Capabilities;

public class CapabilityScope : IDisposable
{
    private readonly CapabilityScopeOptions _options;
    private readonly DefaultCapabilityRegistry _sharedRegistry;
    private readonly ComposerRegistryApi _composers;
    private readonly CompositionRegistryApi _compositions;
    private readonly ScopeOwnerApi _owner;
    private readonly ScopeAnchorsApi _anchors;
    private bool _disposed;

    public CapabilityScope(CapabilityScopeOptions? options = null)
    {
        _options = options ?? new CapabilityScopeOptions();
        _sharedRegistry = new DefaultCapabilityRegistry(new SubjectKeyCanonicalizer(_options.SubjectKeyMappers));
        _composers = new ComposerRegistryApi(_options, _sharedRegistry);
        _compositions = new CompositionRegistryApi(_options, _sharedRegistry);
        _owner = new ScopeOwnerApi(this);
        _anchors = new ScopeAnchorsApi(this);
    }

    public ComposerRegistryApi Composers => _composers;
    public CompositionRegistryApi Compositions => _compositions;
    
    /// <summary>
    /// API for managing the owner of this scope.
    /// </summary>
    public virtual ScopeOwnerApi Owner => _owner;
    
    /// <summary>
    /// API for managing anchors of this scope.
    /// </summary>
    public ScopeAnchorsApi Anchors => _anchors;

    /// <summary>
    /// Creates a new <see cref="Composer"/> for the specified subject.
    /// </summary>
    /// <param name="subject">The subject to compose capabilities for.</param>
    /// <param name="useRegistry">Optional. Whether to use the registry for this composition.</param>
    /// <returns>A new <see cref="Composer"/> instance.</returns>
    public Composer Compose(object subject, bool? useRegistry = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(subject);
        return new Composer(subject, _options, _sharedRegistry, useRegistry);
    }

    /// <summary>
    /// Creates a new <see cref="Composer"/> for the specified subject.
    /// </summary>
    /// <param name="subject">The subject to compose capabilities for.</param>
    /// <param name="useRegistry">Optional. Whether to use the registry for this composition.</param>
    /// <returns>A new <see cref="Composer"/> instance.</returns>
    [Obsolete("Use Compose() instead. This method will be removed in a future version.")]
    public Composer For(object subject, bool? useRegistry = null)
    {
        return Compose(subject, useRegistry);
    }

    public Composer Recompose(IComposition composition, bool? useRegistry = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(composition);
        return new Composer(composition, _options, _sharedRegistry, useRegistry);
    }

    internal void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _composers.Dispose();
        _compositions.Dispose();
        _sharedRegistry.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
