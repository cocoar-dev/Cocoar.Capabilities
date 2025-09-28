namespace Cocoar.Capabilities.Core.Tests;

// Example capabilities that demonstrate real-world usage patterns
public record ExposeAsCapability(Type ContractType) : ICapability<DatabaseConfig>;
public record SingletonLifetimeCapability : ICapability<DatabaseConfig>;
public record ScopedLifetimeCapability : ICapability<DatabaseConfig>;
public record HealthCheckCapability(string Name) : ICapability<DatabaseConfig>;
public record ValidationCapability(string Pattern) : ICapability<DatabaseConfig>, IOrderedCapability
{
    public int Order => -100; // Validation should run first
}

// Example configuration interface
public interface IDbConfig
{
    string ConnectionString { get; }
}

// Example configuration class (using the existing DatabaseConfig from UnitTest1.cs)

public class ExampleUsageTests
{
    [Fact]
    public void CocoarConfiguration_StyleUsage_WorksAsExpected()
    {
        
        // in the context of Cocoar.Configuration integration
        
        var dbConfig = new DatabaseConfig 
        { 
            ConnectionString = "Server=prod;Database=MyApp;Trusted_Connection=true;" 
        };

        // Build capability bag with various configuration capabilities
        var bag = Composer.For(dbConfig)
            .Add(new ExposeAsCapability(typeof(IDbConfig)))
            .Add(new SingletonLifetimeCapability())
            .Add(new HealthCheckCapability("database"))
            .Add(new ValidationCapability(@"Server=.*;Database=.*;"))
            .Build();

        

        // 1. Validation runs first (Order = -100)
        string? validationPattern = null;
        var validation = bag.GetAll<ValidationCapability>().FirstOrDefault();
        if (validation != null)
        {
            validationPattern = validation.Pattern;
            // In real usage: validate the connection string against pattern
        }
        Assert.Equal(@"Server=.*;Database=.*;", validationPattern);

        // 2. Register the service based on lifetime capability
        var registrationType = "";
        var singletonLifetime = bag.GetAll<SingletonLifetimeCapability>().FirstOrDefault();
        if (singletonLifetime != null)
        {
            registrationType = "Singleton";
            // In real usage: services.AddSingleton<DatabaseConfig>(dbConfig)
        }

        var scopedLifetime = bag.GetAll<ScopedLifetimeCapability>().FirstOrDefault();
        if (scopedLifetime != null)
        {
            registrationType = "Scoped";
            // In real usage: services.AddScoped<DatabaseConfig>(dbConfig)
        }

        // 3. Expose under contract interfaces
        var contractType = bag.GetAll<ExposeAsCapability>().FirstOrDefault()?.ContractType;
        Assert.Equal(typeof(IDbConfig), contractType);
        // In real usage: services.AddSingleton<IDbConfig>(provider => dbConfig)

        // 4. Setup health checks
        var healthCheckName = bag.GetAll<HealthCheckCapability>().FirstOrDefault()?.Name;
        Assert.Equal("database", healthCheckName);
        // In real usage: services.AddHealthChecks().AddDbContextCheck<DbContext>(healthCheckName)

        Assert.Equal("Singleton", registrationType);
        Assert.Equal(4, bag.TotalCapabilityCount);
    }

    [Fact]
    public void CrossProject_Extensibility_DifferentAssembliesAddCapabilities()
    {
        // This demonstrates the key value proposition: different projects can add capabilities
        // to the same subject type without circular dependencies
        
        // Core project defines the subject
        var dbConfig = new DatabaseConfig();
        
        var bag = Composer.For(dbConfig)
            // DI project adds lifetime management
            .Add(new SingletonLifetimeCapability())
            
            // Configuration project adds contract exposure
            .Add(new ExposeAsCapability(typeof(IDbConfig)))
            
            // AspNetCore project adds health checks
            .Add(new HealthCheckCapability("database"))
            
            // Validation project adds early validation
            .Add(new ValidationCapability(".*"))
            
            .Build();

        // Each project can independently check for and use its capabilities
        Assert.True(bag.Has<SingletonLifetimeCapability>());
        Assert.True(bag.Has<ExposeAsCapability>());
        Assert.True(bag.Has<HealthCheckCapability>());
        Assert.True(bag.Has<ValidationCapability>());
        
        // Verify ordered processing works correctly
        // The ValidationCapability should be first due to its Order = -100
        var validationCaps = bag.GetAll<ValidationCapability>();
        Assert.Single(validationCaps);
        Assert.Equal(-100, validationCaps[0].Order);
    }

