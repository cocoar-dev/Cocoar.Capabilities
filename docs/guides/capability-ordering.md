# Capability Ordering Guide

Understanding how capabilities are ordered and controlling execution sequence with `IOrderedCapability`.

## Overview

Cocoar.Capabilities provides automatic ordering for capabilities that implement `IOrderedCapability`. This is essential for scenarios where capabilities need to execute in a specific sequence, such as middleware pipelines, processing chains, or layered behaviors.

## IOrderedCapability Interface

```csharp
public interface IOrderedCapability
{
    int Order { get; }
}
```

### Ordering Rules

1. **Ascending Order**: Capabilities are ordered by `Order` property in ascending order (lowest first)
2. **Stable Sort**: Capabilities with the same `Order` value maintain their registration sequence
3. **Mixed Capabilities**: Ordered and non-ordered capabilities can coexist
4. **Default Position**: Non-ordered capabilities appear after ordered ones

## Basic Usage

### Simple Ordered Capability

```csharp
public record MiddlewareCapability<T>(string Name, int Priority) 
    : ICapability<T>, IOrderedCapability
{
    public int Order => Priority;
}

// Register with different priorities
var composition = Composer.For(pipeline)
    .Add(new MiddlewareCapability<Pipeline>("Authentication", 100))
    .Add(new MiddlewareCapability<Pipeline>("Logging", 300))
    .Add(new MiddlewareCapability<Pipeline>("Validation", 200))
    .Build();

// GetAll() returns in order: Authentication (100), Validation (200), Logging (300)
var orderedMiddleware = composition.GetAll<MiddlewareCapability<Pipeline>>();
```

### Processing Order Example

```csharp
public record ProcessorCapability<T>(string Name, int Stage) 
    : ICapability<T>, IOrderedCapability
{
    public int Order => Stage;
}

// Registration order doesn't matter
var composition = Composer.For(data)
    .Add(new ProcessorCapability<Data>("Cleanup", 300))
    .Add(new ProcessorCapability<Data>("Validation", 100))
    .Add(new ProcessorCapability<Data>("Transform", 200))
    .Build();

// Execution follows Order property
var processors = composition.GetAll<ProcessorCapability<Data>>();
foreach (var processor in processors)
{
    Console.WriteLine($"Processing: {processor.Name}"); 
    // Output: Validation, Transform, Cleanup
}
```

## Real-World Examples

### Example 1: HTTP Middleware Pipeline

```csharp
// Base middleware capability
public abstract record MiddlewareCapability<T>(int Priority) 
    : ICapability<T>, IOrderedCapability
{
    public int Order => Priority;
    public abstract Task ProcessAsync(HttpContext context, Func<Task> next);
}

// Specific middleware implementations
public record AuthenticationMiddleware<T> : MiddlewareCapability<T>(100)
{
    public override async Task ProcessAsync(HttpContext context, Func<Task> next)
    {
        // Authentication logic
        await next();
    }
}

public record RateLimitingMiddleware<T> : MiddlewareCapability<T>(200)
{
    public override async Task ProcessAsync(HttpContext context, Func<Task> next)
    {
        // Rate limiting logic
        await next();
    }
}

public record LoggingMiddleware<T> : MiddlewareCapability<T>(300)
{
    public override async Task ProcessAsync(HttpContext context, Func<Task> next)
    {
        // Logging logic
        await next();
    }
}

// Configure pipeline
public class WebApplication
{
    public void ConfigureMiddleware()
    {
        var app = new Application();
        
        // Order doesn't matter during registration
        Composer.For(app)
            .Add(new LoggingMiddleware<Application>())          // Will execute 3rd
            .Add(new AuthenticationMiddleware<Application>())   // Will execute 1st  
            .Add(new RateLimitingMiddleware<Application>())     // Will execute 2nd
            .Build();
    }
    
    public async Task ProcessRequest(HttpContext context)
    {
        var app = new Application();
        var composition = Composition.FindRequired(app);
        var middleware = composition.GetAll<MiddlewareCapability<Application>>();
        
        // Build execution chain in order
        Func<Task> next = () => Task.CompletedTask;
        
        // Reverse iteration to build proper call chain
        for (int i = middleware.Count - 1; i >= 0; i--)
        {
            var current = middleware[i];
            var currentNext = next;
            next = () => current.ProcessAsync(context, currentNext);
        }
        
        await next(); // Execute: Auth -> RateLimit -> Logging
    }
}
```

### Example 2: Data Processing Pipeline

