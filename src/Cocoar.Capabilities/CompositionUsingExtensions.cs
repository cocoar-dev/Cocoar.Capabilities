namespace Cocoar.Capabilities;

/// <summary>
/// Fluent extension methods for inline capability usage on <see cref="IComposition"/>.
/// These are convenience wrappers over the Get* methods that enable chainable, expressive usage patterns.
/// </summary>
public static class CompositionUsingExtensions
{
    // ============= FIRST =============
    
    /// <summary>
    /// Uses the first capability of type <typeparamref name="T"/>.
    /// Throws if no instances exist. Returns composition for chaining.
    /// </summary>
    /// <typeparam name="T">The capability type to use.</typeparam>
    /// <param name="composition">The composition to retrieve the capability from.</param>
    /// <param name="use">The action to execute with the capability.</param>
    /// <returns>The same composition instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">If no capability of type <typeparamref name="T"/> exists.</exception>
    public static IComposition UsingFirst<T>(this IComposition composition, Action<T> use)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(use);
        
        use(composition.GetRequiredFirst<T>());
        return composition;
    }

    /// <summary>
    /// Uses the first capability of type <typeparamref name="T"/> and returns a result.
    /// Throws if no instances exist.
    /// </summary>
    /// <typeparam name="T">The capability type to use.</typeparam>
    /// <typeparam name="TResult">The type of result to return.</typeparam>
    /// <param name="composition">The composition to retrieve the capability from.</param>
    /// <param name="use">The function to execute with the capability.</param>
    /// <returns>The result of the function.</returns>
    /// <exception cref="InvalidOperationException">If no capability of type <typeparamref name="T"/> exists.</exception>
    public static TResult UsingFirst<T, TResult>(this IComposition composition, Func<T, TResult> use)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(use);
        
        return use(composition.GetRequiredFirst<T>());
    }

    /// <summary>
    /// Uses the first capability of type <typeparamref name="T"/> if it exists.
    /// Silently skips if no instances exist. Returns composition for chaining.
    /// </summary>
    /// <typeparam name="T">The capability type to use.</typeparam>
    /// <param name="composition">The composition to retrieve the capability from.</param>
    /// <param name="use">The action to execute with the capability.</param>
    /// <returns>The same composition instance for fluent chaining.</returns>
    public static IComposition UsingFirstOrDefault<T>(this IComposition composition, Action<T> use)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(use);
        
        var cap = composition.GetFirstOrDefault<T>();
        if (cap != null)
        {
            use(cap);
        }
        return composition;
    }

    // ============= LAST =============
    
    /// <summary>
    /// Uses the last capability of type <typeparamref name="T"/>.
    /// Throws if no instances exist. Returns composition for chaining.
    /// </summary>
    /// <typeparam name="T">The capability type to use.</typeparam>
    /// <param name="composition">The composition to retrieve the capability from.</param>
    /// <param name="use">The action to execute with the capability.</param>
    /// <returns>The same composition instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">If no capability of type <typeparamref name="T"/> exists.</exception>
    public static IComposition UsingLast<T>(this IComposition composition, Action<T> use)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(use);
        
        use(composition.GetRequiredLast<T>());
        return composition;
    }

    /// <summary>
    /// Uses the last capability of type <typeparamref name="T"/> and returns a result.
    /// Throws if no instances exist.
    /// </summary>
    /// <typeparam name="T">The capability type to use.</typeparam>
    /// <typeparam name="TResult">The type of result to return.</typeparam>
    /// <param name="composition">The composition to retrieve the capability from.</param>
    /// <param name="use">The function to execute with the capability.</param>
    /// <returns>The result of the function.</returns>
    /// <exception cref="InvalidOperationException">If no capability of type <typeparamref name="T"/> exists.</exception>
    public static TResult UsingLast<T, TResult>(this IComposition composition, Func<T, TResult> use)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(use);
        
        return use(composition.GetRequiredLast<T>());
    }

    /// <summary>
    /// Uses the last capability of type <typeparamref name="T"/> if it exists.
    /// Silently skips if no instances exist. Returns composition for chaining.
    /// </summary>
    /// <typeparam name="T">The capability type to use.</typeparam>
    /// <param name="composition">The composition to retrieve the capability from.</param>
    /// <param name="use">The action to execute with the capability.</param>
    /// <returns>The same composition instance for fluent chaining.</returns>
    public static IComposition UsingLastOrDefault<T>(this IComposition composition, Action<T> use)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(use);
        
        var cap = composition.GetLastOrDefault<T>();
        if (cap != null)
        {
            use(cap);
        }
        return composition;
    }

    // ============= EACH =============
    
    /// <summary>
    /// Executes <paramref name="use"/> for each capability of type <typeparamref name="T"/>.
    /// Silent if no instances exist (iterates empty collection). Returns composition for chaining.
    /// </summary>
    /// <typeparam name="T">The capability type to use.</typeparam>
    /// <param name="composition">The composition to retrieve capabilities from.</param>
    /// <param name="use">The action to execute for each capability.</param>
    /// <returns>The same composition instance for fluent chaining.</returns>
    public static IComposition UsingEach<T>(this IComposition composition, Action<T> use)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(use);
        
        foreach (var cap in composition.GetAll<T>())
        {
            use(cap);
        }
        return composition;
    }

    /// <summary>
    /// Executes <paramref name="use"/> for each capability of type <typeparamref name="T"/> and collects results.
    /// Returns a list of results (empty if no instances exist).
    /// </summary>
    /// <typeparam name="T">The capability type to use.</typeparam>
    /// <typeparam name="TResult">The type of result to collect.</typeparam>
    /// <param name="composition">The composition to retrieve capabilities from.</param>
    /// <param name="use">The function to execute for each capability.</param>
    /// <returns>A read-only list of results from executing the function on each capability.</returns>
    public static IReadOnlyList<TResult> UsingEach<T, TResult>(this IComposition composition, Func<T, TResult> use)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(use);
        
        var all = composition.GetAll<T>();
        var results = new List<TResult>(all.Count);
        foreach (var cap in all)
        {
            results.Add(use(cap));
        }
        return results;
    }

    // ============= ALL =============
    
    /// <summary>
    /// Executes <paramref name="use"/> with the full collection of <typeparamref name="T"/> capabilities.
    /// Use for aggregations or collection-level operations. Returns composition for chaining.
    /// </summary>
    /// <typeparam name="T">The capability type to use.</typeparam>
    /// <param name="composition">The composition to retrieve capabilities from.</param>
    /// <param name="use">The action to execute with the full collection of capabilities.</param>
    /// <returns>The same composition instance for fluent chaining.</returns>
    public static IComposition UsingAll<T>(this IComposition composition, Action<IReadOnlyList<T>> use)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(use);
        
        use(composition.GetAll<T>());
        return composition;
    }

    /// <summary>
    /// Executes <paramref name="use"/> with the full collection of <typeparamref name="T"/> capabilities and returns a result.
    /// Use for aggregations or collection-level operations.
    /// </summary>
    /// <typeparam name="T">The capability type to use.</typeparam>
    /// <typeparam name="TResult">The type of result to return.</typeparam>
    /// <param name="composition">The composition to retrieve capabilities from.</param>
    /// <param name="use">The function to execute with the full collection of capabilities.</param>
    /// <returns>The result of the function.</returns>
    public static TResult UsingAll<T, TResult>(this IComposition composition, Func<IReadOnlyList<T>, TResult> use)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(use);
        
        return use(composition.GetAll<T>());
    }
}
