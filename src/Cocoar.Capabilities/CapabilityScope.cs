namespace Cocoar.Capabilities;

public sealed class CapabilityScope : IDisposable
{
    private readonly CapabilityScopeOptions _options;
    private readonly DefaultCapabilityRegistry _sharedRegistry;
    private readonly ComposerRegistryApi _composers;
    private readonly CompositionRegistryApi _compositions;
    private bool _disposed;

    public CapabilityScope(CapabilityScopeOptions? options = null)
    {
        _options = options ?? new CapabilityScopeOptions();
        _sharedRegistry = new DefaultCapabilityRegistry(new SubjectKeyCanonicalizer(_options.SubjectKeyMappers));
        _composers = new ComposerRegistryApi(_options, _sharedRegistry);
        _compositions = new CompositionRegistryApi(_options, _sharedRegistry);
    }

    internal bool IsDisposed => _disposed;
    public ComposerRegistryApi Composers => _composers;
    public CompositionRegistryApi Compositions => _compositions;
    public Composer<TSubject> For<TSubject>(TSubject subject, bool? useRegistry = null) where TSubject : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(subject);
        return new Composer<TSubject>(subject, _options, _sharedRegistry, useRegistry);
    }
    public Composer<TSubject> Recompose<TSubject>(IComposition<TSubject> composition, bool? useRegistry = null) where TSubject : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(composition);
        return new Composer<TSubject>(composition, _options, _sharedRegistry, useRegistry);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _composers.Dispose();
        _compositions.Dispose();
        _sharedRegistry.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
