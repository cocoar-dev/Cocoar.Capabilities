namespace Cocoar.Capabilities;

/// <summary>
/// Base marker interface for all capabilities.
/// </summary>
public interface ICapability
{
}

/// <summary>
/// Type-safe capability interface that ensures capabilities are only applied to compatible subjects.
/// </summary>
/// <typeparam name="TSubject">The type of subject this capability can be applied to</typeparam>
public interface ICapability<in TSubject> : ICapability
{
}
