namespace Cocoar.Capabilities;

/// <summary>
/// Static helper class providing convenient entry points for creating capability bags.
/// </summary>
public static class Composer
{
    /// <summary>
    /// Creates a new capability bag builder for the specified subject.
    /// This is the primary entry point for building capability bags.
    /// </summary>
    /// <typeparam name="TSubject">The type of subject to build capabilities for</typeparam>
    /// <param name="subject">The subject instance</param>
    /// <returns>A new capability bag builder</returns>
    /// <exception cref="ArgumentNullException">Thrown when subject is null</exception>
    /// <example>
    /// <code>
    /// var config = new DatabaseConfig();
    /// var bag = Composer.For(config)
    ///     .Add(new ExposeAsCapability(typeof(IDbConfig)))
    ///     .Add(new SingletonLifetimeCapability())
    ///     .Build();
    /// </code>
    /// </example>
    public static CapabilityBagBuilder<TSubject> For<TSubject>(TSubject subject) 
        where TSubject : notnull
    {
        return new CapabilityBagBuilder<TSubject>(subject);
    }
}