```csharp
// Processing stage capability
public record ProcessingStageCapability<T>(string Name, int StageOrder, ProcessingAction<T> Action) 
    : ICapability<T>, IOrderedCapability
{
    public int Order => StageOrder;
}

public delegate Task<T> ProcessingAction<T>(T input);

// Configure data processing stages
public class DataProcessor
{
    public void ConfigureProcessing()
    {
        var processor = new DataProcessor();
        
        Composer.For(processor)
            .Add(new ProcessingStageCapability<Data>("Validate", 100, ValidateData))
            .Add(new ProcessingStageCapability<Data>("Transform", 200, TransformData))
            .Add(new ProcessingStageCapability<Data>("Enrich", 300, EnrichData))
            .Add(new ProcessingStageCapability<Data>("Save", 400, SaveData))
            .Build();
    }
    
    public async Task<Data> ProcessAsync(Data input)
    {
        var composition = Composition.FindRequired(this);
        var stages = composition.GetAll<ProcessingStageCapability<Data>>();
        
        var result = input;
        foreach (var stage in stages)
        {
            Console.WriteLine($"Executing stage: {stage.Name}");
            result = await stage.Action(result);
        }
        
        return result;
    }
}
```

### Example 3: Event Handler Priority

```csharp
// Event handler with priority
public record EventHandlerCapability<T>(string HandlerName, int Priority, Func<Event, Task> Handler) 
    : ICapability<T>, IOrderedCapability
{
    public int Order => Priority;
}

// Configure event handlers
public class EventSystem
{
    public void ConfigureHandlers()
    {
        var eventBus = new EventBus();
        
        Composer.For(eventBus)
            // Critical handlers first (low priority numbers)
            .Add(new EventHandlerCapability<EventBus>("Security", 10, HandleSecurity))
            .Add(new EventHandlerCapability<EventBus>("Validation", 20, HandleValidation))
            
            // Business logic handlers
            .Add(new EventHandlerCapability<EventBus>("Business", 100, HandleBusiness))
            .Add(new EventHandlerCapability<EventBus>("Notification", 200, HandleNotification))
            
            // Cleanup handlers last
            .Add(new EventHandlerCapability<EventBus>("Cleanup", 300, HandleCleanup))
            .Add(new EventHandlerCapability<EventBus>("Logging", 400, HandleLogging))
            .Build();
    }
    
    public async Task PublishAsync(Event eventData)
    {
        var eventBus = new EventBus();
        var composition = Composition.FindRequired(eventBus);
        var handlers = composition.GetAll<EventHandlerCapability<EventBus>>();
        
        foreach (var handler in handlers)
        {
            try
            {
                await handler.Handler(eventData);
            }
            catch (Exception ex)
            {
                // Log error but continue with remaining handlers
                Console.WriteLine($"Handler {handler.HandlerName} failed: {ex.Message}");
            }
        }
    }
}
```

## Ordering Strategies

### 1. Priority-Based Ordering

```csharp
// Use priority levels for different categories
public enum Priority
{
    Critical = 0,
    High = 100,
    Normal = 200,
    Low = 300,
    Background = 400
}

public record TaskCapability<T>(string Name, Priority Priority) 
    : ICapability<T>, IOrderedCapability
{
    public int Order => (int)Priority;
}
```

### 2. Stage-Based Ordering

```csharp
// Use stage numbers for pipeline processing
public enum ProcessingStage
{
    PreValidation = 100,
    Validation = 200,
    Transformation = 300,
    BusinessLogic = 400,
    PostProcessing = 500,
    Cleanup = 600
}

public record StageCapability<T>(ProcessingStage Stage) 
    : ICapability<T>, IOrderedCapability
{
    public int Order => (int)Stage;
}
```

### 3. Layered Ordering

```csharp
// Use decimal-like ordering for fine-grained control
public record LayeredCapability<T>(string Layer, double OrderValue) 
    : ICapability<T>, IOrderedCapability
{
    public int Order => (int)(OrderValue * 100); // Convert to int
}

// Usage with fine-grained control
.Add(new LayeredCapability<T>("Auth", 1.0))      // Order: 100
.Add(new LayeredCapability<T>("AuthLog", 1.1))   // Order: 110
.Add(new LayeredCapability<T>("Validation", 2.0)) // Order: 200
```

## Mixed Ordered and Non-Ordered Capabilities

