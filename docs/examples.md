# Cocoar.Capabilities - Examples

This document provides detailed examples and use cases for the Cocoar.Capabilities library.

## Table of Contents

- [Basic Examples](#basic-examples)
- [Primary Capabilities](#primary-capabilities)
- [Multiple Contracts](#multiple-contracts)
- [Capability Ordering](#capability-ordering)
- [Recomposition](#recomposition)
- [Registry Management](#registry-management)
- [Try-Add Pattern](#try-add-pattern)
- [Real-World Use Cases](#real-world-use-cases)

## Basic Examples

### Simple Composition

```csharp
using Cocoar.Capabilities;

// Create a scope
using var scope = new CapabilityScope();

// Define your subject
var document = new Document("README.md");

// Compose capabilities
var composition = scope.For(document)
    .Add(new EditCapability())
    .Add(new PrintCapability())
    .Add(new ShareCapability())
    .Build();

// Retrieve and use capabilities
var editCapabilities = composition.GetAll<EditCapability>();
foreach (var cap in editCapabilities)
{
    cap.Edit(document);
}

// Check if a capability exists
if (composition.Has<PrintCapability>())
{
    Console.WriteLine("Document can be printed");
}

// Get first capability (convenient when you expect only one)
var printCap = composition.GetFirstOrDefault<PrintCapability>();
if (printCap != null)
{
    printCap.Print(document);
}
```

### Getting Single Capabilities

When you know there's only one capability of a type, use `GetFirstOrDefault` or `TryGetFirst`:

```csharp
var composition = scope.For(application)
    .Add(new ConfigurationCapability("appsettings.json"))
    .Add(new LoggingCapability("app.log"))
    .Build();

// GetFirstOrDefault returns null if not found
var config = composition.GetFirstOrDefault<ConfigurationCapability>();
if (config != null)
{
    var setting = config.GetSetting("Key");
}

// TryGetFirst uses out parameter pattern
if (composition.TryGetFirst<LoggingCapability>(out var logger))
{
    logger.Log("Application started");
}

// GetRequiredFirst throws if not found (useful when capability is mandatory)
try
{
    var cache = composition.GetRequiredFirst<CacheCapability>();
    cache.Store("key", value);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Required capability not found: {ex.Message}");
}

// GetLast methods - useful for "override" or "last wins" scenarios
var overrideConfig = composition.GetLastOrDefault<ConfigOverrideCapability>();
if (overrideConfig != null)
{
    // Use the last registered override (highest priority)
    ApplyConfig(overrideConfig);
}

// TryGetLast with out parameter
if (composition.TryGetLast<ThemeCapability>(out var theme))
{
    // Apply the most recently added theme
    ApplyTheme(theme);
}
```

### Working with Multiple Capabilities of Same Type

```csharp
// Add multiple validation capabilities
var composition = scope.For(formData)
    .Add(new ValidationCapability("Email", EmailValidator))
    .Add(new ValidationCapability("Phone", PhoneValidator))
    .Add(new ValidationCapability("ZipCode", ZipValidator))
    .Build();

// Process all validators
var validators = composition.GetAll<ValidationCapability>();
var allValid = validators.All(v => v.Validate());
```

## Primary Capabilities

### Basic Primary Capability

```csharp
public record UserPrimaryCapability(string UserId, string Name) : IPrimaryCapability;
public record AdminCapability(string AdminLevel);
public record AuditCapability(DateTime LastAudit);

var user = new User("user123");

var composition = scope.For(user)
    .Add(new UserPrimaryCapability("user123", "John Doe"))
    .Add(new AdminCapability("Level2"))
    .Add(new AuditCapability(DateTime.Now))
    .Build();

// Retrieve the primary capability
var primary = composition.GetPrimary();
Console.WriteLine($"User: {((UserPrimaryCapability)primary).Name}");

// Type-safe retrieval
if (composition.TryGetPrimaryAs<UserPrimaryCapability>(out var userPrimary))
{
    Console.WriteLine($"User ID: {userPrimary.UserId}");
}

// Or use GetRequiredPrimaryAs for guaranteed non-null
var typedPrimary = composition.GetRequiredPrimaryAs<UserPrimaryCapability>();
Console.WriteLine($"Name: {typedPrimary.Name}");
```

### Primary Capability Validation

```csharp
// Only one primary capability is allowed
var composition = scope.For(subject)
    .Add(new PrimaryCapabilityA("A"));

// This will throw InvalidOperationException
try
{
    composition.Add(new PrimaryCapabilityB("B"));
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message); // "A primary capability is already set..."
}
```

### Replacing Primary Capability via Recomposition

```csharp
var initial = scope.For(user)
    .Add(new GuestPrimaryCapability("guest123"))
    .Build();

// Later, upgrade to registered user
var upgraded = scope.Recompose(initial)
    .WithPrimary(new RegisteredUserPrimaryCapability("user123", "John Doe"))
    .Add(new PreferencesCapability())
    .Build();
```

## Multiple Contracts

### Single Capability, Multiple Interfaces

```csharp
public interface IValidator
{
    bool Validate();
}

public interface IFormatter
{
    string Format();
}

public class DataProcessor : IValidator, IFormatter
{
    public bool Validate() => true;
    public string Format() => "Formatted Data";
}

var processor = new DataProcessor();

var composition = scope.For(myObject)
    .AddAs<(IValidator, IFormatter)>(processor)
    .Build();

// Access via either contract - both return the same instance
var validator = composition.GetAll<IValidator>().First();
var formatter = composition.GetAll<IFormatter>().First();

Console.WriteLine(ReferenceEquals(validator, formatter)); // True
```

### Multiple Contracts with Three or More Interfaces

```csharp
public interface IReadable { }
public interface IWritable { }
public interface ISearchable { }

public class DataStore : IReadable, IWritable, ISearchable
{
    // Implementation
}

var store = new DataStore();

var composition = scope.For(database)
    .AddAs<(IReadable, IWritable, ISearchable)>(store)
    .Build();

// Available under all three contracts
var readers = composition.GetAll<IReadable>();
var writers = composition.GetAll<IWritable>();
var searchers = composition.GetAll<ISearchable>();
```

## Capability Ordering

### Explicit Order Values

```csharp
// Lower numbers = higher priority (returned first)
var composition = scope.For(subject)
    .Add(new LoggingCapability(), order: 10)
    .Add(new ValidationCapability(), order: 5)
    .Add(new ProcessingCapability(), order: 20)
    .Add(new AuditCapability(), order: 15)
    .Build();

var all = composition.GetAll<ICapability>();
// Order: ValidationCapability (5), LoggingCapability (10), 
//        AuditCapability (15), ProcessingCapability (20)
```

### Order Selectors

```csharp
public record TaskCapability(string Name, int Priority);

var composition = scope.For(taskList)
    .Add(new TaskCapability("High Priority", 1), cap => cap.Priority)
    .Add(new TaskCapability("Normal Priority", 5), cap => cap.Priority)
    .Add(new TaskCapability("Low Priority", 10), cap => cap.Priority)
    .Build();

// Tasks retrieved in priority order
var tasks = composition.GetAll<TaskCapability>();
```

### Property-Based Ordering

```csharp
public record OrderedCapability
{
    public string Name { get; init; }
    public int ExecutionOrder { get; init; }
}

var composition = scope.For(pipeline)
    .Add(new OrderedCapability { Name = "Step1", ExecutionOrder = 10 }, 
         cap => cap.ExecutionOrder)
    .Add(new OrderedCapability { Name = "Step2", ExecutionOrder = 5 }, 
         cap => cap.ExecutionOrder)
    .Add(new OrderedCapability { Name = "Step3", ExecutionOrder = 15 }, 
         cap => cap.ExecutionOrder)
    .Build();
```

## Recomposition

### Basic Recomposition

```csharp
var initialComposition = scope.For(document)
    .Add(new ReadCapability())
    .Build();

// Later, add more capabilities
var updatedComposition = scope.Recompose(initialComposition)
    .Add(new WriteCapability())
    .Add(new DeleteCapability())
    .Build();

// Original remains unchanged (immutable)
Console.WriteLine(initialComposition.TotalCapabilityCount); // 1
Console.WriteLine(updatedComposition.TotalCapabilityCount); // 3
```

### Conditional Recomposition

```csharp
var composition = scope.For(user).Add(new BasicUserCapability()).Build();

// Add admin capabilities if user is admin
if (userIsAdmin)
{
    composition = scope.Recompose(composition)
        .Add(new AdminCapability())
        .Add(new ModeratorCapability())
        .Build();
}

// Add premium features if subscribed
if (hasPremiumSubscription)
{
    composition = scope.Recompose(composition)
        .Add(new PremiumFeaturesCapability())
        .Build();
}
```

### Recomposition with Ordering Changes

```csharp
var initial = scope.For(pipeline)
    .Add(new StepA(), 1)
    .Add(new StepB(), 2)
    .Build();

// Insert a new step with higher priority
var modified = scope.Recompose(initial)
    .Add(new ValidationStep(), 0) // Runs first now
    .Build();
```

## Registry Management

### Basic Registry Usage

```csharp
// Enable registries
var options = new CapabilityScopeOptions
{
    UseComposerRegistry = true,
    UseCompositionRegistry = true
};

using var scope = new CapabilityScope(options);

// Build with registry
var composition = scope.For(document)
    .Add(new EditCapability())
    .Build(); // Automatically registered

// Find compositions later
var found = scope.Compositions.FindOrDefault(document);
if (found != null)
{
    var capabilities = found.GetAll<EditCapability>();
}
```

### Registry Lookup Patterns

```csharp
// Check if composition exists
if (scope.Compositions.Has(document))
{
    var comp = scope.Compositions.Find(document);
    // Work with composition
}

// Find or return null
var comp = scope.Compositions.FindOrDefault(document);
if (comp != null)
{
    // Use composition
}

// Try pattern
if (scope.Compositions.TryFind(document, out var composition))
{
    // Use composition
}
```

### Composer Registry

```csharp
var options = new CapabilityScopeOptions { UseComposerRegistry = true };
using var scope = new CapabilityScope(options);

// Start composing
var composer = scope.For(document); // Registered automatically

// Check if a composer is active
if (scope.Composers.Has(document))
{
    Console.WriteLine("Document is currently being composed");
}

// Retrieve the composer
if (scope.Composers.TryGet(document, out var activeComposer))
{
    // Can inspect or modify the active composer
}

// Building removes from composer registry, adds to composition registry
var composition = composer.Add(new Capability()).Build();
```

### Per-Operation Registry Override

```csharp
var options = new CapabilityScopeOptions
{
    UseComposerRegistry = false,
    UseCompositionRegistry = false
};

using var scope = new CapabilityScope(options);

// Override: use registry for this specific operation
var composition = scope.For(subject, useRegistry: true)
    .Add(capability)
    .Build(useRegistry: true);

// This composition IS registered despite global settings
var found = scope.Compositions.FindOrDefault(subject);
Console.WriteLine(found != null); // True
```

## Try-Add Pattern

### Basic Try-Add

```csharp
var composition = scope.For(document)
    .Add(new LoggingCapability())
    .TryAdd(new LoggingCapability()) // Won't add duplicate
    .TryAdd(new MetricsCapability())  // Will add (doesn't exist yet)
    .Build();

Console.WriteLine(composition.GetAll<LoggingCapability>().Count); // 1
Console.WriteLine(composition.GetAll<MetricsCapability>().Count); // 1
```

### Conditional Capability Addition

```csharp
var composer = scope.For(user);

// Add base capabilities
composer.Add(new UserProfileCapability());

// Try to add premium features (won't duplicate if already present)
composer.TryAdd(new PremiumFeatureCapability());

// Try to add admin capabilities
if (userIsAdmin)
{
    composer.TryAdd(new AdminCapability());
}

var composition = composer.Build();
```

### Try-Add with Ordering

```csharp
var composition = scope.For(pipeline)
    .Add(new StepA(), 10)
    .TryAdd(new StepA(), 5) // Won't add, already exists
    .TryAdd(new StepB(), 20) // Will add
    .Build();
```

## Real-World Use Cases

### Plugin Architecture

```csharp
public interface IPlugin
{
    string Name { get; }
    void Initialize();
}

public record EditorPlugin(string Name) : IPlugin
{
    public void Initialize() => Console.WriteLine($"Editor: {Name}");
}

public record ExporterPlugin(string Name, string Format) : IPlugin
{
    public void Initialize() => Console.WriteLine($"Exporter: {Name} ({Format})");
}

// Application setup
using var scope = new CapabilityScope();
var app = new Application();

var composition = scope.For(app)
    .Add(new EditorPlugin("TextEditor"))
    .Add(new EditorPlugin("CodeEditor"))
    .Add(new EditorPlugin("MarkdownEditor"))
    .Add(new ExporterPlugin("PDFExporter", "PDF"))
    .Add(new ExporterPlugin("HTMLExporter", "HTML"))
    .Build();

// Initialize all plugins
var allPlugins = composition.GetAll<IPlugin>();
foreach (var plugin in allPlugins)
{
    plugin.Initialize();
}

// Work with specific plugin types
var editors = composition.GetAll<EditorPlugin>();
var exporters = composition.GetAll<ExporterPlugin>();
```

### Role-Based Access Control

```csharp
public record UserRole(string RoleName) : IPrimaryCapability;
public record Permission(string Resource, string Action);

var user = new User("john@example.com");

var composition = scope.For(user)
    .Add(new UserRole("Admin"))
    .Add(new Permission("Users", "Read"))
    .Add(new Permission("Users", "Write"))
    .Add(new Permission("Users", "Delete"))
    .Add(new Permission("Settings", "Read"))
    .Add(new Permission("Settings", "Write"))
    .Build();

// Check specific permission
bool CanPerform(IComposition comp, string resource, string action)
{
    var permissions = comp.GetAll<Permission>();
    return permissions.Any(p => p.Resource == resource && p.Action == action);
}

if (CanPerform(composition, "Users", "Delete"))
{
    Console.WriteLine("User can delete users");
}

// Get role
var role = composition.GetPrimaryAs<UserRole>();
Console.WriteLine($"Role: {role?.RoleName}");
```

### Event Processing Pipeline

```csharp
public interface IEventHandler
{
    Task HandleAsync(object @event);
}

public record AuditLogger() : IEventHandler
{
    public Task HandleAsync(object @event)
    {
        Console.WriteLine($"Audit: {@event}");
        return Task.CompletedTask;
    }
}

public record NotificationSender() : IEventHandler
{
    public Task HandleAsync(object @event)
    {
        Console.WriteLine($"Notify: {@event}");
        return Task.CompletedTask;
    }
}

public record MetricsCollector() : IEventHandler
{
    public Task HandleAsync(object @event)
    {
        Console.WriteLine($"Metrics: {@event}");
        return Task.CompletedTask;
    }
}

// Setup event processing
var order = new Order("ORD-001");

var composition = scope.For(order)
    .Add(new AuditLogger(), order: 1)
    .Add(new NotificationSender(), order: 2)
    .Add(new MetricsCollector(), order: 3)
    .Build();

// Process event through all handlers in order
async Task ProcessEvent(IComposition comp, object @event)
{
    var handlers = comp.GetAll<IEventHandler>();
    foreach (var handler in handlers)
    {
        await handler.HandleAsync(@event);
    }
}

await ProcessEvent(composition, new OrderCreatedEvent(order));
```

### Dynamic Feature Flags

```csharp
public interface IFeature
{
    string FeatureName { get; }
    bool IsEnabled { get; }
}

public record ExperimentalFeature(string FeatureName, bool IsEnabled) : IFeature;
public record BetaFeature(string FeatureName, bool IsEnabled) : IFeature;

var app = new Application();

var composer = scope.For(app);

// Load features from configuration
var featureConfig = LoadFeatureConfiguration();

foreach (var (name, enabled) in featureConfig.ExperimentalFeatures)
{
    composer.Add(new ExperimentalFeature(name, enabled));
}

foreach (var (name, enabled) in featureConfig.BetaFeatures)
{
    composer.Add(new BetaFeature(name, enabled));
}

var composition = composer.Build();

// Check if feature is enabled
bool IsFeatureEnabled(IComposition comp, string featureName)
{
    var features = comp.GetAll<IFeature>();
    return features.FirstOrDefault(f => f.FeatureName == featureName)?.IsEnabled ?? false;
}

if (IsFeatureEnabled(composition, "NewUI"))
{
    // Enable new UI
}
```

### Document Processing with Middleware

```csharp
public interface IDocumentMiddleware
{
    Task ProcessAsync(Document doc);
}

public record ValidationMiddleware() : IDocumentMiddleware
{
    public Task ProcessAsync(Document doc)
    {
        Console.WriteLine("Validating document...");
        return Task.CompletedTask;
    }
}

public record TransformationMiddleware() : IDocumentMiddleware
{
    public Task ProcessAsync(Document doc)
    {
        Console.WriteLine("Transforming document...");
        return Task.CompletedTask;
    }
}

public record PersistenceMiddleware() : IDocumentMiddleware
{
    public Task ProcessAsync(Document doc)
    {
        Console.WriteLine("Saving document...");
        return Task.CompletedTask;
    }
}

// Setup processing pipeline
var document = new Document("report.pdf");

var composition = scope.For(document)
    .Add(new ValidationMiddleware(), order: 1)
    .Add(new TransformationMiddleware(), order: 2)
    .Add(new PersistenceMiddleware(), order: 3)
    .Build();

// Process through pipeline
var middlewares = composition.GetAll<IDocumentMiddleware>();
foreach (var middleware in middlewares)
{
    await middleware.ProcessAsync(document);
}
```

### Service Decorators

```csharp
public interface INotificationService
{
    Task SendAsync(string message);
}

public record EmailNotification() : INotificationService
{
    public Task SendAsync(string message)
    {
        Console.WriteLine($"Email: {message}");
        return Task.CompletedTask;
    }
}

public record SMSNotification() : INotificationService
{
    public Task SendAsync(string message)
    {
        Console.WriteLine($"SMS: {message}");
        return Task.CompletedTask;
    }
}

public record PushNotification() : INotificationService
{
    public Task SendAsync(string message)
    {
        Console.WriteLine($"Push: {message}");
        return Task.CompletedTask;
    }
}

// User preferences determine notification methods
var user = new User("user@example.com");

var composer = scope.For(user);

if (preferences.EmailEnabled)
    composer.Add(new EmailNotification());

if (preferences.SMSEnabled)
    composer.Add(new SMSNotification());

if (preferences.PushEnabled)
    composer.Add(new PushNotification());

var composition = composer.Build();

// Send via all enabled channels
async Task NotifyUser(IComposition comp, string message)
{
    var services = comp.GetAll<INotificationService>();
    await Task.WhenAll(services.Select(s => s.SendAsync(message)));
}

await NotifyUser(composition, "Your order has shipped!");
```
