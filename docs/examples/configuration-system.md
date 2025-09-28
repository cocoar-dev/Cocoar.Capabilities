# Configuration System Example

A comprehensive example demonstrating how to build a modern configuration system using Cocoar.Capabilities patterns.

## Overview

This example shows how to create a flexible, extensible configuration system that supports:
- Multiple configuration sources (files, environment, cloud)
- Environment-specific behaviors
- Feature flags and settings
- Type-safe configuration access
- Real-time configuration updates

## Core Configuration Model

```csharp
using Cocoar.Capabilities;

// Unified configuration key for cross-project extensibility
public readonly record struct ConfigurationKey(string Section, string Key)
{
    public override string ToString() => $"{Section}:{Key}";
}

// Configuration value wrapper
public readonly record struct ConfigurationValue(object Value, Type ValueType)
{
    public T GetValue<T>() => (T)Value;
    public bool TryGetValue<T>(out T value)
    {
        if (ValueType == typeof(T) && Value is T typedValue)
        {
            value = typedValue;
            return true;
        }
        value = default(T);
        return false;
    }
}
```

## Primary Capability Strategies

```csharp
// Environment-based primary capabilities
public record DevelopmentConfigPrimary<T> : IPrimaryCapability<T>;
public record StagingConfigPrimary<T> : IPrimaryCapability<T>;
public record ProductionConfigPrimary<T> : IPrimaryCapability<T>;

// Source-based primary capabilities  
public record FileConfigPrimary<T>(string FilePath) : IPrimaryCapability<T>;
public record EnvironmentConfigPrimary<T> : IPrimaryCapability<T>;
public record CloudConfigPrimary<T>(string Endpoint) : IPrimaryCapability<T>;
public record DatabaseConfigPrimary<T>(string ConnectionString) : IPrimaryCapability<T>;
```

## Configuration Capabilities

```csharp
// Core configuration capabilities
public record ConfigurationValueCapability<T>(ConfigurationValue Value) : ICapability<T>;

public record DefaultValueCapability<T>(object DefaultValue) : ICapability<T>;

public record ValidationCapability<T>(Func<object, bool> Validator, string ErrorMessage) : ICapability<T>;

public record TypeMappingCapability<T>(Type TargetType, Func<object, object> Converter) : ICapability<T>;

// Feature flag capability
public record FeatureFlagCapability<T>(bool IsEnabled, DateTime? ExpiresAt = null) : ICapability<T>;

// Monitoring and observability
public record ConfigurationMonitoringCapability<T>(
    DateTime LastAccessed,
    DateTime LastModified,
    int AccessCount
) : ICapability<T>;

// Caching capability with ordering
public record ConfigurationCacheCapability<T>(
    TimeSpan CacheDuration,
    int Priority
) : ICapability<T>, IOrderedCapability
{
    public int Order => Priority;
}

// File watching capability
public record FileWatcherCapability<T>(string FilePath, DateTime LastChanged) : ICapability<T>;

// Environment-specific overrides
public record EnvironmentOverrideCapability<T>(string Environment, object OverrideValue) : ICapability<T>;
```

## Configuration Service Implementation

