# Primary Capabilities Guide

Understanding and implementing the Primary Capability Strategy Pattern in Cocoar.Capabilities.

## What are Primary Capabilities?

Primary capabilities are a special type of capability that defines the **main behavior, type, or strategy** for a subject. Unlike regular capabilities where you can have multiple instances of the same type, each subject can have only **one primary capability**.

## The Primary Capability Strategy Pattern

This pattern enables sophisticated cross-project extensibility by using **unified subjects** with **strategy-defining primary capabilities**.

### Core Pattern Structure

```csharp
// 1. Define a unified subject (shared across projects)
public class ConfigurationKey
{
    public string Section { get; init; }
    public string Key { get; init; }
    public override int GetHashCode() => HashCode.Combine(Section, Key);
    public override bool Equals(object? obj) => /* value equality */;
}

// 2. Define primary capabilities for different strategies
public record DatabaseConfigPrimary<T> : IPrimaryCapability<T>;
public record FileConfigPrimary<T> : IPrimaryCapability<T>;
public record CloudConfigPrimary<T> : IPrimaryCapability<T>;

// 3. Different projects register their strategies
var configKey = new ConfigurationKey { Section = "Database", Key = "ConnectionString" };

// Project A: Database-first approach
Composer.For(configKey)
    .WithPrimary(new DatabaseConfigPrimary<ConfigurationKey>())
    .Add(new ConnectionPoolCapability<ConfigurationKey>(size: 20))
    .Build();

// Project B: File-based approach  
Composer.For(configKey)
    .WithPrimary(new FileConfigPrimary<ConfigurationKey>())
    .Add(new FileWatcherCapability<ConfigurationKey>(path: "config.json"))
    .Build();
```

## Interface Definition

```csharp
public interface IPrimaryCapability<in T> : ICapability<T> { }
```

Primary capabilities inherit from `ICapability<T>` but add the constraint that only one can exist per subject.

## Registration API

### Setting Primary Capabilities

```csharp
// Set a primary capability
var composition = Composer.For(subject)
    .WithPrimary(new DatabasePrimaryCapability<Subject>())
    .Add(new LoggingCapability<Subject>(LogLevel.Info))
    .Build();

// Replace existing primary (if any)
var updated = Composer.Recompose(composition)
    .WithPrimary(new CachePrimaryCapability<Subject>())
    .Build();

// Remove primary capability
var withoutPrimary = Composer.Recompose(composition)
    .WithPrimary(null)
    .Build();
```

### Multiple Primary Capability Error

```csharp
// This will throw InvalidOperationException
try
{
    var invalid = Composer.For(subject)
        .WithPrimary(new FirstPrimary<Subject>())
        .WithPrimary(new SecondPrimary<Subject>()) // Error!
        .Build();
}
catch (InvalidOperationException ex)
{
    // "Multiple primary capabilities registered for 'Subject'. Only one primary capability is allowed."
}
```

## Query API

### Basic Primary Capability Queries

```csharp
// Check if any primary capability exists
bool hasPrimary = composition.HasPrimary();

// Check for specific primary capability type
bool hasDbPrimary = composition.HasPrimary<DatabasePrimaryCapability<Subject>>();

// Try to get primary capability (non-generic)
if (composition.TryGetPrimary(out IPrimaryCapability<Subject> primary))
{
    // Use primary capability
}

// Get primary or null
IPrimaryCapability<Subject>? primary = composition.GetPrimaryOrDefault();

// Get primary (throws if not found)
IPrimaryCapability<Subject> required = composition.GetPrimary();
```

### Typed Primary Capability Queries

```csharp
// Try to get specific primary type
if (composition.TryGetPrimaryAs<DatabasePrimaryCapability<Subject>>(out var dbPrimary))
{
    // Use typed primary capability
}

// Get typed primary or null
DatabasePrimaryCapability<Subject>? dbPrimary = 
    composition.GetPrimaryOrDefaultAs<DatabasePrimaryCapability<Subject>>();

// Get typed primary (throws if not found or wrong type)
DatabasePrimaryCapability<Subject> required = 
    composition.GetRequiredPrimaryAs<DatabasePrimaryCapability<Subject>>();
```

## Real-World Examples

### Example 1: Storage Strategy Pattern

