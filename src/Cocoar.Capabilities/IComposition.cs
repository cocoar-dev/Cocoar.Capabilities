namespace Cocoar.Capabilities;

public interface IComposition
{
    object Subject { get; }
    int TotalCapabilityCount { get; }
    
    bool HasPrimary();
    
    bool HasPrimary<TPrimaryCapability>() where TPrimaryCapability : class, IPrimaryCapability;

    bool TryGetPrimary(out IPrimaryCapability primary);

    IPrimaryCapability? GetPrimaryOrDefault();

    IPrimaryCapability GetPrimary();

    bool TryGetPrimaryAs<TPrimaryCapability>(out TPrimaryCapability primary) where TPrimaryCapability : class, IPrimaryCapability;

    TPrimaryCapability? GetPrimaryOrDefaultAs<TPrimaryCapability>() where TPrimaryCapability : class, IPrimaryCapability;

    TPrimaryCapability GetRequiredPrimaryAs<TPrimaryCapability>() where TPrimaryCapability : class, IPrimaryCapability;

    IReadOnlyList<TCapability> GetAll<TCapability>() where TCapability : class;

    IReadOnlyList<object> GetAll();

    TCapability? GetFirstOrDefault<TCapability>() where TCapability : class;

    TCapability GetRequiredFirst<TCapability>() where TCapability : class;

    bool TryGetFirst<TCapability>(out TCapability capability) where TCapability : class;

    TCapability? GetLastOrDefault<TCapability>() where TCapability : class;

    TCapability GetRequiredLast<TCapability>() where TCapability : class;

    bool TryGetLast<TCapability>(out TCapability capability) where TCapability : class;

    bool Has<TCapability>() where TCapability : class;

    int Count<TCapability>() where TCapability : class;
}
