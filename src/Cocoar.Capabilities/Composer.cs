namespace Cocoar.Capabilities;

public sealed class Composer<TSubject> where TSubject : notnull
{
    private readonly TSubject _subject;
    private readonly CapabilityScopeOptions _options;
    private readonly DefaultCapabilityRegistry _registry;
    private readonly bool _useComposerRegistry;
    private readonly CapabilityStore<TSubject> _store = new();
    private bool _built;

    internal Composer(TSubject subject, CapabilityScopeOptions options, DefaultCapabilityRegistry registry, bool? useRegistry = null)
    {
        ArgumentNullException.ThrowIfNull(subject);
        _subject = subject;
        _options = options;
        _registry = registry;
        _useComposerRegistry = useRegistry ?? _options.UseComposerRegistry;
        if (_useComposerRegistry)
        {
            _registry.RegisterComposer(this);
        }
    }

    internal Composer(IComposition<TSubject> existingComposition, CapabilityScopeOptions options, DefaultCapabilityRegistry registry, bool? useRegistry = null)
    {
        ArgumentNullException.ThrowIfNull(existingComposition);
        _subject = existingComposition.Subject;
        _options = options;
        _registry = registry;
        _store.SeedFromComposition(existingComposition);
        _useComposerRegistry = useRegistry ?? _options.UseComposerRegistry;
        if (_useComposerRegistry)
        {
            _registry.RegisterComposer(this);
        }
    }

    public TSubject Subject => _subject;

    public Composer<TSubject> Add(ICapability<TSubject> capability)
    {
        EnsureNotBuilt();
        ArgumentNullException.ThrowIfNull(capability);

        if (capability is IPrimaryCapability<TSubject> && HasPrimary())
        {
            throw new InvalidOperationException(
                $"A primary capability is already set for '{typeof(TSubject).Name}'. Use WithPrimary(...) to replace it.");
        }
        _store.Add(capability, capability.GetType(), capability is IPrimaryCapability<TSubject>);
        return this;
    }
    
    public Composer<TSubject> AddAs<TContract>(ICapability<TSubject> capability)
    {
        EnsureNotBuilt();
        ArgumentNullException.ThrowIfNull(capability);

        var contractType = typeof(TContract);
        
        return IsTupleType(contractType) ? AddAsMultipleContracts<TContract>(capability) : AddAsSingleContract<TContract>(capability);
    }

    public Composer<TSubject> TryAdd<TCapability>(TCapability capability) where TCapability : class, ICapability<TSubject>
    {
        ArgumentNullException.ThrowIfNull(capability);

        if (capability is IPrimaryCapability<TSubject> && HasPrimary())
        {
            return this;
        }

        if (!Has<TCapability>())
        {
            return Add(capability);
        }
        return this;
    }

    public Composer<TSubject> TryAddAs<TContract>(ICapability<TSubject> capability) where TContract : class, ICapability<TSubject>
    {
        ArgumentNullException.ThrowIfNull(capability);

        var contractType = typeof(TContract);
        var isPrimaryContract = contractType.IsGenericType && contractType.GetGenericTypeDefinition() == typeof(IPrimaryCapability<>);
        if (isPrimaryContract && HasPrimary())
        {
            return this;
        }

        if (IsTupleType(contractType))
        {
            var tupleTypes = TupleTypeExtractor.GetTupleTypes<TContract>();
                for (int i = 0; i < tupleTypes.Length; i++)
            {
                var ct = tupleTypes[i];
                if (ct.IsGenericType && ct.GetGenericTypeDefinition() == typeof(IPrimaryCapability<>))
                {
                    if (HasPrimary())
                    {
                        return this;
                    }
                    break;
                }
            }
        }

        if (!Has<TContract>())
        {
            return AddAs<TContract>(capability);
        }
        return this;
    }

    public Composer<TSubject> RemoveWhere(Func<ICapability<TSubject>, bool> predicate)
    {
        EnsureNotBuilt();
        ArgumentNullException.ThrowIfNull(predicate);

        _store.RemoveWhere(predicate);
        return this;
    }

    public Composer<TSubject> WithPrimary(IPrimaryCapability<TSubject>? primary)
    {
        EnsureNotBuilt();

        if (HasPrimary())
        {
            _store.RemoveExistingPrimary();
        }

        if (primary != null)
        {
            _store.Add(primary, primary.GetType(), isPrimary: true);
        }

        return this;
    }

