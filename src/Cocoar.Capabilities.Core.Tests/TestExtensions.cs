namespace Cocoar.Capabilities.Core.Tests;

/// <summary>
/// Test-only extension methods to help with migration from removed API methods.
/// These are convenience methods for tests and should not be used in production code.
/// </summary>
public static class TestExtensions
{
    /// <summary>
    /// Test helper method to simulate the old TryGet behavior using GetAll for string subjects.
    /// Returns the first capability of the specified type if any exist.
    /// </summary>
    public static bool TryGet<TCapability>(this IComposition<string> bag, out TCapability capability)
        where TCapability : class, ICapability<string>
    {
        var capabilities = bag.GetAll<TCapability>();
        if (capabilities.Count > 0)
        {
            capability = capabilities[0];
            return true;
        }
        capability = null!;
        return false;
    }

    /// <summary>
    /// Test helper method to simulate the old TryGet behavior using GetAll for TestSubject.
    /// Returns the first capability of the specified type if any exist.
    /// </summary>
    public static bool TryGet<TCapability>(this IComposition<TestSubject> bag, out TCapability capability)
        where TCapability : class, ICapability<TestSubject>
    {
        var capabilities = bag.GetAll<TCapability>();
        if (capabilities.Count > 0)
        {
            capability = capabilities[0];
            return true;
        }
        capability = null!;
        return false;
    }

    /// <summary>
    /// Test helper method to simulate the old TryGet behavior using GetAll for MultiInterfaceRegistrationTests.TestSubject.
    /// Returns the first capability of the specified type if any exist.
    /// </summary>
    public static bool TryGet<TCapability>(this IComposition<MultiInterfaceRegistrationTests.TestSubject> bag, out TCapability capability)
        where TCapability : class, ICapability<MultiInterfaceRegistrationTests.TestSubject>
    {
        var capabilities = bag.GetAll<TCapability>();
        if (capabilities.Count > 0)
        {
            capability = capabilities[0];
            return true;
        }
        capability = null!;
        return false;
    }

    /// <summary>
    /// Test helper method to simulate the old TryGet behavior using GetAll for DatabaseConfig.
    /// Returns the first capability of the specified type if any exist.
    /// </summary>
    public static bool TryGet<TCapability>(this IComposition<DatabaseConfig> bag, out TCapability capability)
        where TCapability : class, ICapability<DatabaseConfig>
    {
        var capabilities = bag.GetAll<TCapability>();
        if (capabilities.Count > 0)
        {
            capability = capabilities[0];
            return true;
        }
        capability = null!;
        return false;
    }

    /// <summary>
    /// Test helper method to simulate the old GetRequired behavior using GetAll for TestSubject.
    /// Returns the first capability of the specified type or throws if none exist.
    /// </summary>
    public static TCapability GetRequired<TCapability>(this IComposition<TestSubject> bag)
        where TCapability : class, ICapability<TestSubject>
    {
        var capabilities = bag.GetAll<TCapability>();
        if (capabilities.Count > 0)
        {
            return capabilities[0];
        }

        // Create helpful error message with available capability types by checking known capability types
        var availableTypes = new List<string>();
        if (bag.GetAll<TestCapability>().Count > 0) availableTypes.Add("TestCapability");
        if (bag.GetAll<AnotherTestCapability>().Count > 0) availableTypes.Add("AnotherTestCapability");
        if (bag.GetAll<OrderedCapability>().Count > 0) availableTypes.Add("OrderedCapability");

        var availableTypesStr = availableTypes.Count > 0 
            ? $"[{string.Join(", ", availableTypes)}]"
            : "[none]";

        var message = $"Capability '{typeof(TCapability).Name}' not found for subject 'TestSubject'. " +
                     $"Available: {availableTypesStr}";

        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Test helper method to simulate the old GetRequired behavior using GetAll for DatabaseConfig.
    /// Returns the first capability of the specified type or throws if none exist.
    /// </summary>
    public static TCapability GetRequired<TCapability>(this IComposition<DatabaseConfig> bag)
        where TCapability : class, ICapability<DatabaseConfig>
    {
        var capabilities = bag.GetAll<TCapability>();
        if (capabilities.Count > 0)
        {
            return capabilities[0];
        }

        // Create helpful error message with available capability types 
        var availableTypes = new List<string>();
        if (bag.GetAll<SingletonLifetimeCapability>().Count > 0) availableTypes.Add("SingletonLifetimeCapability");
        if (bag.GetAll<HealthCheckCapability>().Count > 0) availableTypes.Add("HealthCheckCapability");
        if (bag.GetAll<ValidationCapability>().Count > 0) availableTypes.Add("ValidationCapability");

        var availableTypesStr = availableTypes.Count > 0 
            ? $"[{string.Join(", ", availableTypes)}]"
            : "[none]";

        var message = $"Capability '{typeof(TCapability).Name}' not found for subject 'DatabaseConfig'. " +
                     $"Available: {availableTypesStr}";

        throw new InvalidOperationException(message);
    }
}
