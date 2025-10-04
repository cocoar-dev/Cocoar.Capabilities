namespace Cocoar.Capabilities;

internal sealed class CapabilityStore<TSubject> where TSubject : notnull
{
    private int _nextCapabilityId;
    private readonly Dictionary<int, ICapability<TSubject>> _capabilitiesById = new(64);
    private readonly Dictionary<Type, List<int>> _typeToIds = new(16);

    internal static readonly Type PrimaryMarkerType = typeof(IPrimaryCapability<TSubject>);

    internal bool HasPrimary() => _typeToIds.TryGetValue(PrimaryMarkerType, out var list) && list.Count > 0;

    internal bool Has<TCapability>() where TCapability : class, ICapability<TSubject>
        => _typeToIds.TryGetValue(typeof(TCapability), out var list) && list.Count > 0;

    internal void Add(ICapability<TSubject> capability, Type singleType, bool isPrimary)
    {
        var id = _nextCapabilityId++;
        _capabilitiesById[id] = capability;
        RegisterIdUnderType(id, singleType);
        if (isPrimary && singleType != PrimaryMarkerType)
        {
            RegisterIdUnderType(id, PrimaryMarkerType);
        }
    }

    internal void Add(ICapability<TSubject> capability, IEnumerable<Type> types, bool includesPrimary)
    {
        var id = _nextCapabilityId++;
        _capabilitiesById[id] = capability;
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

    internal void RemoveWhere(Func<ICapability<TSubject>, bool> predicate)
    {
        var idsToRemove = new List<int>();
        foreach (var kvp in _capabilitiesById)
        {
            if (predicate(kvp.Value)) idsToRemove.Add(kvp.Key);
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

    internal void RemoveExistingPrimary() => RemoveWhere(c => c is IPrimaryCapability<TSubject>);

    internal void SeedFromComposition(IComposition<TSubject> existingComposition)
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
