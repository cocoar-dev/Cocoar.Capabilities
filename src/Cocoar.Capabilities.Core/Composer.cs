namespace Cocoar.Capabilities.Core;

public static class Composer
{
    public static Composer<TSubject> For<TSubject>(TSubject subject)
        where TSubject : notnull
    {
        if (subject is null) throw new ArgumentNullException(nameof(subject));
        
        return new Composer<TSubject>(subject);
    }



    public static Composer<TSubject> Recompose<TSubject>(IComposition<TSubject> existingComposition)
        where TSubject : notnull
    {
        if (existingComposition is null) throw new ArgumentNullException(nameof(existingComposition));
        
        return new Composer<TSubject>(existingComposition);
    }
}

public sealed class Composer<TSubject> where TSubject : notnull
{
    private readonly TSubject _subject;
    private int _nextCapabilityId;
    private readonly Dictionary<int, ICapability<TSubject>> _capabilitiesById = new(64);
    private readonly Dictionary<Type, List<int>> _typeToIds = new(16);
    private bool _built;

    // Cache primary marker type to avoid runtime reflection
    private static readonly Type PrimaryMarkerType = typeof(IPrimaryCapability<TSubject>);

    internal Composer(TSubject subject)
    {
        if (subject is null) throw new ArgumentNullException(nameof(subject));
        _subject = subject;
    }

    internal Composer(IComposition<TSubject> existingComposition)
    {
        if (existingComposition is null) throw new ArgumentNullException(nameof(existingComposition));
        _subject = existingComposition.Subject;
        
        SeedFromComposition(existingComposition);
    }

    public TSubject Subject => _subject;

    public Composer<TSubject> Add(ICapability<TSubject> capability)
    {
        if (_built)
        {
            throw new InvalidOperationException("Build() has already been called. This builder is no longer usable.");
        }
        
        if (capability is null) throw new ArgumentNullException(nameof(capability));

        var id = _nextCapabilityId++;
        _capabilitiesById[id] = capability;
        
        var concreteType = capability.GetType();
        RegisterIdUnderType(id, concreteType);
        
        if (capability is IPrimaryCapability<TSubject>)
        {
            RegisterIdUnderType(id, PrimaryMarkerType);
        }
        
        return this;
    }
    
    public Composer<TSubject> AddAs<TContract>(ICapability<TSubject> capability)
    {
        if (_built)
        {
            throw new InvalidOperationException("Build() has already been called. This builder is no longer usable.");
        }
        
        if (capability is null) throw new ArgumentNullException(nameof(capability));

        var contractType = typeof(TContract);
        
        return IsTupleType(contractType) ? AddAsMultipleContracts<TContract>(capability) : AddAsSingleContract<TContract>(capability);
    }

    public Composer<TSubject> TryAdd<TCapability>(TCapability capability) where TCapability : class, ICapability<TSubject>
    {
        if (!Has<TCapability>())
            return Add(capability);
        return this;
    }

    public Composer<TSubject> TryAddAs<TContract>(ICapability<TSubject> capability) where TContract : class, ICapability<TSubject>
    {
        if (!Has<TContract>())
            return AddAs<TContract>(capability);
        return this;
    }