```csharp
// Define storage strategies as primary capabilities
public record DatabaseStoragePrimary<T>(string ConnectionString) : IPrimaryCapability<T>;
public record FileStoragePrimary<T>(string Directory) : IPrimaryCapability<T>;
public record CloudStoragePrimary<T>(string BucketName) : IPrimaryCapability<T>;

// Entity configuration
public class UserEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// Configure storage strategy
public void ConfigureUserStorage()
{
    var userEntity = new UserEntity();
    
    // Development: File storage
    if (Environment.IsDevelopment())
    {
        Composer.For(userEntity)
            .WithPrimary(new FileStoragePrimary<UserEntity>("./data"))
            .Add(new JsonSerializationCapability<UserEntity>())
            .Add(new FileWatcherCapability<UserEntity>())
            .Build();
    }
    // Production: Database storage
    else
    {
        Composer.For(userEntity)
            .WithPrimary(new DatabaseStoragePrimary<UserEntity>("Server=prod;..."))
            .Add(new ConnectionPoolCapability<UserEntity>(size: 50))
            .Add(new CachingCapability<UserEntity>(TimeSpan.FromMinutes(10)))
            .Build();
    }
}

// Repository implementation adapts to strategy
public class UserRepository
{
    public async Task<User> GetAsync(int id)
    {
        var entity = new UserEntity();
        var composition = Composition.FindRequired(entity);
        
        // Adapt behavior based on primary capability
        return composition.GetPrimaryOrDefault() switch
        {
            DatabaseStoragePrimary<UserEntity> db => await GetFromDatabase(id, db.ConnectionString),
            FileStoragePrimary<UserEntity> file => await GetFromFile(id, file.Directory),
            CloudStoragePrimary<UserEntity> cloud => await GetFromCloud(id, cloud.BucketName),
            _ => throw new InvalidOperationException("No storage strategy configured")
        };
    }
}
```

### Example 2: Processing Pipeline Strategy

```csharp
// Pipeline strategies as primary capabilities
public record BatchProcessingPrimary<T>(int BatchSize) : IPrimaryCapability<T>;
public record StreamProcessingPrimary<T>(int BufferSize) : IPrimaryCapability<T>;
public record RealtimeProcessingPrimary<T> : IPrimaryCapability<T>;

// Data pipeline configuration
public class DataPipeline
{
    public string Name { get; init; }
}

// Configure processing strategy
public void ConfigureDataPipeline()
{
    var pipeline = new DataPipeline { Name = "UserEvents" };
    
    // High-volume: Batch processing
    if (IsHighVolumeScenario())
    {
        Composer.For(pipeline)
            .WithPrimary(new BatchProcessingPrimary<DataPipeline>(batchSize: 1000))
            .Add(new CompressionCapability<DataPipeline>())
            .Add(new DatabaseBulkInsertCapability<DataPipeline>())
            .Build();
    }
    // Real-time: Stream processing
    else
    {
        Composer.For(pipeline)
            .WithPrimary(new RealtimeProcessingPrimary<DataPipeline>())
            .Add(new EventStreamCapability<DataPipeline>())
            .Add(new WebSocketNotificationCapability<DataPipeline>())
            .Build();
    }
}

// Processor adapts to strategy
public class DataProcessor
{
    public async Task ProcessAsync(IEnumerable<Event> events)
    {
        var pipeline = new DataPipeline { Name = "UserEvents" };
        var composition = Composition.FindRequired(pipeline);
        
        switch (composition.GetPrimaryOrDefault())
        {
            case BatchProcessingPrimary<DataPipeline> batch:
                await ProcessInBatches(events, batch.BatchSize);
                break;
                
            case StreamProcessingPrimary<DataPipeline> stream:
                await ProcessAsStream(events, stream.BufferSize);
                break;
                
            case RealtimeProcessingPrimary<DataPipeline>:
                await ProcessRealtime(events);
                break;
                
            default:
                throw new InvalidOperationException("No processing strategy configured");
        }
    }
}
```

### Example 3: Cross-Project Extension Points

```csharp
// Shared model (in common library)
public class APIEndpoint
{
    public string Path { get; init; }
    public string Method { get; init; }
    
    public override int GetHashCode() => HashCode.Combine(Path, Method);
    public override bool Equals(object? obj) => /* value equality */;
}

// Project A: Authentication-first approach
public record AuthenticationPrimary<T>(string Scheme) : IPrimaryCapability<T>;

public class AuthenticationModule
{
    public void ConfigureEndpoint(string path, string method)
    {
        var endpoint = new APIEndpoint { Path = path, Method = method };
        
        Composer.For(endpoint)
            .WithPrimary(new AuthenticationPrimary<APIEndpoint>("Bearer"))
            .Add(new JwtValidationCapability<APIEndpoint>())
            .Add(new RoleAuthorizationCapability<APIEndpoint>(["Admin"]))
            .Build();
    }
}

// Project B: Rate limiting approach
public record RateLimitingPrimary<T>(int RequestsPerMinute) : IPrimaryCapability<T>;

public class RateLimitingModule
{
    public void ConfigureEndpoint(string path, string method)
    {
        var endpoint = new APIEndpoint { Path = path, Method = method };
        
        Composer.For(endpoint)
            .WithPrimary(new RateLimitingPrimary<APIEndpoint>(requestsPerMinute: 100))
            .Add(new IpTrackingCapability<APIEndpoint>())
            .Add(new ThrottlingCapability<APIEndpoint>())
            .Build();
    }
}

// Shared middleware adapts to any primary strategy
public class APIMiddleware
{
    public async Task ProcessRequest(string path, string method)
    {
        var endpoint = new APIEndpoint { Path = path, Method = method };
        var composition = Composition.FindOrDefault(endpoint);
        
        if (composition == null)
        {
            await ProcessDefault();
            return;
        }
        
        // Handle different primary strategies
        switch (composition.GetPrimaryOrDefault())
        {
            case AuthenticationPrimary<APIEndpoint> auth:
                await ProcessWithAuthentication(auth.Scheme);
                break;
                
            case RateLimitingPrimary<APIEndpoint> rateLimit:
                await ProcessWithRateLimit(rateLimit.RequestsPerMinute);
                break;
                
            default:
                await ProcessDefault();
                break;
        }
    }
}
```