    [Fact]
    public void FluentAPI_BuildsComplexConfigurations_WithMultipleCapabilities()
    {
        // Demonstrate the fluent API building complex, real-world configurations
        
        var config = new DatabaseConfig { ConnectionString = "complex-connection-string" };
        
        var bag = Composer.For(config)
            // Multiple capabilities of the same type
            .Add(new ExposeAsCapability(typeof(IDbConfig)))
            .Add(new ExposeAsCapability(typeof(DatabaseConfig)))
            
            // Mix of ordered and unordered capabilities
            .Add(new ValidationCapability("validation-pattern"))
            .Add(new SingletonLifetimeCapability())
            .Add(new HealthCheckCapability("primary-db"))
            
            .Build();

        var exposeCapabilities = bag.GetAll<ExposeAsCapability>();
        Assert.Equal(2, exposeCapabilities.Count);
        
        var contractTypes = exposeCapabilities.Select(c => c.ContractType).ToList();
        Assert.Contains(typeof(IDbConfig), contractTypes);
        Assert.Contains(typeof(DatabaseConfig), contractTypes);
        
        Assert.Equal(5, bag.TotalCapabilityCount);
        Assert.True(bag.Has<ValidationCapability>());
        Assert.True(bag.Has<SingletonLifetimeCapability>());
        Assert.True(bag.Has<HealthCheckCapability>());
    }

    [Fact]
    public void ErrorHandling_ProvidesHelpfulDiagnostics_ForMissingCapabilities()
    {
        // Demonstrate the helpful error messages when capabilities are missing
        // This helps developers understand what went wrong
        
        var config = new DatabaseConfig();
        var bag = Composer.For(config)
            .Add(new SingletonLifetimeCapability())
            .Add(new HealthCheckCapability("test"))
            .Build();

        // Try to get a missing capability
        var ex = Assert.Throws<InvalidOperationException>(() => 
        {
            bag.GetRequired<ExposeAsCapability>();
        });

        // Verify the error message is helpful and actionable
        Assert.Contains("ExposeAsCapability", ex.Message);
        Assert.Contains("DatabaseConfig", ex.Message);
        Assert.Contains("SingletonLifetimeCapability", ex.Message);
        Assert.Contains("HealthCheckCapability", ex.Message);
        
        // The developer can see exactly what capabilities are available
        // and realize they forgot to add the ExposeAsCapability
    }

    [Fact] 
    public void RealWorld_CocoarConfiguration_IntegrationExample()
    {
        // This shows how the system would actually be used in Cocoar.Configuration
        
        // Multiple configuration types with different capabilities
        var dbConfig = new DatabaseConfig { ConnectionString = "db-connection" };
        var cacheConfig = new TestSubject { Name = "Redis" }; // Reusing test type as cache config
        
        // Each gets its own capability bag
        var dbBag = Composer.For(dbConfig)
            .Add(new ExposeAsCapability(typeof(IDbConfig)))
            .Add(new SingletonLifetimeCapability())
            .Add(new HealthCheckCapability("database"))
            .Build();
            
        var cacheBag = Composer.For(cacheConfig)
            .Add(new TestCapability("cache-settings"))
            .Build();

        // Process each configuration's capabilities independently
        var dbServices = new List<string>();
        var cacheServices = new List<string>();

        // Database configuration processing
        var singletonCapability = dbBag.GetAll<SingletonLifetimeCapability>().FirstOrDefault();
        if (singletonCapability != null)
            dbServices.Add("DatabaseConfig as Singleton");
            
        var exposeCapability = dbBag.GetAll<ExposeAsCapability>().FirstOrDefault();
        if (exposeCapability != null)
            dbServices.Add($"Exposed as {exposeCapability.ContractType.Name}");
            
        var healthCapability = dbBag.GetAll<HealthCheckCapability>().FirstOrDefault();
        if (healthCapability != null)
            dbServices.Add($"Health check: {healthCapability.Name}");

        // Cache configuration processing  
        var testCapability = cacheBag.GetAll<TestCapability>().FirstOrDefault();
        if (testCapability != null)
            cacheServices.Add($"Cache: {testCapability.Value}");

        // Verify both configurations were processed correctly
        Assert.Equal(3, dbServices.Count);
        Assert.Single(cacheServices);
        
        Assert.Contains("DatabaseConfig as Singleton", dbServices);
        Assert.Contains("Exposed as IDbConfig", dbServices);
        Assert.Contains("Health check: database", dbServices);
        Assert.Contains("Cache: cache-settings", cacheServices);
    }

