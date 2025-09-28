using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace Cocoar.Capabilities;

/// <summary>
/// Immutable, thread-safe implementation of ICapabilityBag.
/// Uses Dictionary&lt;Type, Array&gt; storage with type-safe arrays for zero-allocation retrieval.
/// </summary>
/// <typeparam name="TSubject">The type of subject this bag contains capabilities for</typeparam>
internal sealed class CapabilityBag<TSubject> : ICapabilityBag<TSubject>
{
    private readonly TSubject _subject;
    private readonly IReadOnlyDictionary<Type, Array> _capabilitiesByType;
    private readonly int _totalCapabilityCount;
    private readonly CapabilityBagBuildOptions _options;
    // Per capability-type index: tag -> typed array of capabilities (allocation-free lookups)
    private readonly IReadOnlyDictionary<Type, FrozenDictionary<object, Array>> _tagsIndex;
    // All unique tags across the whole bag
    private readonly FrozenSet<object> _allTags;
    // Lazy cache for typed tag sets (e.g., string tags), keyed by TTag
    private readonly ConcurrentDictionary<Type, object> _typedTagsCache = new();

    /// <summary>
    /// Initializes a new capability bag with the specified subject and capabilities.
    /// </summary>
    /// <param name="subject">The subject this bag is associated with</param>
    /// <param name="capabilitiesByType">Dictionary mapping capability types to their typed arrays</param>
    /// <param name="totalCapabilityCount">Total count of all capabilities across all types</param>
    /// <param name="options">Build options controlling indexing and tag caching behavior</param>
    /// <exception cref="ArgumentNullException">Thrown when subject is null</exception>
    public CapabilityBag(
        TSubject subject, 
        IReadOnlyDictionary<Type, Array> capabilitiesByType, 
        int totalCapabilityCount,
        CapabilityBagBuildOptions options)
    {
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
        _capabilitiesByType = capabilitiesByType ?? throw new ArgumentNullException(nameof(capabilitiesByType));
        _totalCapabilityCount = totalCapabilityCount;
        _options = options;

        // Build indices for fast, allocation-free tag queries (or skip based on options)
        var doIndex = _options.TagIndexing switch
        {
            TagIndexingMode.Eager => true,
            TagIndexingMode.None => false,
            TagIndexingMode.Auto => _totalCapabilityCount >= (_options.AutoIndexThreshold > 0 ? _options.AutoIndexThreshold : CapabilityBagBuildOptions.Default.AutoIndexThreshold),
            _ => true
        };

        if (doIndex)
        {
            BuildTagIndices(_options.IndexMinFrequency, out _tagsIndex, out _allTags);
        }
        else
        {
            _tagsIndex = new Dictionary<Type, FrozenDictionary<object, Array>>();
            // Even when skipping indices, we still compute the set of all tags for discovery APIs
            _allTags = BuildAllTagsOnly();
        }
    }

    /// <inheritdoc />
    public TSubject Subject => _subject;

    /// <inheritdoc />
    public int TotalCapabilityCount => _totalCapabilityCount;

    /// <inheritdoc />
    public bool TryGet<TCapability>(out TCapability capability) 
        where TCapability : class, ICapability<TSubject>
    {
        if (_capabilitiesByType.TryGetValue(typeof(TCapability), out var arr) && arr.Length > 0)
        {
            // Safe: array element type is exactly TCapability due to typed array creation
            capability = ((TCapability[])arr)[0];
            return true;
        }
        
        capability = null!;
        return false;
    }

    /// <inheritdoc />
    public TCapability GetRequired<TCapability>() 
        where TCapability : class, ICapability<TSubject>
    {
        if (TryGet<TCapability>(out var capability))
        {
            return capability;
        }

        // Create helpful error message with available capability types
        var availableTypes = _capabilitiesByType.Keys
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        var availableTypesStr = availableTypes.Count > 0 
            ? $"[{string.Join(", ", availableTypes)}]"
            : "[none]";

        var message = $"Capability '{typeof(TCapability).Name}' not found for subject '{Subject?.GetType().Name}'. " +
                     $"Available: {availableTypesStr}";

        throw new InvalidOperationException(message);
    }

    /// <inheritdoc />
    public IReadOnlyList<TCapability> GetAll<TCapability>() 
        where TCapability : class, ICapability<TSubject>
    {
        return _capabilitiesByType.TryGetValue(typeof(TCapability), out var arr)
            ? (TCapability[])arr // Safe: array is typed exactly as TCapability[]
            : Array.Empty<TCapability>(); // Zero-allocation empty result
    }

