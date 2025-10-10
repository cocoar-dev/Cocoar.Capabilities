namespace Cocoar.Capabilities;

public sealed class CompositionRegistryApi : IDisposable
{
    private readonly CapabilityScopeOptions options;
    private readonly DefaultCapabilityRegistry _registry;
    private bool _disposed;

    public CompositionRegistryApi(CapabilityScopeOptions options, DefaultCapabilityRegistry sharedRegistry)
    {
        this.options = options;
        _registry = sharedRegistry;
    }

    public bool TryGet<TSubject>(TSubject subject, out IComposition composition) where TSubject : notnull
    {
    return _registry.TryGetComposition(subject, out composition);
    }

    public IComposition? GetOrDefault<TSubject>(TSubject subject) where TSubject : notnull
    {
        return TryGet<TSubject>(subject, out var composition) ? composition : null;
    }

    public IComposition GetRequired<TSubject>(TSubject subject) where TSubject : notnull
    {
        if (TryGet<TSubject>(subject, out var composition))
            return composition;

        throw new InvalidOperationException($"No composition found for subject of type '{typeof(TSubject).Name}'.");
    }

    public bool TryGet(object subject, out IComposition composition)
    {
        ArgumentNullException.ThrowIfNull(subject);
        
        return _registry.TryGetComposition(subject, out composition);
    }

    public IComposition? GetOrDefault(object subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        
        return TryGet(subject, out IComposition composition) ? composition : null;
    }
    

    public IComposition GetRequired(object subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        
        if (TryGet(subject, out IComposition composition))
            return composition;
            
        throw new InvalidOperationException($"No composition found for subject of type '{subject.GetType().Name}'.");
    }

    public bool Remove<TSubject>(TSubject subject) where TSubject : notnull
    {
        ArgumentNullException.ThrowIfNull(subject);
        
        return _registry.Remove(subject);
    }

    // Internal method for registering compositions (used by Composer.Build)
    internal void Register<TSubject>(TSubject subject, IComposition composition, bool forceRegister = false) where TSubject : notnull
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
        if (_disposed)
        {
            return;
        }

        // Don't dispose the shared registry here - it's owned by CapabilityScope
        _disposed = true;
    }
}
