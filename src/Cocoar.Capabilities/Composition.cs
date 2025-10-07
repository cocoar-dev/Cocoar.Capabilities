namespace Cocoar.Capabilities;

internal sealed class Composition<TSubject> : IComposition<TSubject> where TSubject : notnull
{
    private IReadOnlyDictionary<Type, Array> _capabilitiesByType;
    private int _totalCapabilityCount;

    internal Composition(
        TSubject subject,
        IReadOnlyDictionary<Type, Array> capabilitiesByType,
        int totalCapabilityCount)
    {
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        _capabilitiesByType = capabilitiesByType ?? throw new ArgumentNullException(nameof(capabilitiesByType));
        _totalCapabilityCount = totalCapabilityCount;
    }

    public TSubject Subject { get; }

    object IComposition.Subject => Subject!;

    public int TotalCapabilityCount => _totalCapabilityCount;

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

    public IReadOnlyList<ICapability<TSubject>> GetAll()
    {
        if (_capabilitiesByType.Count == 0)
            return Array.Empty<ICapability<TSubject>>();

        var list = new List<ICapability<TSubject>>(_totalCapabilityCount);
        foreach (var array in _capabilitiesByType.Values)
        {
            for (int i = 0; i < array.Length; i++)
            {
                list.Add((ICapability<TSubject>)array.GetValue(i)!);
            }
        }

        if (list.Count > 1)
        {
            // Stable global ordering across different type buckets.
            bool hasOrdered = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is IOrderedCapability)
                {
                    hasOrdered = true; break;
                }
            }
            if (hasOrdered)
            {
                // Use stable sort (CapabilityOrdering) via a temp typed list.
                CapabilityOrdering.SortInPlace(list);
            }
        }

        return list.Count == 0 ? Array.Empty<ICapability<TSubject>>() : list.ToArray();
    }

    public bool Has<TCapability>() 
        where TCapability : class, ICapability<TSubject>
    {
        var queryType = typeof(TCapability);
        if (!_capabilitiesByType.TryGetValue(queryType, out var arr) || arr.Length == 0) return false;
        // Since array only stores capabilities registered for this type, first element existence suffices.
        return true;
    }

    public int Count<TCapability>() 
        where TCapability : class, ICapability<TSubject>
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
