namespace Cocoar.Capabilities;

/// <summary>
/// Internal storage for capabilities attached to an object instance.
/// This is instance-based, not type-based.
/// </summary>
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
        var id = _nextCapabilityId++;
        _capabilitiesById[id] = (capability, order);
        RegisterIdUnderType(id, singleType);
        if (isPrimary && singleType != PrimaryMarkerType)
        {
            RegisterIdUnderType(id, PrimaryMarkerType);
        }
    }

    internal void Add(object capability, IEnumerable<Type> types, bool includesPrimary, int? order)
    {
        var id = _nextCapabilityId++;
        _capabilitiesById[id] = (capability, order);
        bool containsMarker = false;
        foreach (var t in types)
        {
            RegisterIdUnderType(id, t);
            if (t == PrimaryMarkerType) containsMarker = true;
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
            if (predicate(kvp.Value.capability)) idsToRemove.Add(kvp.Key);
        }

        foreach (var id in idsToRemove)
        {
            _capabilitiesById.Remove(id);
            foreach (var typeKvp in _typeToIds.ToList())
            {
                typeKvp.Value.Remove(id);
                if (typeKvp.Value.Count == 0) _typeToIds.Remove(typeKvp.Key);
            }
        }
    }

    internal void RemoveExistingPrimary() => RemoveWhere(c => c is IPrimaryCapability);

    internal void SeedFromComposition(IComposition composition)
    {
        // Use reflection to access internal method since Composition is generic
        var compositionType = composition.GetType();
        var getMethod = compositionType.GetMethod("GetCapabilitiesByType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (getMethod == null)
        {
            throw new ArgumentException("Recompose only supports compositions created by this system", nameof(composition));
        }

        var capabilitiesByType = (IReadOnlyDictionary<Type, Array>)getMethod.Invoke(composition, null)!;
        foreach (var typeKvp in capabilitiesByType)
        {
            foreach (object capability in typeKvp.Value)
            {
                var id = _nextCapabilityId++;
                _capabilitiesById[id] = (capability, null);  // No order info when recomposing
                RegisterIdUnderType(id, typeKvp.Key);
            }
        }
    }

    internal (Dictionary<Type, Array> result, int totalCount) BuildCapabilityArrays()
        => CapabilityArrayBuilder.Build(_capabilitiesById, _typeToIds);

    private void RegisterIdUnderType(int id, Type type)
    {
        if (!_typeToIds.TryGetValue(type, out var list))
        {
            list = new List<int>();
            _typeToIds[type] = list;
        }
        list.Add(id);
    }
}
