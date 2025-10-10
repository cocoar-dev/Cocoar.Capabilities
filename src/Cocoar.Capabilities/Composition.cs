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

        // Arrays store CapabilityMetadata, extract just the capabilities
        var metadataArr = (CapabilityMetadata[])arr;
        var typed = new TCapability[metadataArr.Length];
        for (int i = 0; i < metadataArr.Length; i++)
        {
            typed[i] = (TCapability)metadataArr[i].Capability;
        }
        return typed;
    }

    public IReadOnlyList<object> GetAll()
    {
        if (_capabilitiesByType.Count == 0)
            return Array.Empty<object>();

        // Collect all capabilities with their metadata for global sorting
        var allMetadata = new List<CapabilityMetadata>(_totalCapabilityCount);
        var seen = new HashSet<object>(_totalCapabilityCount);
        
        foreach (var array in _capabilitiesByType.Values)
        {
            var metadataArr = (CapabilityMetadata[])array;
            for (int i = 0; i < metadataArr.Length; i++)
            {
                var metadata = metadataArr[i];
                // Deduplicate: same capability may appear under multiple types
                if (seen.Add(metadata.Capability))
                {
                    allMetadata.Add(metadata);
                }
            }
        }

        // Sort by (order ?? 0, insertionId) for global deterministic ordering
        allMetadata.Sort((a, b) =>
        {
            int oa = a.Order ?? 0;
            int ob = b.Order ?? 0;
            if (oa != ob)
            {
                return oa.CompareTo(ob);
            }
            return a.InsertionId.CompareTo(b.InsertionId);
        });

        // Extract just the capabilities
        var result = new object[allMetadata.Count];
        for (int i = 0; i < allMetadata.Count; i++)
        {
            result[i] = allMetadata[i].Capability;
        }
        
        return result;
    }

    public TCapability? GetFirstOrDefault<TCapability>() 
        where TCapability : class
    {
        var queryType = typeof(TCapability);
        if (!_capabilitiesByType.TryGetValue(queryType, out var arr) || arr.Length == 0)
        {
            return null;
        }
        var metadataArr = (CapabilityMetadata[])arr;
        return (TCapability)metadataArr[0].Capability;
    }

    public TCapability GetRequiredFirst<TCapability>() 
        where TCapability : class
    {
        var queryType = typeof(TCapability);
        if (!_capabilitiesByType.TryGetValue(queryType, out var arr) || arr.Length == 0)
        {
            throw new InvalidOperationException(
                $"Capability of type '{typeof(TCapability).Name}' not found.");
        }
        var metadataArr = (CapabilityMetadata[])arr;
        return (TCapability)metadataArr[0].Capability;
    }

    public bool TryGetFirst<TCapability>(out TCapability capability) 
        where TCapability : class
    {
        var queryType = typeof(TCapability);
        if (_capabilitiesByType.TryGetValue(queryType, out var arr) && arr.Length > 0)
        {
            var metadataArr = (CapabilityMetadata[])arr;
            capability = (TCapability)metadataArr[0].Capability;
            return true;
        }
        capability = null!;
        return false;
    }

    public TCapability? GetLastOrDefault<TCapability>() 
        where TCapability : class
    {
        var queryType = typeof(TCapability);
        if (!_capabilitiesByType.TryGetValue(queryType, out var arr) || arr.Length == 0)
        {
            return null;
        }
        var metadataArr = (CapabilityMetadata[])arr;
        return (TCapability)metadataArr[^1].Capability;
    }

    public TCapability GetRequiredLast<TCapability>() 
        where TCapability : class
    {
        var queryType = typeof(TCapability);
        if (!_capabilitiesByType.TryGetValue(queryType, out var arr) || arr.Length == 0)
        {
            throw new InvalidOperationException(
                $"Capability of type '{typeof(TCapability).Name}' not found.");
        }
        var metadataArr = (CapabilityMetadata[])arr;
        return (TCapability)metadataArr[^1].Capability;
    }

    public bool TryGetLast<TCapability>(out TCapability capability) 
        where TCapability : class
    {
        var queryType = typeof(TCapability);
        if (_capabilitiesByType.TryGetValue(queryType, out var arr) && arr.Length > 0)
        {
            var metadataArr = (CapabilityMetadata[])arr;
            capability = (TCapability)metadataArr[^1].Capability;
            return true;
        }
        capability = null!;
        return false;
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
