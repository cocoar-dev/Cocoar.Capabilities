using Xunit;
using Cocoar.Capabilities;

namespace Cocoar.Capabilities.Tests;

/// <summary>
/// Integration spike demonstrating how Cocoar.Configuration would use the Capabilities System.
/// This is a conceptual demonstration based on the specification requirements.
/// 
/// IMPORTANT: This spike simulates the Cocoar.Configuration integration patterns
/// since the actual Cocoar.Configuration project is not available in this workspace.
/// </summary>
public class CocoarConfigurationIntegrationSpike
{
    // === SIMULATED COCOAR.CONFIGURATION TYPES ===
    // These would normally come from the actual Cocoar.Configuration project
    
    /// <summary>
    /// Simulated configuration rule - represents how Cocoar.Configuration 
    /// would define configuration transformations
    /// </summary>
    public class ConfigurationRule<T> where T : notnull
    {
        public string Name { get; set; } = string.Empty;
        public Func<T, T>? Transform { get; set; }
    }

    /// <summary>
    /// Simulated service collection interface - represents DI container integration
    /// </summary>
    public interface IServiceCollection
    {
        void AddSingleton<T>(T instance);
        void AddSingleton<TInterface>(object instance);
        void AddScoped<T>();
        void AddTransient<T>();
        void AddHealthCheck(string name);
    }

    /// <summary>
    /// Simulated DI service collection for testing
    /// </summary>
    public class MockServiceCollection : IServiceCollection
    {
        public List<string> Registrations { get; } = new();

        public void AddSingleton<T>(T instance)
            => Registrations.Add($"Singleton<{typeof(T).Name}>: {instance}");

        public void AddSingleton<TInterface>(object instance)
            => Registrations.Add($"Singleton<{typeof(TInterface).Name}, {instance?.GetType().Name}>: {instance}");

        public void AddScoped<T>()
            => Registrations.Add($"Scoped<{typeof(T).Name}>");

        public void AddTransient<T>()
            => Registrations.Add($"Transient<{typeof(T).Name}>");

        public void AddHealthCheck(string name)
            => Registrations.Add($"HealthCheck: {name}");
    }

    // === CONFIGURATION-SPECIFIC CAPABILITIES ===
    
    /// <summary>
    /// Capability indicating a configuration type should be exposed under a contract interface
    /// </summary>
    public record ExposeAsCapability<T>(Type ContractType) : ICapability<T> where T : notnull;

    /// <summary>
    /// Capability indicating singleton lifetime for DI registration
    /// </summary>
    public record SingletonLifetimeCapability<T> : ICapability<T> where T : notnull;

    /// <summary>
    /// Capability indicating scoped lifetime for DI registration  
    /// </summary>
    public record ScopedLifetimeCapability<T> : ICapability<T> where T : notnull;

    /// <summary>
    /// Capability indicating transient lifetime for DI registration
    /// </summary>
    public record TransientLifetimeCapability<T> : ICapability<T> where T : notnull;

    /// <summary>
    /// Capability indicating this configuration should have a health check
    /// </summary>
    public record HealthCheckCapability<T>(string Name) : ICapability<T> where T : notnull;

    /// <summary>
    /// Capability indicating this configuration should skip DI registration
    /// </summary>
    public record SkipRegistrationCapability<T> : ICapability<T> where T : notnull;

    // === TEST CONFIGURATION TYPES ===

    public interface IDbConfig
    {
        string ConnectionString { get; }
    }

    public class DatabaseConfig : IDbConfig
    {
        public string ConnectionString { get; set; } = "Data Source=localhost;Initial Catalog=TestDB;";
    }

    public class CacheConfig  
    {
        public string RedisConnectionString { get; set; } = "localhost:6379";
        public int DefaultTtlMinutes { get; set; } = 30;
    }

    // === INTEGRATION SPIKE TESTS ===

    [Fact]
    public void IntegrationSpike_BasicConfiguration_RegistrationWithCapabilities()
    {
        // ARRANGE: Create configuration with capabilities
        var dbConfig = new DatabaseConfig();
        var configBag = Composer.For(dbConfig)
            .Add(new ExposeAsCapability<DatabaseConfig>(typeof(IDbConfig)))
            .Add(new SingletonLifetimeCapability<DatabaseConfig>())
            .Add(new HealthCheckCapability<DatabaseConfig>("database"))
            .Build();

        var services = new MockServiceCollection();

        // ACT: Simulate how Cocoar.Configuration would process capabilities
        ProcessConfigurationCapabilities(configBag, services);

        // ASSERT: Verify correct DI registrations were planned
        Assert.Equal(3, services.Registrations.Count);
        Assert.Contains("Singleton<DatabaseConfig>", services.Registrations[0]);
        Assert.Contains("Singleton<Object, DatabaseConfig>", services.Registrations[1]);
        Assert.Contains("HealthCheck: database", services.Registrations[2]);
    }

