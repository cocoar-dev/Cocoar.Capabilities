namespace Cocoar.Capabilities;

/// <summary>
/// Builder for creating immutable capability bags using a fluent API.
/// This builder is single-use - after calling Build(), it becomes unusable.
/// Not thread-safe - should not be shared across threads during building.
/// </summary>
/// <typeparam name="TSubject">The type of subject this builder creates capabilities for</typeparam>
public sealed class CapabilityBagBuilder<TSubject> where TSubject : notnull
{
    private readonly TSubject _subject;
    private readonly Dictionary<Type, List<ICapability<TSubject>>> _capabilitiesByType = new();
    private bool _built;

    /// <summary>
    /// Initializes a new capability bag builder for the specified subject.
    /// </summary>
    /// <param name="subject">The subject to build capabilities for</param>
    /// <exception cref="ArgumentNullException">Thrown when subject is null</exception>
    public CapabilityBagBuilder(TSubject subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        _subject = subject;
    }

    /// <summary>
    /// Gets the subject this builder is creating capabilities for.
    /// </summary>
    public TSubject Subject => _subject;

    /// <summary>
    /// Adds a capability to the bag being built.
    /// The capability will be registered under its concrete type for exact-type matching.
    /// </summary>
    /// <typeparam name="TCapability">The concrete type of the capability</typeparam>
    /// <param name="capability">The capability instance to add</param>
    /// <returns>This builder for method chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when Build() has already been called</exception>
    /// <exception cref="ArgumentNullException">Thrown when capability is null</exception>
    public CapabilityBagBuilder<TSubject> Add<TCapability>(TCapability capability)
        where TCapability : ICapability<TSubject>
    {
        if (_built)
        {
            throw new InvalidOperationException("Build() has already been called. This builder is no longer usable.");
        }
        
        if (capability is null)
        {
            throw new ArgumentNullException(nameof(capability));
        }

        var type = typeof(TCapability);
        if (!_capabilitiesByType.TryGetValue(type, out var list))
        {
            list = new List<ICapability<TSubject>>();
            _capabilitiesByType[type] = list;
        }

        list.Add(capability);
        return this;
    }

    /// <summary>
    /// Adds a capability and registers it under a specific contract type.
    /// This allows retrieval by interface or base class types.
    /// Use this when you plan to retrieve capabilities by their contract types rather than concrete types.
    /// </summary>
    /// <typeparam name="TContract">The contract type to register the capability under</typeparam>
    /// <param name="capability">The capability instance to add</param>
    /// <returns>This builder for method chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when Build() has already been called</exception>
    /// <exception cref="ArgumentNullException">Thrown when capability is null</exception>
    public CapabilityBagBuilder<TSubject> AddAs<TContract>(ICapability<TSubject> capability)
        where TContract : class, ICapability<TSubject>
    {
        if (_built)
        {
            throw new InvalidOperationException("Build() has already been called. This builder is no longer usable.");
        }
        
        ArgumentNullException.ThrowIfNull(capability);

        var type = typeof(TContract);
        if (!_capabilitiesByType.TryGetValue(type, out var list))
        {
            list = new List<ICapability<TSubject>>();
            _capabilitiesByType[type] = list;
        }

        list.Add(capability);
        return this;
    }

    /// <summary>
    /// Builds an immutable capability bag from the added capabilities.
    /// This method can only be called once - subsequent calls will throw an exception.
    /// After calling this method, the builder becomes unusable.
    /// </summary>
    /// <returns>An immutable capability bag containing all added capabilities</returns>
    /// <exception cref="InvalidOperationException">Thrown when Build() has already been called</exception>
    public ICapabilityBag<TSubject> Build()
        => Build(CapabilityBagBuildOptions.Default);

    /// <summary>
    /// Builds an immutable capability bag from the added capabilities using the provided options.
    /// </summary>
    /// <param name="options">Build-time options (indexing behavior, thresholds)</param>
    /// <returns>An immutable capability bag</returns>
    /// <exception cref="InvalidOperationException">Thrown when Build() has already been called</exception>
    public ICapabilityBag<TSubject> Build(CapabilityBagBuildOptions options = default)
    {
        if (_built)
        {
            throw new InvalidOperationException("Build() can only be called once. This builder is no longer usable.");
        }

        _built = true;

        // Build arrays typed to concrete capability types for type safety
        var result = new Dictionary<Type, Array>(_capabilitiesByType.Count);
        int totalCount = 0;

        foreach (var (type, list) in _capabilitiesByType)
        {
            var ordered = SortCapabilities(list);
            
            // Create array typed exactly to the capability type (e.g., ExposeAsCapability[])
            // This ensures type-safe casting in the bag implementation
            var arr = Array.CreateInstance(type, ordered.Count);
            for (int i = 0; i < ordered.Count; i++)
            {
                arr.SetValue(ordered[i], i);
            }

            result[type] = arr;
            totalCount += arr.Length;
        }

        return new CapabilityBag<TSubject>(_subject, result, totalCount, options);
    }

    /// <summary>
    /// Sorts capabilities according to the ordering rules:
    /// 1. Lower Order value runs first (0, 10, 100...)
    /// 2. Non-IOrderedCapability treated as Order = 0
    /// 3. Stable tie-breaker: insertion order
    /// </summary>
    private static List<ICapability<TSubject>> SortCapabilities(List<ICapability<TSubject>> capabilities)
    {
        return capabilities
            .Select((capability, index) => new { capability, index })
            .OrderBy(x => (x.capability as IOrderedCapability)?.Order ?? 0)  // Order first
            .ThenBy(x => x.index)  // Insertion order for ties (stable sort)
            .Select(x => x.capability)
            .ToList();
    }
}