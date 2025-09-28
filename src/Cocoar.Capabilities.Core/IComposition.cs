namespace Cocoar.Capabilities.Core;

public interface IComposition
{
    object Subject { get; }
    int TotalCapabilityCount { get; }
}

public interface IComposition<TSubject> : IComposition
{
    new TSubject Subject { get; }

    bool HasPrimary();
    
    bool HasPrimary<TPrimaryCapability>() where TPrimaryCapability : class, IPrimaryCapability<TSubject>;

    bool TryGetPrimary(out IPrimaryCapability<TSubject> primary);

    IPrimaryCapability<TSubject>? GetPrimaryOrDefault();

    IPrimaryCapability<TSubject> GetPrimary();

    bool TryGetPrimaryAs<TPrimaryCapability>(out TPrimaryCapability primary) where TPrimaryCapability : class, IPrimaryCapability<TSubject>;

    TPrimaryCapability? GetPrimaryOrDefaultAs<TPrimaryCapability>() where TPrimaryCapability : class, IPrimaryCapability<TSubject>;

    TPrimaryCapability GetRequiredPrimaryAs<TPrimaryCapability>() where TPrimaryCapability : class, IPrimaryCapability<TSubject>;

    IReadOnlyList<TCapability> GetAll<TCapability>() where TCapability : class, ICapability<TSubject>;

    IReadOnlyList<ICapability<TSubject>> GetAll();

    bool Has<TCapability>() where TCapability : class, ICapability<TSubject>;

    int Count<TCapability>() where TCapability : class, ICapability<TSubject>;
}
