# Capability Patterns Cookbook

A collection of practical patterns and creative use cases for Cocoar.Capabilities, organized by domain and complexity.

> **💡 Philosophy**: "Data with behavior" – Capabilities let any object declare how it participates in your system.

---

## 🎯 **Value Type Extension Patterns**

Value types (integers, structs, enums) can hold capabilities with value equality semantics.

### **Enum Enrichment**
Add metadata to enums you cannot modify:

```csharp
// Enrich HTTP status codes
Composer.For(HttpStatusCode.NotFound)
    .Add(new DescriptionCapability("Resource not found"))
    .Add(new SeverityCapability(Severity.Warning))
    .Add(new RetryPolicyCapability(shouldRetry: false))
    .Build();

// Usage
string Describe(HttpStatusCode code) =>
    Composition.FindOrDefault(code)?
        .GetAll<DescriptionCapability>()
        .FirstOrDefault()?.Description ?? code.ToString();

bool ShouldRetry(HttpStatusCode code) =>
    Composition.FindOrDefault(code)?
        .GetAll<RetryPolicyCapability>()
        .FirstOrDefault()?.ShouldRetry ?? false;
```

### **Feature Flags with Capabilities**
```csharp
Composer.For("feature:dark-mode")
    .Add(new ToggleCapability(enabled: true))
    .Add(new RolloutCapability(percentage: 35))
    .Add(new AuditCapability(trackUsage: true))
    .Build();

// Feature flag evaluation
bool IsFeatureEnabled(string feature, int userId) =>
    Composition.FindOrDefault(feature) is { } comp &&
    comp.GetAll<ToggleCapability>().Any(t => t.Enabled) &&
    comp.GetAll<RolloutCapability>().Any(r => (userId % 100) < r.Percentage);
```

### **Sensitive Data Protection**
```csharp
// Mark sensitive fields
Composer.For("ssn")
    .Add(new SecurityClassificationCapability(Sensitivity.High))
    .Add(new RedactionCapability("***-**-****"))
    .Add(new AuditCapability(logAccess: true))
    .Build();

// Automatic sanitization
string Sanitize(string fieldName, string value)
{
    var comp = Composition.FindOrDefault(fieldName);
    if (comp?.GetAll<RedactionCapability>().FirstOrDefault() is { } redaction)
    {
        // Log access if auditing enabled
        if (comp.GetAll<AuditCapability>().Any(a => a.LogAccess))
            Logger.LogAccess(fieldName, DateTime.UtcNow);
            
        return redaction.Mask;
    }
    return value;
}
```

---

## 🌐 **Cross-Cutting Concern Patterns**

### **Universal Logging Strategy**
```csharp
// Different subjects, same logging behavior
Composer.For(userService)
    .Add(new LoggingCapability(LogLevel.Info, category: "UserManagement"))
    .Build();

Composer.For(orderService) 
    .Add(new LoggingCapability(LogLevel.Warning, category: "OrderProcessing"))
    .Build();

// Generic logging processor
void LogOperation<T>(T subject, string operation)
{
    var comp = Composition.FindOrDefault(subject);
    foreach (var log in comp?.GetAll<LoggingCapability>() ?? [])
    {
        Logger.Log(log.Level, $"[{log.Category}] {operation} on {typeof(T).Name}");
    }
}
```

### **Caching with TTL**
```csharp
Composer.For(expensiveService)
    .Add(new CachingCapability(TimeSpan.FromMinutes(5)))
    .Add(new CacheKeyCapability(key => $"service:{key.GetHashCode()}"))
    .Build();

// Generic caching wrapper
async Task<TResult> WithCaching<T, TResult>(T subject, string operation, Func<Task<TResult>> operation)
{
    var comp = Composition.FindOrDefault(subject);
    var cache = comp?.GetAll<CachingCapability>().FirstOrDefault();
    var keyGen = comp?.GetAll<CacheKeyCapability>().FirstOrDefault();
    
    if (cache != null && keyGen != null)
    {
        var key = keyGen.Generator(operation);
        if (MemoryCache.TryGetValue(key, out TResult cached))
            return cached;
            
        var result = await operation();
        MemoryCache.Set(key, result, cache.Duration);
        return result;
    }
    
    return await operation();
}
```

