namespace Cocoar.Capabilities;

public class ComposerRegistryApi : IDisposable
{
    private readonly CapabilityScopeOptions options;
    private readonly DefaultCapabilityRegistry _registry;
    private bool _disposed;

    public ComposerRegistryApi(CapabilityScopeOptions options, DefaultCapabilityRegistry sharedRegistry)
    {
        this.options = options;
        _registry = sharedRegistry;
    }

    public bool TryFind<TSubject>(TSubject subject, out Composer<TSubject>? composer) where TSubject : notnull
    {
    return _registry.TryGetComposer(subject, out composer);
    }

    public Composer<TSubject>? FindOrDefault<TSubject>(TSubject subject) where TSubject : notnull
    {
        return TryFind(subject, out var composer) ? composer : null;
    }

    public Composer<TSubject> FindRequired<TSubject>(TSubject subject) where TSubject : notnull
    {
        if (TryFind(subject, out var composer) && composer != null)
            return composer;

        throw new InvalidOperationException($"No composer found for subject of type '{typeof(TSubject).Name}'.");
    }

    public void Register<TSubject>(TSubject subject, Composer<TSubject> composer, bool forceRegister = false) where TSubject : notnull
    {
        // Only register if scope allows it OR if explicitly forced (method override)
        if (options.UseComposerRegistry || forceRegister)
        {
            _registry.RegisterComposer(composer);
        }
    }

    public bool Remove<TSubject>(TSubject subject) where TSubject : notnull
    {
        return _registry.Remove(subject);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Don't dispose the shared registry here - it's owned by CapabilityScope
            _disposed = true;
        }
    }
}
