namespace Cocoar.Capabilities;

internal sealed class Composition : IComposition
{
    private IReadOnlyDictionary<Type, Array> _capabilitiesByType;
    private int _totalCapabilityCount;

    internal Composition(
        object subject,
        IReadOnlyDictionary<Type, Array> capabilitiesByType,
        int totalCapabilityCount)
    {
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        _capabilitiesByType = capabilitiesByType ?? throw new ArgumentNullException(nameof(capabilitiesByType));
        _totalCapabilityCount = totalCapabilityCount;
    }

    public object Subject { get; }

    public int TotalCapabilityCount => _totalCapabilityCount;

    public bool HasPrimary()
    {
        return Has<IPrimaryCapability>();
    }

    public bool HasPrimary<TPrimaryCapability>()
        where TPrimaryCapability : class, IPrimaryCapability
    {
        return Has<TPrimaryCapability>();
    }

    public bool TryGetPrimary(out IPrimaryCapability primary)
    {
        var primaryCapabilities = GetAll<IPrimaryCapability>();
        if (primaryCapabilities.Count > 0)
        {
            primary = primaryCapabilities[0];
            return true;
        }
        primary = null!;
        return false;
    }

    public IPrimaryCapability? GetPrimaryOrDefault()
    {
        TryGetPrimary(out var primary);
        return primary;
    }

    public IPrimaryCapability GetPrimary()
    {
        if (TryGetPrimary(out var primary))
        {
            return primary;
        }
        throw new InvalidOperationException($"Primary capability not found for subject '{Subject?.GetType().Name}'.");
    }

    public bool TryGetPrimaryAs<TPrimaryCapability>(out TPrimaryCapability primary)
        where TPrimaryCapability : class, IPrimaryCapability
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
        where TPrimaryCapability : class, IPrimaryCapability
    {
        TryGetPrimaryAs<TPrimaryCapability>(out var primary);
        return primary;
    }

    public TPrimaryCapability GetRequiredPrimaryAs<TPrimaryCapability>()
        where TPrimaryCapability : class, IPrimaryCapability
    {
        if (TryGetPrimaryAs<TPrimaryCapability>(out var primary))
        {
            return primary;
        }
        throw new InvalidOperationException(
            $"Primary capability of type '{typeof(TPrimaryCapability).Name}' not found.");
    }

    public IReadOnlyList<TCapability> GetAll<TCapability>() 
        where TCapability : class
    {
        var queryType = typeof(TCapability);
        if (!_capabilitiesByType.TryGetValue(queryType, out var arr) || arr.Length == 0)
        {
            return Array.Empty<TCapability>();
        }

        // Arrays are already stably ordered during build; just project to the typed result.
        var typed = new TCapability[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            typed[i] = (TCapability)arr.GetValue(i)!;
        }
        return typed;
    }

    public IReadOnlyList<object> GetAll()
    {
        if (_capabilitiesByType.Count == 0)
            return Array.Empty<object>();

        var list = new List<object>(_totalCapabilityCount);
        foreach (var array in _capabilitiesByType.Values)
        {
            for (int i = 0; i < array.Length; i++)
            {
                list.Add(array.GetValue(i)!);
            }
        }

        // NOTE: Arrays are already sorted by order during Build().
        // However, when we combine capabilities from different type buckets,
        // we need to maintain stable ordering across the entire list.
        // Since we don't have order information here, we'll return them
        // in the order they were stored (by type bucket).
        
        return list.AsReadOnly();
    }

    public bool Has<TCapability>() 
        where TCapability : class
    {
        var queryType = typeof(TCapability);
        if (!_capabilitiesByType.TryGetValue(queryType, out var arr) || arr.Length == 0) return false;
        // Since array only stores capabilities registered for this type, first element existence suffices.
        return true;
    }

    public int Count<TCapability>() 
        where TCapability : class
    {
        var queryType = typeof(TCapability);
        if (!_capabilitiesByType.TryGetValue(queryType, out var arr) || arr.Length == 0) return 0;
        return arr.Length; // All entries in the bucket are of the registered type.
    }

    internal IReadOnlyDictionary<Type, Array> GetCapabilitiesByType() => _capabilitiesByType;
    internal void UpdateCapabilities(
        IReadOnlyDictionary<Type, Array> capabilitiesByType,
        int totalCapabilityCount)
    {
        _capabilitiesByType = capabilitiesByType ?? throw new ArgumentNullException(nameof(capabilitiesByType));
        _totalCapabilityCount = totalCapabilityCount;
    }
}
