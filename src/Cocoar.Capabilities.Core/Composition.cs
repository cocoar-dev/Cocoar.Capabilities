namespace Cocoar.Capabilities.Core;

internal sealed class Composition<TSubject>(
    TSubject subject,
    IReadOnlyDictionary<Type, Array> capabilitiesByType,
    IReadOnlyDictionary<Type, List<Type>> contractToConcreteMap,
    IReadOnlyDictionary<ICapability<TSubject>, bool> contractOnlyInstances,
    int totalCapabilityCount)
    : IComposition<TSubject>
{
    private readonly IReadOnlyDictionary<Type, Array> _capabilitiesByType = capabilitiesByType ?? throw new ArgumentNullException(nameof(capabilitiesByType));
    private readonly IReadOnlyDictionary<Type, List<Type>> _contractToConcreteMap = contractToConcreteMap ?? throw new ArgumentNullException(nameof(contractToConcreteMap));
    private readonly IReadOnlyDictionary<ICapability<TSubject>, bool> _contractOnlyInstances = contractOnlyInstances ?? throw new ArgumentNullException(nameof(contractOnlyInstances));

    public TSubject Subject { get; } = subject ?? throw new ArgumentNullException(nameof(subject));

    object IComposition.Subject => Subject!;

    public int TotalCapabilityCount => totalCapabilityCount;

    public bool HasPrimary()
    {
        return Has<IPrimaryCapability<TSubject>>();
    }

    public bool HasPrimary<TPrimaryCapability>()
        where TPrimaryCapability : class, IPrimaryCapability<TSubject>
    {
        return Has<TPrimaryCapability>();
    }

    public bool TryGetPrimary(out IPrimaryCapability<TSubject> primary)
    {
        var primaryCapabilities = GetAll<IPrimaryCapability<TSubject>>();
        if (primaryCapabilities.Count > 0)
        {
            primary = primaryCapabilities[0];
            return true;
        }
        primary = null!;
        return false;
    }

    public IPrimaryCapability<TSubject>? GetPrimaryOrDefault()
    {
        TryGetPrimary(out var primary);
        return primary;
    }

    public IPrimaryCapability<TSubject> GetPrimary()
    {
        if (TryGetPrimary(out var primary))
        {
            return primary;
        }
        throw new InvalidOperationException($"Primary capability not found for subject '{Subject?.GetType().Name}'.");
    }

    public bool TryGetPrimaryAs<TPrimaryCapability>(out TPrimaryCapability primary)
        where TPrimaryCapability : class, IPrimaryCapability<TSubject>
    {
        if (TryGetPrimary(out var basePrimary) && basePrimary is TPrimaryCapability typed)
        {
            primary = typed;
            return true;
        }
        primary = null!;
        return false;
    }

    public TPrimaryCapability? GetPrimaryOrDefaultAs<TPrimaryCapability>()
        where TPrimaryCapability : class, IPrimaryCapability<TSubject>
    {
        TryGetPrimaryAs<TPrimaryCapability>(out var primary);
        return primary;
    }

    public TPrimaryCapability GetRequiredPrimaryAs<TPrimaryCapability>()
        where TPrimaryCapability : class, IPrimaryCapability<TSubject>
    {
        if (TryGetPrimaryAs<TPrimaryCapability>(out var primary))
        {
            return primary;
        }
        throw new InvalidOperationException(
            $"Primary capability of type '{typeof(TPrimaryCapability).Name}' not found for subject '{typeof(TSubject).Name}'.");
    }

    public IReadOnlyList<TCapability> GetAll<TCapability>() 
        where TCapability : class, ICapability<TSubject>
    {
        var queryType = typeof(TCapability);
        var filtered = new List<TCapability>();

        var isContractQuery = _contractToConcreteMap.ContainsKey(queryType);
        
        var storageTypes = isContractQuery ? _contractToConcreteMap[queryType] : [queryType];

        foreach (var storageType in storageTypes)
        {
            if (_capabilitiesByType.TryGetValue(storageType, out var arr))
            {
                var concreteArray = (Array)arr;
                
                for (var i = 0; i < concreteArray.Length; i++)
                {
                    var item = concreteArray.GetValue(i);
                    if (item is TCapability capability)
                    {
                        if (!isContractQuery && 
                            _contractOnlyInstances.ContainsKey(capability))
                        {
                            continue;
                        }
                        
                        filtered.Add(capability);
                    }
                }
            }
        }

        if (filtered.Count > 1)
        {
            var hasOrderedCapabilities = false;
            for (int i = 0; i < filtered.Count; i++)
            {
                if (filtered[i] is IOrderedCapability)
                {
                    hasOrderedCapabilities = true;
                    break;
                }
            }
            
            if (hasOrderedCapabilities)
            {
                filtered.Sort((x, y) =>
                {
                    var orderX = x is IOrderedCapability orderedX ? orderedX.Order : 0;
                    var orderY = y is IOrderedCapability orderedY ? orderedY.Order : 0;
                    return orderX.CompareTo(orderY);
                });
            }
        }

        if (filtered.Count == 0)
        {
            return [];
        }
        
        var result = new TCapability[filtered.Count];
        for (int i = 0; i < filtered.Count; i++)
        {
            result[i] = filtered[i];
        }
        return result;
    }

    public IReadOnlyList<ICapability<TSubject>> GetAll()
    {
        if (_capabilitiesByType.Count == 0)
            return [];
        
        var allCapabilities = new List<ICapability<TSubject>>(totalCapabilityCount);
        
        foreach (var array in _capabilitiesByType.Values)
        {
            foreach (ICapability<TSubject> capability in array)
            {
                allCapabilities.Add(capability);
            }
        }
        
        if (allCapabilities.Count > 1)
        {
            var hasOrderedCapabilities = false;
            for (int i = 0; i < allCapabilities.Count; i++)
            {
                if (allCapabilities[i] is IOrderedCapability)
                {
                    hasOrderedCapabilities = true;
                    break;
                }
            }
            
            if (hasOrderedCapabilities)
            {
                allCapabilities.Sort((x, y) =>
                {
                    var orderX = x is IOrderedCapability orderedX ? orderedX.Order : 0;
                    var orderY = y is IOrderedCapability orderedY ? orderedY.Order : 0;
                    return orderX.CompareTo(orderY);
                });
            }
        }
        
        var result = new ICapability<TSubject>[allCapabilities.Count];
        for (int i = 0; i < allCapabilities.Count; i++)
        {
            result[i] = allCapabilities[i];
        }
        return result;
    }

    public bool Has<TCapability>() 
        where TCapability : class, ICapability<TSubject>
    {
        var queryType = typeof(TCapability);
        var isContractQuery = _contractToConcreteMap.ContainsKey(queryType);

        var storageTypes = isContractQuery ? _contractToConcreteMap[queryType] : [queryType];
        
        foreach (var storageType in storageTypes)
        {
            if (_capabilitiesByType.TryGetValue(storageType, out var arr))
            {
                if (isContractQuery && arr.Length > 0)
                {
                    return true;
                }
                
                if (!isContractQuery)
                {
                    for (var i = 0; i < arr.Length; i++)
                    {
                        var item = arr.GetValue(i);
                        if (item is TCapability && !_contractOnlyInstances.ContainsKey((ICapability<TSubject>)item))
                        {
                            return true;
                        }
                    }
                }
            }
        }
        
        return false;
    }

    public int Count<TCapability>() 
        where TCapability : class, ICapability<TSubject>
    {
        var queryType = typeof(TCapability);
        var isContractQuery = _contractToConcreteMap.ContainsKey(queryType);
        var totalCount = 0;

        var storageTypes = isContractQuery ? _contractToConcreteMap[queryType] : [queryType];
        
        foreach (var storageType in storageTypes)
        {
            if (_capabilitiesByType.TryGetValue(storageType, out var arr))
            {
                if (isContractQuery)
                {
                    totalCount += arr.Length;
                }
                else
                {
                    for (var i = 0; i < arr.Length; i++)
                    {
                        var item = arr.GetValue(i);
                        if (item is TCapability capability && !_contractOnlyInstances.ContainsKey(capability))
                        {
                            totalCount++;
                        }
                    }
                }
            }
        }
        
        return totalCount;
    }

    internal IReadOnlyDictionary<Type, Array> GetCapabilitiesByType() => _capabilitiesByType;
    internal IReadOnlyDictionary<Type, List<Type>> GetContractToConcreteMap() => _contractToConcreteMap;
    internal IReadOnlyDictionary<ICapability<TSubject>, bool> GetContractOnlyInstances() => _contractOnlyInstances;
}
