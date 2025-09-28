namespace Cocoar.Capabilities;

internal sealed class CapabilityStore
{
    private int _nextCapabilityId;
    private readonly Dictionary<int, (object capability, int? order)> _capabilitiesById = new(64);
    private readonly Dictionary<Type, List<int>> _typeToIds = new(16);

    internal static readonly Type PrimaryMarkerType = typeof(IPrimaryCapability);

    internal bool HasPrimary() => _typeToIds.TryGetValue(PrimaryMarkerType, out var list) && list.Count > 0;

    internal bool Has<TCapability>() where TCapability : class
        => _typeToIds.TryGetValue(typeof(TCapability), out var list) && list.Count > 0;

    internal void Add(object capability, Type singleType, bool isPrimary, int? order)
    {
        int id = _nextCapabilityId++;
        _capabilitiesById[id] = (capability, order);
        RegisterIdUnderType(id, singleType);
        if (isPrimary && singleType != PrimaryMarkerType)
        {
            RegisterIdUnderType(id, PrimaryMarkerType);
        }
    }

    internal void Add(object capability, IEnumerable<Type> types, bool includesPrimary, int? order)
    {
        int id = _nextCapabilityId++;
        _capabilitiesById[id] = (capability, order);
        bool containsMarker = false;
        foreach (var t in types)
        {
            RegisterIdUnderType(id, t);
            if (t == PrimaryMarkerType)
            {
                containsMarker = true;
            }
        }
        if (includesPrimary && !containsMarker)
        {
            RegisterIdUnderType(id, PrimaryMarkerType);
        }
    }

    internal void RemoveWhere(Func<object, bool> predicate)
    {
        var idsToRemove = new List<int>();
        foreach (var kvp in _capabilitiesById)
        {
            if (predicate(kvp.Value.capability))
            {
                idsToRemove.Add(kvp.Key);
            }
        }

        foreach (int id in idsToRemove)
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
    }

    internal void RemoveExistingPrimary() => RemoveWhere(c => c is IPrimaryCapability);

    internal void SeedFromComposition(IComposition composition)
    {
        // Use reflection to access internal method. Not cached because:
        // - Recompose is rare, each CapabilityStore used once
        // - Overhead negligible vs. actual recompose work
        var compositionType = composition.GetType();
        var getMethod = compositionType.GetMethod("GetCapabilitiesByType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (getMethod == null)
        {
            throw new ArgumentException("Recompose only supports compositions created by this system", nameof(composition));
        }

        var capabilitiesByType = (IReadOnlyDictionary<Type, Array>)getMethod.Invoke(composition, null)!;
        
        // Collect all unique capabilities by insertionId to avoid duplicates.
        // Build mapping of insertionId -> (metadata, types) in one pass.
        var metadataById = new Dictionary<int, (CapabilityMetadata metadata, List<Type> types)>();
        
        foreach (var typeKvp in capabilitiesByType)
        {
            var metadataArr = (CapabilityMetadata[])typeKvp.Value;
            for (int i = 0; i < metadataArr.Length; i++)
            {
                var metadata = metadataArr[i];
                if (metadataById.TryGetValue(metadata.InsertionId, out var existing))
                {
                    existing.types.Add(typeKvp.Key);
                }
                else
                {
                    var types = new List<Type>(4) { typeKvp.Key };
                    metadataById[metadata.InsertionId] = (metadata, types);
                }
            }
        }
        
        // Add capabilities in insertion order to maintain stable IDs
        foreach (var kvp in metadataById.OrderBy(x => x.Key))
        {
            var (metadata, types) = kvp.Value;
            int id = _nextCapabilityId++;
            _capabilitiesById[id] = (metadata.Capability, metadata.Order);
            
            // Register under all types this capability appeared in
            for (int i = 0; i < types.Count; i++)
            {
                RegisterIdUnderType(id, types[i]);
            }
        }
    }

    internal (Dictionary<Type, Array> result, int totalCount) BuildCapabilityArrays()
        => CapabilityArrayBuilder.Build(_capabilitiesById, _typeToIds);

    private void RegisterIdUnderType(int id, Type type)
    {
        if (!_typeToIds.TryGetValue(type, out var list))
        {
            list = [];
            _typeToIds[type] = list;
        }
        list.Add(id);
    }
}
