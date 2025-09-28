# Tagged Capabilities - Processing in Grouped Order

This example demonstrates how to use **tagged capabilities** to process capabilities in logical groups while maintaining order within each group.

## The Problem

You often need to:
1. **Group capabilities** by logical categories (e.g., "ServiceLifetime", "Validation", "Configuration")
2. **Process each group** in a specific sequence
3. **Maintain order** within each group based on priority

## The Solution: Tagged Capabilities

Tagged capabilities allow you to group related capabilities together and process them in order:

```csharp
using Cocoar.Capabilities;
using Cocoar.Capabilities.Extensions;

// Define tagged capabilities with both grouping and ordering
public record ServiceLifetimeCapability(string ServiceType, int Priority) 
    : ICapability<ConfigureSpec>, ITaggedOrderedCapability
{
    public string Tag => "ServiceLifetime";
    public int Order => Priority;
}

public record ValidationCapability(string Rule) 
    : ICapability<ConfigureSpec>, ITaggedCapability
{
    public string Tag => "Validation";
}

public record ConfigurationCapability(string Key, string Value, int Priority) 
    : ICapability<ConfigureSpec>, ITaggedOrderedCapability
{
    public string Tag => "Configuration";
    public int Order => Priority;
}
```

## Usage Example

```csharp
// Build a configuration spec with tagged capabilities
var configSpec = Composer.For(new ConfigureSpec())
    // Validation capabilities (processed in insertion order since no Order specified)
    .Add(new ValidationCapability("ValidateConnectionString"))  // Runs first
    .Add(new ValidationCapability("ValidatePermissions"))       // Runs second
    
    // Service registration capabilities (ordered by priority)
    .Add(new ServiceLifetimeCapability("Database", 10))      // High priority
    .Add(new ServiceLifetimeCapability("Cache", 30))         // Lower priority
    .Add(new ServiceLifetimeCapability("HttpClient", 20))    // Medium priority
    
    // Configuration capabilities (ordered by priority)
    .Add(new ConfigurationCapability("ConnectionString", "...", 10))
    .Add(new ConfigurationCapability("LogLevel", "Debug", 20))
    .Build();

// Process each tag group in the order you want
ProcessConfigurationSpec(configSpec);

void ProcessConfigurationSpec(ICapabilityBag<ConfigureSpec> bag)
{
    // 1. First, run all validations
    bag.ForEachInTag<ValidationCapability>("Validation", capability =>
    {
        Console.WriteLine($"Validating: {capability.Rule}");
        // Perform validation logic
    });
    
    // 2. Then, register services in priority order
    bag.ForEachInTag<ServiceLifetimeCapability>("ServiceLifetime", capability =>
    {
        Console.WriteLine($"Registering {capability.ServiceType} (Priority: {capability.Priority})");
        // Register service with DI container
    });
    
    // 3. Finally, apply configuration settings in priority order
    bag.ForEachInTag<ConfigurationCapability>("Configuration", capability =>
    {
        Console.WriteLine($"Setting {capability.Key} = {capability.Value} (Priority: {capability.Priority})");
        // Apply configuration
    });
}
```

Output:
```
Validating: ValidateConnectionString
Validating: ValidatePermissions
Registering Database (Priority: 10)
Registering HttpClient (Priority: 20)  
Registering Cache (Priority: 30)
Setting ConnectionString = ... (Priority: 10)
Setting LogLevel = Debug (Priority: 20)
```

## Advanced Usage

### Tag Discovery

```csharp
// Discover all available tags
var availableTags = bag.GetAvailableTags<ICapability<ConfigureSpec>>();
foreach (var tag in availableTags)
{
    Console.WriteLine($"Processing tag: {tag}");
    
    // Get all capabilities for this tag
    var capabilities = bag.GetAllByTag<ICapability<ConfigureSpec>>(tag);
    foreach (var capability in capabilities)
    {
        // Process capability
        ProcessCapability(capability, tag);
    }
}
```

### Flexible Processing Pipeline

```csharp
public class ConfigurationProcessor
{
    private readonly Dictionary<string, int> _tagPriorities = new()
    {
        ["Validation"] = 10,        // Run validations first
        ["ServiceLifetime"] = 20,   // Then register services
        ["Configuration"] = 30,     // Finally apply settings
        ["Cleanup"] = 40           // Last, any cleanup
    };

    public void Process<T>(ICapabilityBag<T> bag)
    {
        var availableTags = bag.GetAvailableTags<ICapability<T>>();
        
        // Process tags in priority order
        foreach (var tag in availableTags.OrderBy(t => _tagPriorities.GetValueOrDefault(t, 999)))
        {
            Console.WriteLine($"Processing tag group: {tag}");
            
            // Process capabilities within this tag (already ordered by IOrderedCapability)
            var capabilities = bag.GetAllByTag<ICapability<T>>(tag);
            foreach (var capability in capabilities)
            {
                ProcessCapability(capability, tag);
            }
        }
    }
    
    private void ProcessCapability<T>(ICapability<T> capability, string tag)
    {
        // Tag-specific processing logic
        switch (tag)
        {
            case "Validation":
                if (capability is ValidationCapability validation)
                    RunValidation(validation.Rule);
                break;
                
            case "ServiceLifetime":
                if (capability is ServiceLifetimeCapability service)
                    RegisterService(service.ServiceType);
                break;
                
            case "Configuration":
                if (capability is ConfigurationCapability config)
                    ApplyConfiguration(config.Key, config.Value);
                break;
        }
    }
}
```

## Key Benefits

1. **Logical Grouping**: Related capabilities are grouped by meaningful tags
2. **Ordered Processing**: Within each group, capabilities are processed in Order priority
3. **Flexible Pipeline**: You control which tag groups to process and in what sequence
4. **Discovery**: You can discover available tags dynamically
5. **Backward Compatible**: Works alongside existing non-tagged capabilities

## Comparison: Before vs After

**Before (without tags):**
```csharp
// You had to get all capabilities and manually filter/sort
var allCapabilities = bag.GetAll<ICapability<ConfigSpec>>();
var validations = allCapabilities.OfType<ValidationCapability>();
var services = allCapabilities.OfType<ServiceLifetimeCapability>().OrderBy(s => s.Priority);
var configs = allCapabilities.OfType<ConfigurationCapability>().OrderBy(c => c.Priority);

// Process each group manually...
```

**After (with tags):**
```csharp
// Clean, intent-revealing code
bag.ForEachInTag<ValidationCapability>("Validation", ProcessValidation);
bag.ForEachInTag<ServiceLifetimeCapability>("ServiceLifetime", ProcessService);
bag.ForEachInTag<ConfigurationCapability>("Configuration", ProcessConfig);
```

This approach gives you **exactly** what you need: the ability to process capabilities in grouped order, maintaining the sequence within each logical category! 🎯