```csharp
public class ConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    
    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
    }
    
    // Initialize configuration for a key
    public void ConfigureKey(string section, string key, object value, string environment = "Development")
    {
        var configKey = new ConfigurationKey(section, key);
        
        var composer = Composer.For(configKey);
        
        // Set primary capability based on environment
        composer = environment switch
        {
            "Development" => composer.WithPrimary(new DevelopmentConfigPrimary<ConfigurationKey>()),
            "Staging" => composer.WithPrimary(new StagingConfigPrimary<ConfigurationKey>()),
            "Production" => composer.WithPrimary(new ProductionConfigPrimary<ConfigurationKey>()),
            _ => composer.WithPrimary(new DevelopmentConfigPrimary<ConfigurationKey>())
        };
        
        // Add base configuration value
        composer = composer.Add(new ConfigurationValueCapability<ConfigurationKey>(
            new ConfigurationValue(value, value.GetType())
        ));
        
        // Add environment-specific capabilities
        composer = AddEnvironmentCapabilities(composer, configKey, environment);
        
        // Add monitoring
        composer = composer.Add(new ConfigurationMonitoringCapability<ConfigurationKey>(
            LastAccessed: DateTime.UtcNow,
            LastModified: DateTime.UtcNow,
            AccessCount: 0
        ));
        
        composer.Build();
        
        _logger.LogInformation("Configured {Key} for environment {Environment}", configKey, environment);
    }
    
    private Composer<ConfigurationKey> AddEnvironmentCapabilities(
        Composer<ConfigurationKey> composer, 
        ConfigurationKey key, 
        string environment)
    {
        return environment switch
        {
            "Development" => composer
                .Add(new FileWatcherCapability<ConfigurationKey>("appsettings.Development.json", DateTime.UtcNow))
                .Add(new ConfigurationCacheCapability<ConfigurationKey>(TimeSpan.FromMinutes(1), 300)), // Low priority cache
                
            "Staging" => composer
                .Add(new ConfigurationCacheCapability<ConfigurationKey>(TimeSpan.FromMinutes(5), 200))
                .Add(new ValidationCapability<ConfigurationKey>(ValidateForStaging, "Staging validation failed")),
                
            "Production" => composer
                .Add(new ConfigurationCacheCapability<ConfigurationKey>(TimeSpan.FromMinutes(15), 100)) // High priority cache
                .Add(new ValidationCapability<ConfigurationKey>(ValidateForProduction, "Production validation failed")),
                
            _ => composer
        };
    }
    
    // Get configuration value with type safety
    public T GetValue<T>(string section, string key, T defaultValue = default(T))
    {
        var configKey = new ConfigurationKey(section, key);
        var composition = Composition.FindOrDefault(configKey);
        
        if (composition == null)
        {
            _logger.LogWarning("Configuration key {Key} not found, using default", configKey);
            return defaultValue;
        }
        
        // Update access tracking
        UpdateAccessTracking(composition);
        
        // Check cache first (ordered by priority)
        var cacheCapabilities = composition.GetAll<ConfigurationCacheCapability<ConfigurationKey>>();
        foreach (var cache in cacheCapabilities)
        {
            if (TryGetFromCache<T>(configKey, cache, out var cachedValue))
            {
                _logger.LogDebug("Retrieved {Key} from cache", configKey);
                return cachedValue;
            }
        }
        
        // Get primary configuration value
        var configValues = composition.GetAll<ConfigurationValueCapability<ConfigurationKey>>();
        var primaryValue = configValues.FirstOrDefault();
        
        if (primaryValue?.Value.TryGetValue<T>(out var value) == true)
        {
            // Apply environment overrides
            value = ApplyEnvironmentOverrides(composition, value);
            
            // Cache the value
            CacheValue(configKey, value, cacheCapabilities);
            
            _logger.LogDebug("Retrieved {Key} = {Value}", configKey, value);
            return value;
        }
        
        // Try default value capability
        var defaultCapabilities = composition.GetAll<DefaultValueCapability<ConfigurationKey>>();
        var defaultCapability = defaultCapabilities.FirstOrDefault();
        
        if (defaultCapability != null && defaultCapability.DefaultValue is T defaultVal)
        {
            _logger.LogDebug("Using default value for {Key}", configKey);
            return defaultVal;
        }
        
        _logger.LogWarning("Could not retrieve value for {Key}, using provided default", configKey);
        return defaultValue;
    }
    
    // Feature flag support
    public bool IsFeatureEnabled(string featureName)
    {
        var configKey = new ConfigurationKey("Features", featureName);
        var composition = Composition.FindOrDefault(configKey);
        
        if (composition == null) return false;
        
        var featureFlags = composition.GetAll<FeatureFlagCapability<ConfigurationKey>>();
        var flag = featureFlags.FirstOrDefault();
        
        if (flag == null) return false;
        
        // Check expiration
        if (flag.ExpiresAt.HasValue && DateTime.UtcNow > flag.ExpiresAt.Value)
        {
            _logger.LogInformation("Feature flag {Feature} expired", featureName);
            return false;
        }
        
        return flag.IsEnabled;
    }
    
    // Dynamic configuration updates
    public void UpdateConfiguration(string section, string key, object newValue)
    {
        var configKey = new ConfigurationKey(section, key);
        var existingComposition = Composition.FindOrDefault(configKey);
        
        if (existingComposition == null)
        {
            ConfigureKey(section, key, newValue);
            return;
        }
        
        // Recompose with new value
        var newComposition = Composer.Recompose(existingComposition)
            .RemoveWhere(cap => cap is ConfigurationValueCapability<ConfigurationKey>)
            .Add(new ConfigurationValueCapability<ConfigurationKey>(
                new ConfigurationValue(newValue, newValue.GetType())
            ))
            .RemoveWhere(cap => cap is ConfigurationMonitoringCapability<ConfigurationKey>)
            .Add(new ConfigurationMonitoringCapability<ConfigurationKey>(
                LastAccessed: DateTime.UtcNow,
                LastModified: DateTime.UtcNow,
                AccessCount: 0
            ))
            .Build();
        
        // Invalidate cache
        InvalidateCache(configKey);
        
        _logger.LogInformation("Updated configuration {Key} = {Value}", configKey, newValue);
    }
    
    // Configuration validation
    private bool ValidateForStaging(object value) => value != null;
    
    private bool ValidateForProduction(object value)
    {
        // More strict validation for production
        return value != null && !string.IsNullOrWhiteSpace(value.ToString());
    }
    
    // Helper methods for caching and tracking
    private void UpdateAccessTracking(IComposition<ConfigurationKey> composition)
    {
        var monitoring = composition.GetAll<ConfigurationMonitoringCapability<ConfigurationKey>>().FirstOrDefault();
        if (monitoring != null)
        {
            var updated = monitoring with 
            { 
                LastAccessed = DateTime.UtcNow, 
                AccessCount = monitoring.AccessCount + 1 
            };
            
            var newComposition = Composer.Recompose(composition)
                .RemoveWhere(cap => cap is ConfigurationMonitoringCapability<ConfigurationKey>)
                .Add(updated)
                .Build();
        }
    }
    
    private bool TryGetFromCache<T>(ConfigurationKey key, ConfigurationCacheCapability<ConfigurationKey> cache, out T value)
    {
        // Implementation would check actual cache storage
        value = default(T);
        return false; // Simplified for example
    }
    
    private void CacheValue<T>(ConfigurationKey key, T value, IReadOnlyList<ConfigurationCacheCapability<ConfigurationKey>> caches)
    {
        // Implementation would store in actual cache
    }
    
    private void InvalidateCache(ConfigurationKey key)
    {
        // Implementation would clear cache entries
    }
    
    private T ApplyEnvironmentOverrides<T>(IComposition<ConfigurationKey> composition, T value)
    {
        var overrides = composition.GetAll<EnvironmentOverrideCapability<ConfigurationKey>>();
        var currentEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        var override = overrides.FirstOrDefault(o => o.Environment == currentEnv);
        if (override?.OverrideValue is T overrideValue)
        {
            _logger.LogDebug("Applied environment override for {Environment}", currentEnv);
            return overrideValue;
        }
        
        return value;
    }
}
```