## Best Practices

### 1. Use Value Objects as Subjects

```csharp
// Good: Value object with proper equality
public record ConfigurationKey(string Section, string Key);

// Good: Class with value equality
public class APIEndpoint
{
    public string Path { get; init; }
    public string Method { get; init; }
    
    public override int GetHashCode() => HashCode.Combine(Path, Method);
    public override bool Equals(object? obj) => /* proper value equality */;
}

// Avoid: Reference types without proper equality
public class BadSubject { } // Uses reference equality
```

### 2. Make Primary Capabilities Descriptive

```csharp
// Good: Describes the strategy clearly
public record DatabaseStoragePrimary<T>(string ConnectionString) : IPrimaryCapability<T>;
public record InMemoryCachingPrimary<T>(TimeSpan Duration) : IPrimaryCapability<T>;

// Avoid: Generic or unclear names
public record ConfigPrimary<T> : IPrimaryCapability<T>; // What kind of config?
public record HandlerPrimary<T> : IPrimaryCapability<T>; // What does it handle?
```

### 3. Combine with Regular Capabilities

```csharp
// Primary defines the strategy, regular capabilities add features
var composition = Composer.For(entity)
    .WithPrimary(new DatabaseStoragePrimary<Entity>("connection"))  // Strategy
    .Add(new LoggingCapability<Entity>(LogLevel.Debug))            // Feature
    .Add(new CachingCapability<Entity>(TimeSpan.FromMinutes(5)))   // Feature
    .Add(new ValidationCapability<Entity>())                       // Feature
    .Build();
```

### 4. Handle Missing Primary Capabilities

```csharp
// Always handle the case where no primary capability is set
public void ProcessEntity(Entity entity)
{
    var composition = Composition.FindOrDefault(entity);
    
    var primary = composition?.GetPrimaryOrDefault();
    if (primary == null)
    {
        // Use default behavior or throw meaningful error
        throw new InvalidOperationException($"No processing strategy configured for {entity}");
    }
    
    // Process based on primary capability type
}
```

## Performance Considerations

- **Single Storage**: Primary capabilities use the same storage as regular capabilities
- **Query Performance**: Primary capability queries are O(1) operations
- **Memory Impact**: Minimal - primary capabilities are stored alongside other capabilities
- **Thread Safety**: All primary capability operations are thread-safe through immutability

## Error Scenarios

### Multiple Primary Registration

```csharp
// This pattern will fail
var composer = Composer.For(subject)
    .WithPrimary(new FirstPrimary<Subject>())
    .WithPrimary(new SecondPrimary<Subject>()); // InvalidOperationException

// Instead, replace the primary
var composer = Composer.For(subject)
    .WithPrimary(new FirstPrimary<Subject>())
    .WithPrimary(null)  // Remove first
    .WithPrimary(new SecondPrimary<Subject>()); // Set new
```

### Wrong Type Queries

```csharp
// This will throw if wrong type or not found
try
{
    var dbPrimary = composition.GetRequiredPrimaryAs<DatabasePrimary<Subject>>();
}
catch (InvalidOperationException)
{
    // Handle missing or wrong type primary capability
}

// Safer approach
var dbPrimary = composition.GetPrimaryOrDefaultAs<DatabasePrimary<Subject>>();
if (dbPrimary != null)
{
    // Use database primary
}
```

## Related Patterns

- **Strategy Pattern**: Primary capabilities implement strategy selection
- **Factory Pattern**: Use primary capabilities to determine object creation strategy
- **Template Method**: Primary capabilities can define algorithmic variations
- **Chain of Responsibility**: Combine with ordered regular capabilities for processing chains

Primary capabilities provide a powerful way to implement cross-cutting architectural strategies while maintaining loose coupling and extensibility.