    public Composer<TSubject> RemoveWhere(Func<ICapability<TSubject>, bool> predicate)
    {
        if (_built)
        {
            throw new InvalidOperationException("Build() has already been called. This builder is no longer usable.");
        }
        
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));

        var idsToRemove = new List<int>();
        
        foreach (var kvp in _capabilitiesById)
        {
            if (predicate(kvp.Value))
                idsToRemove.Add(kvp.Key);
        }
        
        foreach (var id in idsToRemove)
        {
            _capabilitiesById.Remove(id);
            
            foreach (var typeKvp in _typeToIds.ToList())
            {
                typeKvp.Value.Remove(id);
                if (typeKvp.Value.Count == 0)
                {
                    _typeToIds.Remove(typeKvp.Key);
                }
            }
        }
        
        return this;
    }

    public Composer<TSubject> WithPrimary(IPrimaryCapability<TSubject>? primary)
    {
        if (_built)
        {
            throw new InvalidOperationException("Build() has already been called. This builder is no longer usable.");
        }

        if (HasPrimary())
        {
            RemoveExistingPrimary();
        }

        if (primary != null)
        {
            var id = _nextCapabilityId++;
            _capabilitiesById[id] = primary;
            
            var concreteType = primary.GetType();
            RegisterIdUnderType(id, concreteType);
            
            RegisterIdUnderType(id, PrimaryMarkerType);
        }

        return this;
    }

    public bool HasPrimary()
    {
        return _typeToIds.ContainsKey(PrimaryMarkerType) && _typeToIds[PrimaryMarkerType].Count > 0;
    }

    public bool Has<TCapability>() where TCapability : class, ICapability<TSubject>
    {
        var queryType = typeof(TCapability);
        return _typeToIds.ContainsKey(queryType) && _typeToIds[queryType].Count > 0;
    }

    public IComposition<TSubject> Build()
    {
        if (_built)
        {
            throw new InvalidOperationException("Build() can only be called once. This builder is no longer usable.");
        }

        _built = true;

        var result = new Dictionary<Type, Array>(_typeToIds.Count);
        var totalCount = _capabilitiesById.Count;

        foreach (var typeKvp in _typeToIds)
        {
            var capabilities = new List<ICapability<TSubject>>();
            foreach (var id in typeKvp.Value)
            {
                capabilities.Add(_capabilitiesById[id]);
            }
            
            var ordered = SortCapabilities(capabilities);
            
            // Use Array.CreateInstance to avoid boxing with generic collections
            var arr = Array.CreateInstance(typeof(ICapability<TSubject>), ordered.Count);
            for (var i = 0; i < ordered.Count; i++)
            {
                arr.SetValue(ordered[i], i);
            }

            result[typeKvp.Key] = arr;
        }

        // Business rule: Only one primary capability allowed per subject
        if (result.TryGetValue(PrimaryMarkerType, out var primaryArr) && primaryArr.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple primary capabilities registered for '{typeof(TSubject).Name}'. Only one primary capability is allowed.");
        }

        var bag = new Composition<TSubject>(_subject, result, new Dictionary<Type, List<Type>>(), new Dictionary<ICapability<TSubject>, bool>(), totalCount);

        return bag;
    }

    private void RegisterIdUnderType(int id, Type type)
    {
        if (!_typeToIds.TryGetValue(type, out var list))
        {
            list = new List<int>();
            _typeToIds[type] = list;
        }
        list.Add(id);
    }

    private Composer<TSubject> AddAsSingleContract<TContract>(ICapability<TSubject> capability)
    {
        var contractType = typeof(TContract);
        
        if (!typeof(ICapability<TSubject>).IsAssignableFrom(contractType))
        {
            throw new ArgumentException($"Type '{contractType.Name}' must implement ICapability<{typeof(TSubject).Name}> to be registered as a capability contract.");
        }

        var id = _nextCapabilityId++;
        _capabilitiesById[id] = capability;
        
        RegisterIdUnderType(id, contractType);
        
        if (contractType.IsGenericType && contractType.GetGenericTypeDefinition() == typeof(IPrimaryCapability<>))
        {
            if (_typeToIds.ContainsKey(contractType))
            {
                throw new InvalidOperationException(
                    $"A primary capability is already set for '{typeof(TSubject).Name}'. Only one primary capability is allowed.");
            }
        }

        return this;
    }

    private Composer<TSubject> AddAsMultipleContracts<TContract>(ICapability<TSubject> capability)
    {
        var contractTypes = TupleTypeExtractor.GetTupleTypes<TContract>();
        
        TupleTypeExtractor.ValidateCapabilityTypes<TSubject>(contractTypes);

        var id = _nextCapabilityId++;
        _capabilitiesById[id] = capability;

        foreach (var contractType in contractTypes)
        {
            if (contractType.IsGenericType && contractType.GetGenericTypeDefinition() == typeof(IPrimaryCapability<>))
            {
                if (_typeToIds.ContainsKey(contractType))
                {
                    throw new InvalidOperationException(
                        $"A primary capability is already set for '{typeof(TSubject).Name}'. Only one primary capability is allowed.");
                }
            }
            
            RegisterIdUnderType(id, contractType);
        }

        return this;
    }

    private void RemoveExistingPrimary()
    {
        RemoveWhere(cap => cap is IPrimaryCapability<TSubject>);
    }

    private void SeedFromComposition(IComposition<TSubject> existingComposition)
    {
        if (existingComposition is not Composition<TSubject> internalComposition)
        {
            throw new ArgumentException("Recompose only supports compositions created by this system", nameof(existingComposition));
        }
        
        var capabilitiesByType = internalComposition.GetCapabilitiesByType();
        
        foreach (var typeKvp in capabilitiesByType)
        {
            foreach (ICapability<TSubject> capability in typeKvp.Value)
            {
                var id = _nextCapabilityId++;
                _capabilitiesById[id] = capability;
                RegisterIdUnderType(id, typeKvp.Key);
            }
        }
    }

    private static bool IsTupleType(Type type)
    {
        return type.IsGenericType && 
               type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;
    }

    private static List<ICapability<TSubject>> SortCapabilities(List<ICapability<TSubject>> capabilities)
    {
        if (capabilities.Count <= 1)
            return capabilities;

        bool hasOrderedCapabilities = false;
        for (int i = 0; i < capabilities.Count; i++)
        {
            if (capabilities[i] is IOrderedCapability)
            {
                hasOrderedCapabilities = true;
                break;
            }
        }

        if (!hasOrderedCapabilities)
            return capabilities;

        var sorted = new List<ICapability<TSubject>>(capabilities.Count);
        for (int i = 0; i < capabilities.Count; i++)
        {
            sorted.Add(capabilities[i]);
        }

        for (int i = 1; i < sorted.Count; i++)
        {
            var current = sorted[i];
            var currentOrder = (current as IOrderedCapability)?.Order ?? 0;
            var currentIndex = capabilities.IndexOf(current);
            
            int j = i - 1;
            while (j >= 0)
            {
                var compareOrder = (sorted[j] as IOrderedCapability)?.Order ?? 0;
                var compareIndex = capabilities.IndexOf(sorted[j]);
                
                if (currentOrder < compareOrder || 
                    (currentOrder == compareOrder && currentIndex < compareIndex))
                {
                    sorted[j + 1] = sorted[j];
                    j--;
                }
                else
                {
                    break;
                }
            }
            sorted[j + 1] = current;
        }

        return sorted;
    }
}