    [Fact]
    public void AddAs_ContractRegistration_EnablesInterfaceRetrieval()
    {
        // Demonstrates the AddAs<T>() feature for interface-based retrieval
        
        var config = new DatabaseConfig();
        var concreteCapability = new ValidationCapability("test-pattern");
        
        // Register capability under a base capability interface that matches DatabaseConfig
        var bag = Composer.For(config)
            .AddAs<ICapability<DatabaseConfig>>(concreteCapability)
            .Build();

        // Can retrieve by base interface
        var baseCapability = bag.GetRequired<ICapability<DatabaseConfig>>();
        Assert.Equal(concreteCapability, baseCapability);
        
        // And it's also an ordered capability
        var orderedCapability = (IOrderedCapability)baseCapability;
        Assert.Equal(-100, orderedCapability.Order);
        
        // But NOT by concrete type (exact-type matching)
        Assert.False(bag.TryGet<ValidationCapability>(out _));
        
        // This prevents the common mistake of adding concrete but trying to retrieve by interface
    }

    [Fact]
    public void GetAll_WithoutGenericArgument_ReturnsAllCapabilities()
    {
        
        var config = new DatabaseConfig();
        var bag = Composer.For(config)
            .Add(new SingletonLifetimeCapability())
            .Add(new HealthCheckCapability("Database"))
            .Add(new ValidationCapability(".*"))
            .Build();

        
        var allCapabilities = bag.GetAll();

        
        Assert.Equal(3, allCapabilities.Count);
        Assert.Contains(allCapabilities, c => c is SingletonLifetimeCapability);
        Assert.Contains(allCapabilities, c => c is HealthCheckCapability);
        Assert.Contains(allCapabilities, c => c is ValidationCapability);
    }

    [Fact]
    public void GetAll_WithoutGenericArgument_ReturnsEmptyListWhenNoCapabilities()
    {
        
        var config = new DatabaseConfig();
        var bag = Composer.For(config)
            .Build();

        
        var allCapabilities = bag.GetAll();

        
        Assert.Empty(allCapabilities);
    }

    [Fact]
    public void HasPrimary_Generic_ChecksForSpecificPrimaryType()
    {
        
        var config = new DatabaseConfig();
        var primaryCapability = new TestPrimaryCapability("Primary DB config");
        var bag = Composer.For(config)
            .Add(new SingletonLifetimeCapability())
            .Add(primaryCapability)
            .Build();

        
        Assert.True(bag.HasPrimary<TestPrimaryCapability>());
        Assert.True(bag.HasPrimary<IPrimaryCapability<DatabaseConfig>>());
        Assert.False(bag.HasPrimary<AnotherPrimaryCapability>());
    }

    [Fact]
    public void HasPrimary_Generic_ReturnsFalseWhenNoPrimaryExists()
    {
        
        var config = new DatabaseConfig();
        var bag = Composer.For(config)
            .Add(new SingletonLifetimeCapability())
            .Build();

        
        Assert.False(bag.HasPrimary<TestPrimaryCapability>());
        Assert.False(bag.HasPrimary<IPrimaryCapability<DatabaseConfig>>());
    }

    [Fact]
    public void HasPrimary_Generic_ReturnsFalseForWrongPrimaryType()
    {
        
        var config = new DatabaseConfig();
        var primaryCapability = new TestPrimaryCapability("Primary DB config");
        var bag = Composer.For(config)
            .Add(primaryCapability)
            .Build();

        
        Assert.True(bag.HasPrimary<TestPrimaryCapability>());
        Assert.False(bag.HasPrimary<AnotherPrimaryCapability>());
    }

    [Fact]
    public void GetAll_WithoutGenericArgument_AppliesOrdering()
    {
        
        var config = new DatabaseConfig();
        var validation = new ValidationCapability(".*");
        var singleton = new SingletonLifetimeCapability();
        var health = new HealthCheckCapability("Database");
        
        var bag = Composer.For(config)
            .Add(singleton)
            .Add(health)
            .Add(validation)
            .Build();

        
        var allCapabilities = bag.GetAll();

        
        Assert.Equal(3, allCapabilities.Count);
        Assert.IsType<ValidationCapability>(allCapabilities[0]);
        // The other two have Order = 0, so their relative order is stable (insertion order)
        Assert.Contains(allCapabilities.Skip(1), c => c is SingletonLifetimeCapability);
        Assert.Contains(allCapabilities.Skip(1), c => c is HealthCheckCapability);
    }
}

// Test primary capabilities for the new API tests
public class TestPrimaryCapability : IPrimaryCapability<DatabaseConfig>
{
    public TestPrimaryCapability(string description) => Description = description;
    public string Description { get; }
    public override string ToString() => Description;
}

public class AnotherPrimaryCapability : IPrimaryCapability<DatabaseConfig>
{
    public AnotherPrimaryCapability(string description) => Description = description;
    public string Description { get; }
    public override string ToString() => Description;
}