---

## 🔄 **Event-Driven Patterns**

### **Event Handler Registration**
```csharp
// Register event handlers via capabilities
Composer.For(typeof(OrderCreated))
    .Add(new EventHandlerCapability<OrderCreated>(new EmailNotificationHandler()))
    .Add(new EventHandlerCapability<OrderCreated>(new InventoryUpdateHandler()))
    .Add(new EventHandlerCapability<OrderCreated>(new AuditLogHandler()))
    .Build();

// Generic event dispatcher
async Task PublishEvent<T>(T eventData)
{
    var comp = Composition.FindOrDefault(typeof(T));
    var handlers = comp?.GetAll<EventHandlerCapability<T>>() ?? [];
    
    foreach (var handler in handlers)
    {
        await handler.HandleAsync(eventData);
    }
}
```

### **Ordered Middleware Pipelines**
```csharp
// Build processing pipeline with capabilities
Composer.For(apiEndpoint)
    .Add(new MiddlewareCapability(new AuthenticationMiddleware(), order: 1))
    .Add(new MiddlewareCapability(new RateLimitingMiddleware(), order: 2))
    .Add(new MiddlewareCapability(new LoggingMiddleware(), order: 3))
    .Build();

// Execute pipeline in order
async Task<HttpResponse> ProcessRequest(HttpRequest request, string endpoint)
{
    var comp = Composition.FindOrDefault(endpoint);
    var middlewares = comp?.GetAll<MiddlewareCapability>()
        .OrderBy(m => m.Order)
        .ToArray() ?? [];
    
    var context = new RequestContext(request);
    
    foreach (var middleware in middlewares)
    {
        await middleware.Component.ProcessAsync(context);
        if (context.ShouldShortCircuit) break;
    }
    
    return context.Response;
}
```

---

## 🏗️ **Architectural Patterns**

### **Plugin Architecture**
```csharp
// Register plugins with capabilities
Composer.For("reporting-system")
    .Add(new PluginCapability("pdf-exporter", new PdfExportPlugin()))
    .Add(new PluginCapability("excel-exporter", new ExcelExportPlugin()))
    .Add(new PluginCapability("email-sender", new EmailPlugin()))
    .Build();

// Dynamic plugin discovery
IEnumerable<T> GetPluginsOfType<T>(string system)
{
    var comp = Composition.FindOrDefault(system);
    return comp?.GetAll<PluginCapability>()
        .Where(p => p.Instance is T)
        .Select(p => (T)p.Instance) ?? [];
}
```

### **Configuration Strategy Pattern**
```csharp
// Different environments, different strategies
Composer.For("database-config")
    .WithPrimary(new DatabasePrimaryCapability()) // Single source of truth
    .Add(new ConnectionStringCapability(Environment.GetConnectionString()))
    .Add(new RetryPolicyCapability(maxRetries: 3))
    .Add(new TimeoutCapability(TimeSpan.FromSeconds(30)))
    .Build();

// Environment-specific capabilities
if (Environment.IsDevelopment())
{
    Composer.Recompose("database-config")
        .Add(new DebuggingCapability(enabled: true))
        .Add(new VerboseLoggingCapability(enabled: true))
        .Build();
}
```

### **Cross-Project Extension Points**
```csharp
// Core project defines extension points
public static class ServiceExtensions
{
    public static Composer<T> AddLogging<T>(this Composer<T> composer, LogLevel level)
        => composer.Add(new LoggingCapability<T>(level));
        
    public static Composer<T> AddCaching<T>(this Composer<T> composer, TimeSpan duration)
        => composer.Add(new CachingCapability<T>(duration));
}

// Extension project adds new capabilities without circular dependencies
public static class DiExtensions
{
    public static Composer<T> AsSingleton<T>(this Composer<T> composer)
        => composer.Add(new SingletonLifetimeCapability<T>());
        
    public static Composer<T> AsScoped<T>(this Composer<T> composer)
        => composer.Add(new ScopedLifetimeCapability<T>());
}

// Usage combines both projects seamlessly
var composition = Composer.For(userService)
    .AddLogging(LogLevel.Info)    // Core project extension
    .AddCaching(TimeSpan.FromMinutes(5))  // Core project extension
    .AsSingleton()               // DI project extension
    .Build();
```