    [Fact]
    public void IntegrationSpike_MultipleConfigurations_DifferentLifetimes()
    {
        // ARRANGE: Multiple configurations with different capabilities
        var dbConfig = new DatabaseConfig();
        var dbBag = Composer.For(dbConfig)
            .Add(new ExposeAsCapability<DatabaseConfig>(typeof(IDbConfig)))
            .Add(new SingletonLifetimeCapability<DatabaseConfig>())
            .Build();

        var cacheConfig = new CacheConfig();
        var cacheBag = Composer.For(cacheConfig)
            .Add(new ScopedLifetimeCapability<CacheConfig>())
            .Add(new SkipRegistrationCapability<CacheConfig>()) // This one skips registration
            .Build();

        var services = new MockServiceCollection();

        // ACT: Process both configurations
        ProcessConfigurationCapabilities(dbBag, services);
        ProcessConfigurationCapabilities(cacheBag, services);

        // ASSERT: DB was registered, cache was skipped
        Assert.Equal(2, services.Registrations.Count);
        Assert.Contains("Singleton<DatabaseConfig>", services.Registrations[0]);
        Assert.Contains("Singleton<Object, DatabaseConfig>", services.Registrations[1]);
        
        // Cache was not registered due to SkipRegistrationCapability
        Assert.DoesNotContain("CacheConfig", string.Join(", ", services.Registrations));
    }

    [Fact]
    public void IntegrationSpike_ComplexScenario_AllCapabilityTypes()
    {
        // ARRANGE: Complex configuration with multiple capabilities
        var dbConfig = new DatabaseConfig();
        var configBag = Composer.For(dbConfig)
            .Add(new ExposeAsCapability<DatabaseConfig>(typeof(IDbConfig)))
            .Add(new SingletonLifetimeCapability<DatabaseConfig>())
            .Add(new HealthCheckCapability<DatabaseConfig>("primary-database"))
            .Add(new HealthCheckCapability<DatabaseConfig>("database-performance")) // Multiple health checks
            .Build();

        var services = new MockServiceCollection();

        // ACT: Process configuration
        ProcessConfigurationCapabilities(configBag, services);

        // ASSERT: All capabilities were processed
        Assert.Equal(4, services.Registrations.Count);
        Assert.Contains("Singleton<DatabaseConfig>", services.Registrations[0]);
        Assert.Contains("Singleton<Object, DatabaseConfig>", services.Registrations[1]);
        Assert.Contains("HealthCheck: primary-database", services.Registrations[2]);
        Assert.Contains("HealthCheck: database-performance", services.Registrations[3]);
    }

    [Fact]
    public void IntegrationSpike_ConfigurationRules_WithCapabilities()
    {
        // ARRANGE: Simulate Cocoar.Configuration rules with capability bags
        var rule = new ConfigurationRule<DatabaseConfig>
        {
            Name = "DatabaseSetup",
            Transform = config => 
            {
                config.ConnectionString = "TransformedConnectionString";
                return config;
            }
        };

        var originalConfig = new DatabaseConfig();
        var transformedConfig = rule.Transform?.Invoke(originalConfig) ?? originalConfig;
        
        // Create capability bag for the transformed configuration
        var configBag = Composer.For(transformedConfig)
            .Add(new ExposeAsCapability<DatabaseConfig>(typeof(IDbConfig)))
            .Add(new SingletonLifetimeCapability<DatabaseConfig>())
            .Build();

        var services = new MockServiceCollection();

        // ACT: Process the rule result with capabilities
        ProcessConfigurationCapabilities(configBag, services);

        // ASSERT: Transformed configuration was registered correctly
        Assert.Equal("TransformedConnectionString", transformedConfig.ConnectionString);
        Assert.Equal(2, services.Registrations.Count);
        Assert.Contains("Singleton<DatabaseConfig>", services.Registrations[0]);
        Assert.Contains("Singleton<Object, DatabaseConfig>", services.Registrations[1]);
    }

    /// <summary>
    /// Simulates how Cocoar.Configuration would process capability bags
    /// This demonstrates the integration pattern that would be used in the real system
    /// </summary>
    private static void ProcessConfigurationCapabilities<T>(ICapabilityBag<T> configBag, IServiceCollection services)
        where T : notnull
    {
        // Skip registration if explicitly requested
        if (configBag.Contains<SkipRegistrationCapability<T>>())
            return;

        // Determine lifetime and register the main configuration type
        if (configBag.Contains<SingletonLifetimeCapability<T>>())
        {
            services.AddSingleton(configBag.Subject);
        }
        else if (configBag.Contains<ScopedLifetimeCapability<T>>())
        {
            services.AddScoped<T>();
        }
        else if (configBag.Contains<TransientLifetimeCapability<T>>())
        {
            services.AddTransient<T>();
        }

        // Register under contract interfaces
        foreach (var exposeAs in configBag.GetAll<ExposeAsCapability<T>>())
        {
            if (configBag.Contains<SingletonLifetimeCapability<T>>())
            {
                // Simulate registering the configuration instance under the contract type
                // In real code, this would use proper DI container registration
                services.AddSingleton<object>(configBag.Subject);
            }
        }

        // Setup health checks
        foreach (var healthCheck in configBag.GetAll<HealthCheckCapability<T>>())
        {
            services.AddHealthCheck(healthCheck.Name);
        }
    }
}