    /// <inheritdoc />
    public IReadOnlyList<TCapability> GetAllByTag<TCapability>(object tag) 
        where TCapability : class, ICapability<TSubject>
    {
        ArgumentNullException.ThrowIfNull(tag);

        // Fast path: use precomputed tag index
        if (_tagsIndex.TryGetValue(typeof(TCapability), out var perType) &&
            perType.TryGetValue(tag, out var arr))
        {
            return (TCapability[])arr;
        }

        // Fallback: scan this capability type only; allocate only for results
        if (_capabilitiesByType.TryGetValue(typeof(TCapability), out var all))
        {
            var typed = (TCapability[])all;
            // Worst case capacity typed.Length, but we try to keep it small
            var list = new List<TCapability>();
            foreach (var cap in typed)
            {
                if (cap is ITaggedCapability<TSubject> taggedCap && taggedCap.Tags.Contains(tag))
                {
                    list.Add(cap);
                }
            }
            return list.Count == 0 ? Array.Empty<TCapability>() : list.ToArray();
        }

        return Array.Empty<TCapability>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TCapability> GetAllByTags<TCapability>(params object[] tags)
        where TCapability : class, ICapability<TSubject>
    {
        ArgumentNullException.ThrowIfNull(tags);
        if (tags.Length == 0)
            return Array.Empty<TCapability>();
        if (tags.Length == 1)
            return GetAllByTag<TCapability>(tags[0]);

        // Use precomputed per-tag arrays and intersect
        _tagsIndex.TryGetValue(typeof(TCapability), out var perType);

        // Gather arrays for indexed tags; keep list of tags that are not indexed for fallback checks
        var arrays = new List<TCapability[]>(tags.Length);
        var nonIndexedTags = new List<object>();
        foreach (var tag in tags)
        {
            if (perType != null && perType.TryGetValue(tag, out var tagArr))
            {
                arrays.Add((TCapability[])tagArr);
            }
            else
            {
                nonIndexedTags.Add(tag);
            }
        }

        // If none were indexed, fall back to single-scan filtering over this capability type
        if (arrays.Count == 0)
        {
            if (!_capabilitiesByType.TryGetValue(typeof(TCapability), out var all))
                return Array.Empty<TCapability>();

            var typed = (TCapability[])all;
            var list = new List<TCapability>();
            foreach (var cap in typed)
            {
                if (cap is ITaggedCapability<TSubject> taggedCap)
                {
                    bool ok = true;
                    for (int i = 0; i < tags.Length; i++)
                    {
                        if (!taggedCap.Tags.Contains(tags[i])) { ok = false; break; }
                    }
                    if (ok) list.Add(cap);
                }
            }
            return list.Count == 0 ? Array.Empty<TCapability>() : list.ToArray();
        }

        // Choose the smallest as baseline to minimize checks
        arrays.Sort((a, b) => a.Length.CompareTo(b.Length));
        var baseline = arrays[0];

        // Heuristic: if the baseline is relatively large compared to the whole type-set,
        // the simple full-scan with tag checks tends to be faster and allocate less
        // than building multiple HashSets. Use that path when it is likely cheaper.
        int typeLen = 0;
        if (_capabilitiesByType.TryGetValue(typeof(TCapability), out var allCaps))
        {
            typeLen = allCaps.Length;
        }
        else
        {
            typeLen = baseline.Length; // best effort
        }

        // If we would filter most of the type anyway, the scan is simpler and often faster.
        // Also, when there are only 2 tags and the baseline is not tiny, scanning is favorable.
        bool preferScan = false;
        if (arrays.Count >= 2)
        {
            var additionalTotal = 0;
            for (int i = 1; i < arrays.Count; i++) additionalTotal += arrays[i].Length;
            // Rough model: cost_index ≈ baselineLen + additional set-up; cost_scan ≈ typeLen * tagCount
            // Prefer scan when baseline is not small vs type, or when building sets is expensive.
            preferScan = baseline.Length > (typeLen >> 2) // baseline > 25% of type
                          || (arrays.Count == 2 && baseline.Length > 64)
                          || additionalTotal > baseline.Length * 2;
        }

        if (preferScan)
        {
            var list = new List<TCapability>();
            var needTags = new object[nonIndexedTags.Count + arrays.Count];
            // Combine all tags (indexed and non-indexed) so the check is uniform
            int p = 0;
            for (int i = 0; i < tags.Length; i++) needTags[p++] = tags[i];

            foreach (var cap in (TCapability[])_capabilitiesByType[typeof(TCapability)])
            {
                if (cap is ITaggedCapability<TSubject> taggedCap)
                {
                    bool ok = true;
                    for (int i = 0; i < needTags.Length; i++)
                    {
                        if (!taggedCap.Tags.Contains(needTags[i])) { ok = false; break; }
                    }
                    if (ok) list.Add(cap);
                }
            }
            return list.Count == 0 ? Array.Empty<TCapability>() : list.ToArray();
        }

        if (arrays.Count == 1)
        {
            if (nonIndexedTags.Count == 0)
                return baseline;
            // Need to filter baseline against non-indexed tags
            var filtered = new List<TCapability>(baseline.Length);
            foreach (var item in baseline)
            {
                if (item is ITaggedCapability<TSubject> taggedCap)
                {
                    bool ok = true;
                    for (int i = 0; i < nonIndexedTags.Count; i++)
                    {
                        if (!taggedCap.Tags.Contains(nonIndexedTags[i])) { ok = false; break; }
                    }
                    if (ok) filtered.Add(item);
                }
            }
            return filtered.Count == 0 ? Array.Empty<TCapability>() : filtered.ToArray();
        }

        // Build at most one HashSet (reference equality) for the largest remaining array for fast membership checks
        int largestIdx = 1;
        for (int i = 2; i < arrays.Count; i++) if (arrays[i].Length > arrays[largestIdx].Length) largestIdx = i;
        HashSet<TCapability>? fastSet = null;
        if (arrays[largestIdx].Length > 64)
        {
            fastSet = new HashSet<TCapability>(arrays[largestIdx], ReferenceEqualityComparer<TCapability>.Default);
        }

        // Filter baseline by membership in indexed sets and non-indexed tag checks
        var result = new List<TCapability>(baseline.Length);
        foreach (var item in baseline)
        {
            bool inAll = true;
            // Check membership against other indexed arrays
            for (int i = 1; i < arrays.Count && inAll; i++)
            {
                if (i == largestIdx && fastSet != null)
                {
                    if (!fastSet.Contains(item)) { inAll = false; break; }
                }
                else
                {
                    // For smaller arrays, linear scan is cheaper than building more sets
                    if (Array.IndexOf(arrays[i], item) < 0) { inAll = false; break; }
                }
            }
            if (inAll && nonIndexedTags.Count > 0 && item is ITaggedCapability<TSubject> taggedCap)
            {
                for (int i = 0; i < nonIndexedTags.Count; i++)
                {
                    if (!taggedCap.Tags.Contains(nonIndexedTags[i])) { inAll = false; break; }
                }
            }
            if (inAll)
                result.Add(item);
        }

        return result.ToArray();
    }

    // Reference equality comparer for capability instances
    private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Default = new();
        private ReferenceEqualityComparer() { }
        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<object> GetAllTags()
    {
        // Return cached, immutable set (allocation-free per call)
        return _allTags;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<TTag> GetAllTagsOfType<TTag>()
    {
        // Lazy cache per TTag to avoid repeated allocations
        if (_typedTagsCache.TryGetValue(typeof(TTag), out var cached))
        {
            return (IReadOnlyCollection<TTag>)cached;
        }

        var set = new HashSet<TTag>();
        foreach (var tag in _allTags)
        {
            if (tag is TTag tt)
                set.Add(tt);
        }

        var frozen = set.ToFrozenSet();
        _typedTagsCache[typeof(TTag)] = frozen;
        return frozen;
    }

    /// <inheritdoc />
    public bool Contains<TCapability>() 
        where TCapability : class, ICapability<TSubject>
    {
        return _capabilitiesByType.TryGetValue(typeof(TCapability), out var arr) && arr.Length > 0;
    }

    /// <inheritdoc />
    public int Count<TCapability>() 
        where TCapability : class, ICapability<TSubject>
    {
        return _capabilitiesByType.TryGetValue(typeof(TCapability), out var arr) ? arr.Length : 0;
    }

    private void BuildTagIndices(
        int minFrequency,
        out IReadOnlyDictionary<Type, FrozenDictionary<object, Array>> tagsIndex,
        out FrozenSet<object> allTags)
    {
        var perTypeIndex = new Dictionary<Type, FrozenDictionary<object, Array>>(_capabilitiesByType.Count);
        var all = new HashSet<object>();

        foreach (var (type, capabilities) in _capabilitiesByType)
        {
            // Build temporary map: tag -> list of capabilities (preserving existing order)
            var tmp = new Dictionary<object, List<object>>();

            foreach (var capability in capabilities)
            {
                if (capability is ITaggedCapability<TSubject> tagged)
                {
                    foreach (var tag in tagged.Tags)
                    {
                        all.Add(tag);
                        if (!tmp.TryGetValue(tag, out var list))
                        {
                            list = new List<object>();
                            tmp[tag] = list;
                        }
                        list.Add(capability);
                    }
                }
            }

            if (tmp.Count == 0)
            {
                // No tags for this type -> store empty index for quick negative lookups
                perTypeIndex[type] = FrozenDictionary<object, Array>.Empty;
                continue;
            }

            // Materialize typed arrays per tag and freeze
            var typedArrays = new Dictionary<object, Array>(tmp.Count);
            foreach (var (tag, list) in tmp)
            {
                // Only index tags that meet the frequency threshold to avoid
                // creating many singleton arrays for unique tags.
                if (list.Count >= minFrequency)
                {
                    var arr = Array.CreateInstance(type, list.Count);
                    for (int i = 0; i < list.Count; i++)
                    {
                        arr.SetValue(list[i], i);
                    }
                    typedArrays[tag] = arr;
                }
            }
            perTypeIndex[type] = typedArrays.ToFrozenDictionary();
        }

        tagsIndex = perTypeIndex;
        allTags = all.ToFrozenSet();
    }

    private FrozenSet<object> BuildAllTagsOnly()
    {
        var all = new HashSet<object>();
        foreach (var capabilities in _capabilitiesByType.Values)
        {
            foreach (var capability in capabilities)
            {
                if (capability is ITaggedCapability<TSubject> tagged)
                {
                    foreach (var tag in tagged.Tags)
                    {
                        all.Add(tag);
                    }
                }
            }
        }
        return all.ToFrozenSet();
    }
}