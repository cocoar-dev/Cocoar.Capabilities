namespace Cocoar.Capabilities;

/// <summary>
/// Represents a capability that can be categorized by multiple tags of any type.
/// Tags enable grouping, filtering, and identification of capabilities across libraries.
/// </summary>
/// <typeparam name="TSpec">The specification type this capability applies to.</typeparam>
public interface ITaggedCapability<TSpec> : ICapability<TSpec>
{
    /// <summary>
    /// Gets the collection of tags associated with this capability.
    /// Tags can be any object type: strings, enums, types, assemblies, etc.
    /// </summary>
    /// <example>
    /// <code>
    /// // Library identification + functional categorization
    /// Tags => [typeof(CocoarConfigurationDI), "DI", DIOperations.Registration];
    /// 
    /// // Assembly-based grouping
    /// Tags => [Assembly.GetExecutingAssembly(), "Validation", "Required"];
    /// 
    /// // Simple string categorization
    /// Tags => ["DI", "Registration"];
    /// </code>
    /// </example>
    IReadOnlyCollection<object> Tags { get; }
}

/// <summary>
/// Interface for capabilities that support both tagging and ordering.
/// Capabilities are first grouped by tag, then ordered within each group.
/// </summary>
/// <typeparam name="TSpec">The specification type this capability applies to.</typeparam>
public interface ITaggedOrderedCapability<TSpec> : ITaggedCapability<TSpec>, IOrderedCapability
{
}