namespace Cocoar.Capabilities;

public sealed class Composer
{
    private readonly object _subject;
    private readonly CapabilityScopeOptions _options;
    private readonly DefaultCapabilityRegistry _registry;
    private readonly bool _useComposerRegistry;
    private readonly CapabilityStore _store = new();
    private bool _built;

    internal Composer(object subject, CapabilityScopeOptions options, DefaultCapabilityRegistry registry, bool? useRegistry = null)
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

    internal Composer(IComposition existingComposition, CapabilityScopeOptions options, DefaultCapabilityRegistry registry, bool? useRegistry = null)
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

    public object Subject => _subject;

    public Composer Add(object capability, int? order = null)
    {
        EnsureNotBuilt();
        ArgumentNullException.ThrowIfNull(capability);

        if (capability is IPrimaryCapability && HasPrimary())
        {
            throw new InvalidOperationException(
                $"A primary capability is already set. Use WithPrimary(...) to replace it.");
        }
        _store.Add(capability, capability.GetType(), capability is IPrimaryCapability, order);
        return this;
    }

    public Composer Add(object capability, Func<object, int> orderSelector)
    {
        ArgumentNullException.ThrowIfNull(orderSelector);
        var order = orderSelector(capability);
        return Add(capability, order);
    }
    
    public Composer AddAs<TContract>(object capability, int? order = null)
    {
        EnsureNotBuilt();
        ArgumentNullException.ThrowIfNull(capability);

        var contractType = typeof(TContract);
        
        return IsTupleType(contractType) ? AddAsMultipleContracts<TContract>(capability, order) : AddAsSingleContract<TContract>(capability, order);
    }

    public Composer AddAs<TContract>(object capability, Func<object, int> orderSelector)
    {
        ArgumentNullException.ThrowIfNull(orderSelector);
        var order = orderSelector(capability);
        return AddAs<TContract>(capability, order);
    }

    public Composer TryAdd<TCapability>(TCapability capability, int? order = null) where TCapability : class
    {
        ArgumentNullException.ThrowIfNull(capability);

        if (capability is IPrimaryCapability && HasPrimary())
        {
            return this;
        }

        if (!Has<TCapability>())
        {
            return Add(capability, order);
        }
        return this;
    }

    public Composer TryAdd<TCapability>(TCapability capability, Func<object, int> orderSelector) where TCapability : class
    {
        ArgumentNullException.ThrowIfNull(capability);
        ArgumentNullException.ThrowIfNull(orderSelector);

        var order = orderSelector(capability);
        return TryAdd(capability, order);
    }

    public Composer TryAddAs<TContract>(object capability, int? order = null) where TContract : class
    {
        ArgumentNullException.ThrowIfNull(capability);

        var contractType = typeof(TContract);
        var isPrimaryContract = typeof(IPrimaryCapability).IsAssignableFrom(contractType);
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
                if (typeof(IPrimaryCapability).IsAssignableFrom(ct))
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
            return AddAs<TContract>(capability, order);
        }
        return this;
    }

    public Composer TryAddAs<TContract>(object capability, Func<object, int> orderSelector) where TContract : class
    {
        ArgumentNullException.ThrowIfNull(capability);
        ArgumentNullException.ThrowIfNull(orderSelector);

        var order = orderSelector(capability);
        return TryAddAs<TContract>(capability, order);
    }

    public Composer RemoveWhere(Func<object, bool> predicate)
    {
        EnsureNotBuilt();
        ArgumentNullException.ThrowIfNull(predicate);

        _store.RemoveWhere(predicate);
        return this;
    }

    public Composer WithPrimary(IPrimaryCapability? primary)
    {
        EnsureNotBuilt();

        if (HasPrimary())
        {
            _store.RemoveExistingPrimary();
        }

        if (primary != null)
        {
            _store.Add(primary, primary.GetType(), isPrimary: true, order: null);
        }

        return this;
    }

    public bool HasPrimary()
    {
        return _store.HasPrimary();
    }

    public bool Has<TCapability>() where TCapability : class
    {
        return _store.Has<TCapability>();
    }

    public IComposition Build(bool? useRegistry = null)
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

    private IComposition RecomposeExisting(IComposition existingComposition)
    {
        
        if (existingComposition is not Composition internalComposition)
        {
            throw new ArgumentException("Recompose only supports compositions created by this system", nameof(existingComposition));
        }

        var (result, totalCount) = _store.BuildCapabilityArrays();
        if (result.TryGetValue(CapabilityStore.PrimaryMarkerType, out var primaryArr) && primaryArr.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple primary capabilities registered. Only one primary capability is allowed.");
        }
        internalComposition.UpdateCapabilities(result, totalCount);
        
        return existingComposition;
    }

    private Composer AddAsSingleContract<TContract>(object capability, int? order)
    {
        var contractType = typeof(TContract);
        
        var isPrimaryContract = typeof(IPrimaryCapability).IsAssignableFrom(contractType);

        if (isPrimaryContract && HasPrimary())
        {
            throw new InvalidOperationException(
                $"A primary capability is already set. Use WithPrimary(...) to replace it.");
        }
        _store.Add(capability, contractType, isPrimaryContract, order);

        return this;        
    }

    private Composer AddAsMultipleContracts<TContract>(object capability, int? order)
    {
        var contractTypes = TupleTypeExtractor.GetTupleTypes<TContract>();
        
        TupleTypeExtractor.ValidateCapabilityTypes(contractTypes);
        
        // Check if any contract type is or implements IPrimaryCapability
        // We need to deduplicate - if PrimaryA implements IPrimaryCapability,
        // and the tuple contains (IPrimaryCapability, PrimaryA), we should only count it once
        bool hasPrimaryContract = false;
        int markerInterfaceCount = 0;
        var distinctPrimaryTypes = new HashSet<Type>();
        
        foreach (var ct in contractTypes)
        {
            if (typeof(IPrimaryCapability).IsAssignableFrom(ct))
            {
                hasPrimaryContract = true;
                // Count how many times the marker interface itself appears
                if (ct == typeof(IPrimaryCapability))
                {
                    markerInterfaceCount++;
                }
                else
                {
                    // It's a concrete type implementing IPrimaryCapability
                    distinctPrimaryTypes.Add(ct);
                }
            }
        }

        // Multiple errors to catch:
        // 1. IPrimaryCapability marker appears more than once
        // 2. Multiple distinct types that implement IPrimaryCapability
        if (markerInterfaceCount > 1 || distinctPrimaryTypes.Count > 1)
        {
            throw new InvalidOperationException(
                $"Multiple primary capability contracts specified in the same tuple. Only one primary capability is allowed.");
        }

        if (hasPrimaryContract && HasPrimary())
        {
            throw new InvalidOperationException(
                $"A primary capability is already set. Use WithPrimary(...) to replace it.");
        }

        _store.Add(capability, contractTypes, hasPrimaryContract, order);
        return this;        
    }

    private static bool IsTupleType(Type type) =>
        type.IsGenericType && type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;

    private void EnsureNotBuilt()
    {
        if (_built) throw new InvalidOperationException("Build() has already been called. This builder is no longer usable.");
    }


    private Composition BuildCompositionSnapshot()
    {
        var (result, totalCount) = _store.BuildCapabilityArrays();
        if (result.TryGetValue(CapabilityStore.PrimaryMarkerType, out var primaryArr) && primaryArr.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple primary capabilities registered. Only one primary capability is allowed.");
        }
        return new Composition(_subject, result, totalCount);
    }
}
