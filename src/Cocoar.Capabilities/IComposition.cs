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

    bool Has<TCapability>() where TCapability : class;

    int Count<TCapability>() where TCapability : class;
}
