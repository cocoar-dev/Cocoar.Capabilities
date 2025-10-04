namespace Cocoar.Capabilities;

public class CompositionRegistryApi : IDisposable
{
    private readonly CapabilityScopeOptions options;
    private readonly DefaultCapabilityRegistry _registry;
    private bool _disposed;

    public CompositionRegistryApi(CapabilityScopeOptions options, DefaultCapabilityRegistry sharedRegistry)
    {
        this.options = options;
        _registry = sharedRegistry;
    }

    public bool TryFind<TSubject>(TSubject subject, out IComposition<TSubject> composition) where TSubject : notnull
    {
    return _registry.TryGetComposition(subject, out composition);
    }

    public IComposition<TSubject>? FindOrDefault<TSubject>(TSubject subject) where TSubject : notnull
    {
        return TryFind<TSubject>(subject, out var composition) ? composition : null;
    }

    public IComposition<TSubject> FindRequired<TSubject>(TSubject subject) where TSubject : notnull
    {
        if (TryFind<TSubject>(subject, out var composition))
            return composition;

        throw new InvalidOperationException($"No composition found for subject of type '{typeof(TSubject).Name}'.");
    }

    public bool TryFind(object subject, out IComposition composition)
    {
        ArgumentNullException.ThrowIfNull(subject);
        
        return _registry.TryGetComposition(subject, out composition);
    }

    public IComposition? FindOrDefault(object subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        
        return TryFind(subject, out IComposition composition) ? composition : null;
    }
    

    public IComposition FindRequired(object subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        
        if (TryFind(subject, out IComposition composition))
            return composition;
            
        throw new InvalidOperationException($"No composition found for subject of type '{subject.GetType().Name}'.");
    }

    public bool Remove<TSubject>(TSubject subject) where TSubject : notnull
    {
        ArgumentNullException.ThrowIfNull(subject);
        
        return _registry.Remove(subject);
    }

    // Internal method for registering compositions (used by Composer.Build)
    internal void Register<TSubject>(TSubject subject, IComposition<TSubject> composition, bool forceRegister = false) where TSubject : notnull
    {
        // Only register if scope allows it OR if explicitly forced (method override)
        if (options.UseCompositionRegistry || forceRegister)
        {
            _registry.RegisterComposition(composition);
        }
    }


    public bool Remove(object subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        
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