---

## 🎨 **Creative Patterns**

### **Capability-Based Permissions**
```csharp
// Define permissions as capabilities
Composer.For(currentUser)
    .Add(new PermissionCapability("users:read"))
    .Add(new PermissionCapability("users:write"))
    .Add(new RoleCapability("administrator"))
    .Build();

// Permission checking
bool HasPermission(object user, string permission) =>
    Composition.FindOrDefault(user)?
        .GetAll<PermissionCapability>()
        .Any(p => p.Permission == permission) ?? false;
```

### **Type-Safe Configuration**
```csharp
// Attach configuration to types themselves
Composer.For(typeof(EmailService))
    .Add(new SmtpConfigurationCapability("smtp.example.com", 587))
    .Add(new RetryConfigurationCapability(maxRetries: 3))
    .Build();

// Automatic configuration injection
T CreateConfiguredService<T>() where T : new()
{
    var service = new T();
    var comp = Composition.FindOrDefault(typeof(T));
    
    // Apply all configuration capabilities
    foreach (var config in comp?.GetAll<IConfigurationCapability>() ?? [])
    {
        config.Configure(service);
    }
    
    return service;
}
```

---

## 📊 **Performance Patterns**

### **Lazy Loading with Capabilities**
```csharp
Composer.For(expensiveResource)
    .Add(new LazyLoadingCapability<ExpensiveData>(() => LoadExpensiveData()))
    .Add(new CachingCapability(TimeSpan.FromHours(1)))
    .Build();

// Lazy access with caching
TData GetData<TData>(object resource)
{
    var comp = Composition.FindOrDefault(resource);
    var lazy = comp?.GetAll<LazyLoadingCapability<TData>>().FirstOrDefault();
    return lazy?.Value ?? default(TData);
}
```

### **Batching Operations**
```csharp
Composer.For(dbContext)
    .Add(new BatchingCapability(batchSize: 100))
    .Add(new FlushPolicyCapability(TimeSpan.FromSeconds(5)))
    .Build();

// Automatic batching based on capabilities
void AddOperation<T>(T context, IOperation operation)
{
    var comp = Composition.FindOrDefault(context);
    var batcher = comp?.GetAll<BatchingCapability>().FirstOrDefault();
    
    if (batcher != null)
    {
        batcher.AddOperation(operation);
        if (batcher.ShouldFlush())
        {
            batcher.ExecuteBatch();
        }
    }
    else
    {
        operation.Execute(); // Immediate execution
    }
}
```

---

## 🔧 **Best Practices Summary**

### 1. **Capability Design Principles**
- **Single Responsibility**: Each capability should have one clear purpose
- **Immutable Data**: Prefer record types for capability data
- **Descriptive Naming**: Use clear, intention-revealing names

### 2. **Performance Considerations**
- **Small Compositions**: Keep capability counts reasonable (typically < 10)
- **Value Type Cleanup**: Remember to remove value type compositions
- **Lazy Evaluation**: Use lazy patterns for expensive operations

### 3. **Architecture Guidelines**
- **Interface Contracts**: Use interfaces for cross-project flexibility
- **Ordered Processing**: Implement `IOrderedCapability` for pipeline scenarios
- **Primary Capabilities**: Use for single-source-of-truth patterns

### 4. **Testing Strategies**
- **Isolated Testing**: Test capabilities independently
- **Composition Testing**: Verify capability interactions
- **Mock Compositions**: Use test doubles for complex scenarios

---

*These patterns demonstrate the flexibility and power of capability composition for solving real-world architectural challenges.*