## Usage Examples

### Basic Configuration Setup

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var configService = new ConfigurationService(logger);
        
        // Database configuration
        configService.ConfigureKey("Database", "ConnectionString", 
            "Server=localhost;Database=MyApp;", "Development");
        
        configService.ConfigureKey("Database", "ConnectionString", 
            "Server=prod-db;Database=MyApp;", "Production");
        
        // API settings
        configService.ConfigureKey("API", "BaseUrl", "https://api.dev.com", "Development");
        configService.ConfigureKey("API", "BaseUrl", "https://api.prod.com", "Production");
        configService.ConfigureKey("API", "Timeout", TimeSpan.FromSeconds(30));
        
        // Feature flags
        configService.ConfigureKey("Features", "NewUI", true);
        configService.ConfigureKey("Features", "BetaFeatures", false);
        
        services.AddSingleton(configService);
    }
}
```

### Service Usage

```csharp
public class DatabaseService
{
    private readonly ConfigurationService _config;
    
    public DatabaseService(ConfigurationService config)
    {
        _config = config;
    }
    
    public async Task InitializeAsync()
    {
        var connectionString = _config.GetValue<string>("Database", "ConnectionString");
        var timeout = _config.GetValue<TimeSpan>("Database", "Timeout", TimeSpan.FromSeconds(30));
        
        // Initialize database connection
        await ConnectAsync(connectionString, timeout);
    }
}

public class APIClient
{
    private readonly ConfigurationService _config;
    
    public APIClient(ConfigurationService config)
    {
        _config = config;
    }
    
