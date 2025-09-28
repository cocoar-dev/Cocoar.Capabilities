namespace Cocoar.Capabilities.Extensions;

/// <summary>
/// Extension methods providing convenient ways to work with capability bags.
/// </summary>
public static class CapabilityBagExtensions
{
    /// <summary>
    /// Executes an action if the specified capability exists in the bag.
    /// Provides a safe way to conditionally use capabilities without explicit null checks.
    /// </summary>
    /// <typeparam name="TSubject">The type of subject the bag contains capabilities for</typeparam>
    /// <typeparam name="TCapability">The type of capability to look for</typeparam>
    /// <param name="bag">The capability bag to search</param>
    /// <param name="action">The action to execute if the capability is found</param>
    /// <example>
    /// <code>
    /// bag.Use&lt;SingletonLifetimeCapability&gt;(capability => 
    /// {
    ///     services.AddSingleton(bag.Subject);
    /// });
    /// </code>
    /// </example>
    public static void Use<TSubject, TCapability>(
        this ICapabilityBag<TSubject> bag, 
        Action<TCapability> action)
        where TCapability : class, ICapability<TSubject>
    {
        ArgumentNullException.ThrowIfNull(bag);
        ArgumentNullException.ThrowIfNull(action);

        if (bag.TryGet<TCapability>(out var capability))
        {
            action(capability);
        }
    }

    /// <summary>
    /// Transforms a capability if it exists in the bag, returning the transformation result or a default value.
    /// Provides a safe way to extract values from capabilities without explicit null checks.
    /// </summary>
    /// <typeparam name="TSubject">The type of subject the bag contains capabilities for</typeparam>
    /// <typeparam name="TCapability">The type of capability to look for</typeparam>
    /// <typeparam name="TResult">The type of result to return</typeparam>
    /// <param name="bag">The capability bag to search</param>
    /// <param name="transformer">The function to transform the capability if found</param>
    /// <returns>The transformation result if the capability exists, otherwise the default value for TResult</returns>
    /// <example>
    /// <code>
    /// var contractType = bag.Transform&lt;ExposeAsCapability, Type&gt;(cap => cap.ContractType);
    /// if (contractType != null)
    /// {
    ///     services.AddSingleton(contractType, bag.Subject);
    /// }
    /// </code>
    /// </example>
    public static TResult? Transform<TSubject, TCapability, TResult>(
        this ICapabilityBag<TSubject> bag, 
        Func<TCapability, TResult> transformer)
        where TCapability : class, ICapability<TSubject>
    {
        ArgumentNullException.ThrowIfNull(bag);
        ArgumentNullException.ThrowIfNull(transformer);

        return bag.TryGet<TCapability>(out var capability) 
            ? transformer(capability) 
            : default;
    }

    /// <summary>
    /// Executes an action for each capability in the specified tag group, in order.
    /// This is perfect for processing capabilities that need to be handled sequentially.
    /// </summary>
    /// <example>
    /// <code>
    /// // Process all service lifetime capabilities in the order they were registered
    /// bag.ForEachInTag&lt;ILifetimeCapability&gt;("ServiceLifetime", capability =>
    /// {
    ///     services.Configure(capability);
    /// });
    /// </code>
    /// </example>
    public static void ForEachInTag<TSubject, TCapability>(
        this ICapabilityBag<TSubject> bag,
        object tag,
        Action<TCapability> action)
        where TCapability : class, ICapability<TSubject>
    {
        ArgumentNullException.ThrowIfNull(bag);
        ArgumentNullException.ThrowIfNull(action);
        
        var capabilities = bag.GetAllByTag<TCapability>(tag);
        foreach (var capability in capabilities)
        {
            action(capability);
        }
    }

    /// <summary>
    /// Gets all unique tags present in the capabilities of the specified type.
    /// Useful for discovering what tag groups are available for processing.
    /// </summary>
    /// <example>
    /// <code>
    /// // Discover all available service lifetime tags
    /// var tags = bag.GetAvailableTags&lt;IServiceCapability&gt;();
    /// foreach (var tag in tags)
    /// {
    ///     var capabilities = bag.GetAllByTag&lt;IServiceCapability&gt;(tag);
    ///     // Process capabilities by tag group
    /// }
    /// </code>
    /// </example>
    public static IReadOnlyList<object> GetAvailableTags<TSubject, TCapability>(
        this ICapabilityBag<TSubject> bag)
        where TCapability : class, ICapability<TSubject>
    {
        ArgumentNullException.ThrowIfNull(bag);
        
        var capabilities = bag.GetAll<TCapability>();
        var tags = new HashSet<object>();
        
        foreach (var capability in capabilities)
        {
            if (capability is ITaggedCapability<TSubject> tagged)
            {
                foreach (var tag in tagged.Tags)
                {
                    tags.Add(tag);
                }
            }
        }
            
        return tags.ToArray();
    }
}