```csharp
// Mix of ordered and non-ordered capabilities
public record OrderedCapability<T>(int Priority) : ICapability<T>, IOrderedCapability
{
    public int Order => Priority;
}

public record RegularCapability<T>(string Name) : ICapability<T>;

var composition = Composer.For(subject)
    .Add(new RegularCapability<Subject>("First"))           // Will appear after ordered
    .Add(new OrderedCapability<Subject>(100))               // Will appear first
    .Add(new RegularCapability<Subject>("Second"))          // Will appear after ordered
    .Add(new OrderedCapability<Subject>(50))                // Will appear before first ordered
    .Build();

// Result order when getting all ICapability<Subject>:
// 1. OrderedCapability(50)
// 2. OrderedCapability(100)  
// 3. RegularCapability("First")
// 4. RegularCapability("Second")
```

## Query Patterns

### Contract-Based Ordering

```csharp
// Define contract interface with ordering
public interface IProcessorCapability<T> : ICapability<T>
{
    Task ProcessAsync(T subject);
}

public record ValidationProcessor<T> : IProcessorCapability<T>, IOrderedCapability
{
    public int Order => 100;
    public async Task ProcessAsync(T subject) { /* validate */ }
}

public record TransformProcessor<T> : IProcessorCapability<T>, IOrderedCapability
{
    public int Order => 200;
    public async Task ProcessAsync(T subject) { /* transform */ }
}

// Register under contract and get ordered results
var composition = Composer.For(data)
    .AddAs<IProcessorCapability<Data>>(new TransformProcessor<Data>())
    .AddAs<IProcessorCapability<Data>>(new ValidationProcessor<Data>())
    .Build();

// Query by contract - still returns in order
var processors = composition.GetAll<IProcessorCapability<Data>>();
// Result: ValidationProcessor (100), TransformProcessor (200)
```

### Conditional Ordering

```csharp
public record ConditionalCapability<T>(string Name, bool IsHighPriority) 
    : ICapability<T>, IOrderedCapability
{
    public int Order => IsHighPriority ? 0 : 1000;
}

// Dynamic priority based on conditions
var composition = Composer.For(service)
    .Add(new ConditionalCapability<Service>("Normal", false))  // Order: 1000
    .Add(new ConditionalCapability<Service>("Critical", true)) // Order: 0
    .Build();
```

## Performance Considerations

- **Sorting Overhead**: Ordering happens during composition building, not during queries
- **Query Performance**: `GetAll<T>()` returns pre-ordered results in O(1) time
- **Memory Impact**: No additional memory overhead for ordering
- **Registration Order**: The order of `.Add()` calls doesn't affect final ordering

## Best Practices

### 1. Use Meaningful Order Values

```csharp
// Good: Clear priority levels
public record MiddlewareCapability<T> : ICapability<T>, IOrderedCapability
{
    public int Order => Stage switch
    {
        "Security" => 100,
        "Authentication" => 200,
        "Authorization" => 300,
        "Business" => 400,
        "Logging" => 500,
        _ => 1000
    };
}

// Avoid: Magic numbers without context
public record BadMiddleware<T> : ICapability<T>, IOrderedCapability
{
    public int Order => 73; // What does 73 mean?
}
```

### 2. Leave Gaps for Extensibility

```csharp
// Good: Use gaps for future insertions
public enum ProcessingOrder
{
    PreValidation = 100,    // Room for 101-199
    Validation = 200,       // Room for 201-299
    PostValidation = 300,   // Room for 301-399
    Business = 400          // Room for 401-499
}

// Avoid: Consecutive numbers
public enum BadOrder
{
    First = 1,   // No room for insertion
    Second = 2,  // No room for insertion
    Third = 3    // No room for insertion
}
```

### 3. Document Ordering Contracts

```csharp
/// <summary>
/// Middleware capability with standard ordering:
/// - Security/Auth: 0-99
/// - Validation: 100-199
/// - Business Logic: 200-299
/// - Logging/Cleanup: 300+
/// </summary>
public abstract record MiddlewareCapability<T>(int Priority) 
    : ICapability<T>, IOrderedCapability
{
    public int Order => Priority;
}
```

### 4. Test Ordering Behavior

```csharp
[Test]
public void Capabilities_Should_Execute_In_Order()
{
    var composition = Composer.For(subject)
        .Add(new ProcessorCapability<Subject>("Third", 300))
        .Add(new ProcessorCapability<Subject>("First", 100))
        .Add(new ProcessorCapability<Subject>("Second", 200))
        .Build();
    
    var processors = composition.GetAll<ProcessorCapability<Subject>>();
    
    Assert.AreEqual("First", processors[0].Name);
    Assert.AreEqual("Second", processors[1].Name);
    Assert.AreEqual("Third", processors[2].Name);
}
```

Capability ordering provides powerful control over execution sequences while maintaining the flexibility and composability of the capabilities system.