    public bool HasPrimary()
    {
        return _store.HasPrimary();
    }

    public bool Has<TCapability>() where TCapability : class, ICapability<TSubject>
    {
        return _store.Has<TCapability>();
    }

    public IComposition<TSubject> Build(bool? useRegistry = null)
    {
        if (_built) throw new InvalidOperationException("Build() can only be called once. This builder is no longer usable.");
        _built = true;
        var bag = BuildCompositionSnapshot();

    var shouldUseCompositionRegistry = useRegistry ?? _options.UseCompositionRegistry;

        if (!_useComposerRegistry && !shouldUseCompositionRegistry)
        {
            return bag;
        }

        if (!_useComposerRegistry && shouldUseCompositionRegistry)
        {
            _registry.RegisterComposition(bag);
        }

        if (_useComposerRegistry && !shouldUseCompositionRegistry)
        {
            _registry.RemoveComposer(_subject);
        }

        if (_useComposerRegistry && shouldUseCompositionRegistry)
        {
            _registry.TransitionToComposition(bag);
        }

        return bag;
    }

    private IComposition<TSubject> RecomposeExisting(IComposition<TSubject> existingComposition)
    {
        
        if (existingComposition is not Composition<TSubject> internalComposition)
        {
            throw new ArgumentException("Recompose only supports compositions created by this system", nameof(existingComposition));
        }

        var (result, totalCount) = _store.BuildCapabilityArrays();
        if (result.TryGetValue(CapabilityStore<TSubject>.PrimaryMarkerType, out var primaryArr) && primaryArr.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple primary capabilities registered for '{typeof(TSubject).Name}'. Only one primary capability is allowed.");
        }
    internalComposition.UpdateCapabilities(result, totalCount);
        
    return existingComposition;
    }

    private Composer<TSubject> AddAsSingleContract<TContract>(ICapability<TSubject> capability)
    {
        var contractType = typeof(TContract);
        
        if (!typeof(ICapability<TSubject>).IsAssignableFrom(contractType))
        {
            throw new ArgumentException($"Type '{contractType.Name}' must implement ICapability<{typeof(TSubject).Name}> to be registered as a capability contract.");
        }
        var isPrimaryContract = contractType.IsGenericType && contractType.GetGenericTypeDefinition() == typeof(IPrimaryCapability<>);

        if (isPrimaryContract && HasPrimary())
        {
            throw new InvalidOperationException(
                $"A primary capability is already set for '{typeof(TSubject).Name}'. Use WithPrimary(...) to replace it.");
        }
    _store.Add(capability, contractType, isPrimaryContract);

        return this;        
    }

    private Composer<TSubject> AddAsMultipleContracts<TContract>(ICapability<TSubject> capability)
    {
        var contractTypes = TupleTypeExtractor.GetTupleTypes<TContract>();
        
        TupleTypeExtractor.ValidateCapabilityTypes<TSubject>(contractTypes);
        int primaryCountInTuple = 0;
        foreach (var ct in contractTypes)
        {
            if (ct.IsGenericType && ct.GetGenericTypeDefinition() == typeof(IPrimaryCapability<>))
            {
                primaryCountInTuple++;
            }
        }

        if (primaryCountInTuple > 1)
        {
            throw new InvalidOperationException(
                $"Multiple primary capability contracts specified in the same tuple for '{typeof(TSubject).Name}'. Only one primary capability is allowed.");
        }

        if (primaryCountInTuple == 1 && HasPrimary())
        {
            throw new InvalidOperationException(
                $"A primary capability is already set for '{typeof(TSubject).Name}'. Use WithPrimary(...) to replace it.");
        }

        _store.Add(capability, contractTypes, primaryCountInTuple == 1);
        return this;        
    }

    private static bool IsTupleType(Type type) =>
        type.IsGenericType && type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;

    private void EnsureNotBuilt()
    {
        if (_built) throw new InvalidOperationException("Build() has already been called. This builder is no longer usable.");
    }


    private Composition<TSubject> BuildCompositionSnapshot()
    {
        var (result, totalCount) = _store.BuildCapabilityArrays();
        if (result.TryGetValue(CapabilityStore<TSubject>.PrimaryMarkerType, out var primaryArr) && primaryArr.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple primary capabilities registered for '{typeof(TSubject).Name}'. Only one primary capability is allowed.");
        }
    return new Composition<TSubject>(_subject, result, totalCount);
    }
}
