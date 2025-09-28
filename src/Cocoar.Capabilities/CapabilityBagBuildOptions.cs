namespace Cocoar.Capabilities;

/// <summary>
/// Options controlling how a <see cref="CapabilityBag{TSubject}"/> is built.
/// </summary>
public enum TagIndexingMode
{
    /// <summary>
    /// Do not build any tag indices. Tag queries will scan capabilities of the requested type.
    /// Recommended for very small bags (e.g., ~20 items) to minimize construction cost.
    /// </summary>
    None = 0,

    /// <summary>
    /// Build per-type indices from tag -> typed array for fast allocation-free queries.
    /// Recommended for read-many scenarios with medium/large bags.
    /// </summary>
    Eager = 1,

    /// <summary>
    /// Let the builder choose based on total capability count.
    /// Uses <see cref="CapabilityBagBuildOptions.AutoIndexThreshold"/> to decide.
    /// </summary>
    Auto = 2
}

/// <summary>
/// Build options for <see cref="CapabilityBag{TSubject}"/>.
/// </summary>
public readonly record struct CapabilityBagBuildOptions
{
    /// <summary>
    /// Whether and how to build tag indices.
    /// </summary>
    public TagIndexingMode TagIndexing { get; init; }

    /// <summary>
    /// Minimum number of capabilities that must share a tag to include that tag in the index.
    /// Defaults to 2 to avoid creating many singleton arrays.
    /// Only used when <see cref="TagIndexing"/> is <see cref="TagIndexingMode.Eager"/>.
    /// </summary>
    public int IndexMinFrequency { get; init; }

    /// <summary>
    /// Threshold for switching from no indexing to eager indexing when <see cref="TagIndexing"/> is <see cref="TagIndexingMode.Auto"/>.
    /// If total capability count is greater than or equal to this threshold, eager indexing is used; otherwise indexing is skipped.
    /// </summary>
    public int AutoIndexThreshold { get; init; }

    /// <summary>
    /// Default options tuned for read-many scenarios.
    /// </summary>
    public static CapabilityBagBuildOptions Default => new()
    {
        TagIndexing = TagIndexingMode.Auto,
        IndexMinFrequency = 2,
        AutoIndexThreshold = 64
    };

    /// <summary>
    /// Preset optimized for small, build-once bags where construction cost matters most.
    /// </summary>
    public static CapabilityBagBuildOptions SmallBag => new()
    {
        TagIndexing = TagIndexingMode.None,
        IndexMinFrequency = 2,
        AutoIndexThreshold = 64
    };
}