    public async Task<T> GetAsync<T>(string endpoint)
    {
        var baseUrl = _config.GetValue<string>("API", "BaseUrl");
        var timeout = _config.GetValue<TimeSpan>("API", "Timeout", TimeSpan.FromSeconds(30));
        
        // Check feature flag
        if (_config.IsFeatureEnabled("NewAPI"))
        {
            return await GetFromNewAPIAsync<T>(baseUrl, endpoint, timeout);
        }
        
        return await GetFromLegacyAPIAsync<T>(baseUrl, endpoint, timeout);
    }
}
```

### Dynamic Configuration Updates

```csharp
public class ConfigurationController : ControllerBase
{
    private readonly ConfigurationService _config;
    
    public ConfigurationController(ConfigurationService config)
    {
        _config = config;
    }
    
    [HttpPost("update")]
    public IActionResult UpdateConfiguration([FromBody] UpdateConfigRequest request)
    {
        try
        {
            _config.UpdateConfiguration(request.Section, request.Key, request.Value);
            return Ok($"Updated {request.Section}:{request.Key}");
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to update configuration: {ex.Message}");
        }
    }
    
    [HttpPost("feature-flag")]
    public IActionResult ToggleFeatureFlag([FromBody] FeatureFlagRequest request)
    {
        _config.UpdateConfiguration("Features", request.FeatureName, request.IsEnabled);
        return Ok($"Feature {request.FeatureName} set to {request.IsEnabled}");
    }
}
```

### Environment-Specific Behavior

```csharp
public class EmailService
{
    private readonly ConfigurationService _config;
    
    public EmailService(ConfigurationService config)
    {
        _config = config;
    }
    
    public async Task SendAsync(string to, string subject, string body)
    {
        var emailKey = new ConfigurationKey("Email", "Provider");
        var composition = Composition.FindOrDefault(emailKey);
        
        if (composition == null)
        {
            await SendDefaultEmail(to, subject, body);
            return;
        }
        
        // Adapt behavior based on primary capability (environment)
        var result = composition.GetPrimaryOrDefault() switch
        {
            DevelopmentConfigPrimary<ConfigurationKey> => 
                await SendToConsole(to, subject, body),
                
            StagingConfigPrimary<ConfigurationKey> => 
                await SendToTestMailbox(to, subject, body),
                
            ProductionConfigPrimary<ConfigurationKey> => 
                await SendViaRealProvider(to, subject, body),
                
            _ => await SendDefaultEmail(to, subject, body)
        };
    }
}
```

## Advanced Patterns

### Configuration Inheritance

```csharp
public void ConfigureInheritedSettings()
{
    var baseKey = new ConfigurationKey("Logging", "Level");
    var specificKey = new ConfigurationKey("Logging.Database", "Level");
    
    // Base configuration
    Composer.For(baseKey)
        .WithPrimary(new DevelopmentConfigPrimary<ConfigurationKey>())
        .Add(new ConfigurationValueCapability<ConfigurationKey>(
            new ConfigurationValue(LogLevel.Information, typeof(LogLevel))
        ))
        .Build();
    
    // Specific configuration inherits from base
    Composer.For(specificKey)
        .WithPrimary(new DevelopmentConfigPrimary<ConfigurationKey>())
        .Add(new ConfigurationValueCapability<ConfigurationKey>(
            new ConfigurationValue(LogLevel.Debug, typeof(LogLevel))
        ))
        .Add(new DefaultValueCapability<ConfigurationKey>(LogLevel.Information)) // Fallback to base
        .Build();
}
```

### Multi-Source Configuration

```csharp
public void ConfigureMultiSourceSettings()
{
    var key = new ConfigurationKey("API", "Key");
    
    Composer.For(key)
        .WithPrimary(new FileConfigPrimary<ConfigurationKey>("appsettings.json"))
        .Add(new EnvironmentOverrideCapability<ConfigurationKey>("Development", "dev-api-key"))
        .Add(new EnvironmentOverrideCapability<ConfigurationKey>("Production", "prod-api-key"))
        .Add(new ConfigurationCacheCapability<ConfigurationKey>(TimeSpan.FromHours(1), 100))
        .Build();
}
```

This configuration system demonstrates the full power of Cocoar.Capabilities for building flexible, extensible systems that adapt to different environments and requirements while maintaining type safety and performance.