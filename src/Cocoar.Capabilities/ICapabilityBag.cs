namespace Cocoar.Capabilities;

/// <summary>
/// Immutable, thread-safe bag containing capabilities for a specific subject.
/// Provides type-safe retrieval of capabilities with exact-type matching semantics.
/// </summary>
/// <typeparam name="TSubject">The type of subject this bag contains capabilities for</typeparam>
public interface ICapabilityBag<TSubject>
{
    /// <summary>
    /// Gets the subject that this capability bag is associated with.
    /// </summary>
    TSubject Subject { get; }

    /// <summary>
    /// Attempts to retrieve a capability of the specified type.
    /// Uses exact-type matching - will not find base classes or interfaces.
    /// </summary>
    /// <typeparam name="TCapability">The exact type of capability to retrieve</typeparam>
    /// <param name="capability">The found capability, or null if not found</param>
    /// <returns>True if the capability was found, false otherwise</returns>
    bool TryGet<TCapability>(out TCapability capability) 
        where TCapability : class, ICapability<TSubject>;

    /// <summary>
    /// Retrieves a required capability of the specified type.
    /// Throws InvalidOperationException with helpful message if not found.
    /// </summary>
    /// <typeparam name="TCapability">The exact type of capability to retrieve</typeparam>
    /// <returns>The found capability</returns>
    /// <exception cref="InvalidOperationException">Thrown when the capability is not found</exception>
    TCapability GetRequired<TCapability>() 
        where TCapability : class, ICapability<TSubject>;

    /// <summary>
    /// Retrieves all capabilities of the specified type, ordered according to IOrderedCapability rules.
    /// Returns empty list if no capabilities of this type are found.
    /// </summary>
    /// <typeparam name="TCapability">The exact type of capabilities to retrieve</typeparam>
    /// <returns>Read-only list of all capabilities of the specified type</returns>
    IReadOnlyList<TCapability> GetAll<TCapability>() 
        where TCapability : class, ICapability<TSubject>;

    /// <summary>
    /// Retrieves all capabilities of the specified type that have the specified tag,
    /// ordered according to IOrderedCapability rules within the tag group.
    /// </summary>
    /// <typeparam name="TCapability">The type of capabilities to retrieve</typeparam>
    /// <param name="tag">The tag to filter by - can be any object (string, enum, type, assembly, etc.)</param>
    /// <returns>A read-only list of matching tagged capabilities in the correct order</returns>
    IReadOnlyList<TCapability> GetAllByTag<TCapability>(object tag)
        where TCapability : class, ICapability<TSubject>;

    /// <summary>
    /// Gets all capabilities of the specified type that have all of the specified tags.
    /// Capabilities are returned in stable sort order (Order property first, then insertion order).
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to retrieve</typeparam>
    /// <param name="tags">The tags to filter by - capabilities must have ALL of these tags</param>
    /// <returns>A read-only list of capabilities with all specified tags, in stable sort order</returns>
    IReadOnlyList<TCapability> GetAllByTags<TCapability>(params object[] tags)
        where TCapability : class, ICapability<TSubject>;
        
    /// <summary>
    /// Gets all unique tags used by capabilities in this bag.
    /// </summary>
    /// <returns>A read-only collection of all tags</returns>
    IReadOnlyCollection<object> GetAllTags();
        
    /// <summary>
    /// Gets all tags of a specific type used by capabilities in this bag.
    /// </summary>
    /// <typeparam name="TTag">The type of tags to retrieve</typeparam>
    /// <returns>A read-only collection of tags of the specified type</returns>
    IReadOnlyCollection<TTag> GetAllTagsOfType<TTag>();

    /// <summary>
    /// Checks if the bag contains any capabilities of the specified type.
    /// </summary>
    /// <typeparam name="TCapability">The exact type of capability to check for</typeparam>
    /// <returns>True if at least one capability of this type exists</returns>
    bool Contains<TCapability>() 
        where TCapability : class, ICapability<TSubject>;

    /// <summary>
    /// Gets the count of capabilities of the specified type.
    /// </summary>
    /// <typeparam name="TCapability">The exact type of capability to count</typeparam>
    /// <returns>The number of capabilities of the specified type</returns>
    int Count<TCapability>() 
        where TCapability : class, ICapability<TSubject>;

    /// <summary>
    /// Gets the total number of capabilities in this bag across all types.
    /// </summary>
    int TotalCapabilityCount { get; }
}