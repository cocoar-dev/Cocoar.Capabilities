using Cocoar.Capabilities;
using Cocoar.Capabilities.Extensions;

namespace Cocoar.Capabilities.Tests;

public class TaggedCapabilityTests
{
    private record TestSubject;

    // Example tagged capabilities for service lifetime management
    private record ServiceLifetimeCapability(string Lifetime, int Priority) 
        : ICapability<TestSubject>, ITaggedOrderedCapability<TestSubject>
    {
        public IReadOnlyCollection<object> Tags => ["ServiceLifetime"];
        public int Order => Priority;
    }

    // Example tagged capabilities for validation
    private record ValidationCapability(string Rule) 
        : ICapability<TestSubject>, ITaggedCapability<TestSubject>
    {
        public IReadOnlyCollection<object> Tags => ["Validation"];
    }

    // Example tagged capabilities for configuration
    private record ConfigurationCapability(string Key, string Value, int Order) 
        : ICapability<TestSubject>, ITaggedOrderedCapability<TestSubject>
    {
        public IReadOnlyCollection<object> Tags => ["Configuration"];
        public int Order { get; } = Order;
    }

    [Fact]
    public void GetAllByTag_ServiceLifetimeCapabilities_ReturnsInCorrectOrder()
    {
        // Arrange - Add service lifetime capabilities in mixed order
        var bag = Composer.For(new TestSubject())
            .Add(new ServiceLifetimeCapability("Transient", 30))
            .Add(new ServiceLifetimeCapability("Singleton", 10))  // Should be first
            .Add(new ServiceLifetimeCapability("Scoped", 20))     // Should be middle
            .Build();

        // Act - Get all service lifetime capabilities by tag
        var lifetimeCapabilities = bag.GetAllByTag<ServiceLifetimeCapability>("ServiceLifetime");

        // Assert - Should be ordered by priority
        Assert.Equal(3, lifetimeCapabilities.Count);
        Assert.Equal("Singleton", lifetimeCapabilities[0].Lifetime);   // Order = 10
        Assert.Equal("Scoped", lifetimeCapabilities[1].Lifetime);      // Order = 20
        Assert.Equal("Transient", lifetimeCapabilities[2].Lifetime);   // Order = 30
    }

    [Fact]
    public void GetAllByTag_MixedTaggedCapabilities_OnlyReturnsMatchingTag()
    {
        // Arrange - Mix different types of tagged capabilities
        var bag = Composer.For(new TestSubject())
            .Add(new ServiceLifetimeCapability("Singleton", 10))
            .Add(new ValidationCapability("Required"))
            .Add(new ConfigurationCapability("ConnectionString", "...", 5))
            .Add(new ServiceLifetimeCapability("Scoped", 20))
            .Add(new ValidationCapability("MaxLength"))
            .Build();

        // Act & Assert - Each tag group should return only its own capabilities
        var lifetimeCapabilities = bag.GetAllByTag<ServiceLifetimeCapability>("ServiceLifetime");
        Assert.Equal(2, lifetimeCapabilities.Count);
        Assert.All(lifetimeCapabilities, cap => Assert.Contains("ServiceLifetime", cap.Tags));

        // Even though we ask for ValidationCapability by ServiceLifetime tag, we get nothing
        // because ValidationCapability instances have Tag = "Validation"
        var validationByWrongTag = bag.GetAllByTag<ValidationCapability>("ServiceLifetime");
        Assert.Empty(validationByWrongTag);

        // But asking for the right tag works
        var validationCapabilities = bag.GetAllByTag<ValidationCapability>("Validation");
        Assert.Equal(2, validationCapabilities.Count);
    }

    [Fact]
    public void GetAllByTag_NullTag_ThrowsArgumentNullException()
    {
        // Arrange
        var bag = Composer.For(new TestSubject()).Build();

        // Act & Assert  
        Assert.Throws<ArgumentNullException>(() => bag.GetAllByTag<ServiceLifetimeCapability>(null!));
    }

    [Fact]
    public void GetAllByTag_NoCapabilitiesWithTag_ReturnsEmpty()
    {
        // Arrange
        var bag = Composer.For(new TestSubject())
            .Add(new ValidationCapability("Required")) // Has "Validation" tag
            .Build();

        // Act
        var result = bag.GetAllByTag<ValidationCapability>("NonExistentTag");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ForEachInTag_ProcessesCapabilitiesInOrder()
    {
        // Arrange
        var bag = Composer.For(new TestSubject())
            .Add(new ConfigurationCapability("Database", "...", 30))
            .Add(new ConfigurationCapability("Cache", "...", 10))     // Should be processed first
            .Add(new ConfigurationCapability("Logging", "...", 20))  // Should be processed second
            .Build();

        var processedKeys = new List<string>();

        // Act - Use the extension method to process in order
        bag.ForEachInTag<TestSubject, ConfigurationCapability>("Configuration", capability =>
        {
            processedKeys.Add(capability.Key);
        });

        // Assert - Should be processed in order of priority
        Assert.Equal(3, processedKeys.Count);
        Assert.Equal("Cache", processedKeys[0]);      // Order = 10
        Assert.Equal("Logging", processedKeys[1]);    // Order = 20  
        Assert.Equal("Database", processedKeys[2]);   // Order = 30
    }

    [Fact]
    public void GetAvailableTags_ReturnsAllUniqueTags()
    {
        // Arrange - Create capabilities with various tags
        var bag = Composer.For(new TestSubject())
            .Add(new ServiceLifetimeCapability("Singleton", 10))
            .Add(new ValidationCapability("Required"))
            .Add(new ConfigurationCapability("Key1", "Value1", 5))
            .Add(new ServiceLifetimeCapability("Scoped", 20)) // Duplicate tag
            .Add(new ConfigurationCapability("Key2", "Value2", 10)) // Duplicate tag
            .Build();

        // Act - We need to check each capability type individually or use a base interface approach
        var allTags = new HashSet<object>();
        
        // Collect tags from each capability type
        var serviceTags = bag.GetAvailableTags<TestSubject, ServiceLifetimeCapability>();
        var validationTags = bag.GetAvailableTags<TestSubject, ValidationCapability>();
        var configTags = bag.GetAvailableTags<TestSubject, ConfigurationCapability>();
        
        foreach (var tag in serviceTags) allTags.Add(tag);
        foreach (var tag in validationTags) allTags.Add(tag);
        foreach (var tag in configTags) allTags.Add(tag);
        
        var availableTags = allTags.OfType<string>().OrderBy(t => t).ToList();

        // Assert - Should return unique tags in sorted order
        Assert.Equal(3, availableTags.Count);
        Assert.Equal("Configuration", availableTags[0]);   // Alphabetically first
        Assert.Equal("ServiceLifetime", availableTags[1]);
        Assert.Equal("Validation", availableTags[2]);      // Alphabetically last
    }

    [Fact]
    public void RealWorldScenario_ServiceConfigurationPipeline_ProcessesByTag()
    {
        // Arrange - Simulate a service configuration scenario
        var bag = Composer.For(new TestSubject())
            // Validation capabilities (should run first)
            .Add(new ValidationCapability("ValidateConnectionString"))
            .Add(new ValidationCapability("ValidatePermissions"))
            
            // Service lifetime capabilities (in priority order)
            .Add(new ServiceLifetimeCapability("RegisterDatabase", 10))
            .Add(new ServiceLifetimeCapability("RegisterCache", 20))
            .Add(new ServiceLifetimeCapability("RegisterHttpClient", 30))
            
            // Configuration capabilities (final setup)
            .Add(new ConfigurationCapability("SetupLogging", "Debug", 10))
            .Add(new ConfigurationCapability("SetupMetrics", "Enabled", 20))
            .Build();

        // Act & Assert - Process each tag group in logical order
        
        // 1. First, run all validations
        var validationRules = new List<string>();
        bag.ForEachInTag<TestSubject, ValidationCapability>("Validation", cap =>
        {
            validationRules.Add(cap.Rule);
        });
        Assert.Equal(2, validationRules.Count);
        
        // 2. Then, register services in priority order
        var serviceRegistrations = new List<string>();
        bag.ForEachInTag<TestSubject, ServiceLifetimeCapability>("ServiceLifetime", cap =>
        {
            serviceRegistrations.Add(cap.Lifetime);
        });
        Assert.Equal(3, serviceRegistrations.Count);
        Assert.Equal("RegisterDatabase", serviceRegistrations[0]);  // Priority 10
        Assert.Equal("RegisterCache", serviceRegistrations[1]);     // Priority 20
        Assert.Equal("RegisterHttpClient", serviceRegistrations[2]); // Priority 30
        
        // 3. Finally, apply configuration settings
        var configurationSettings = new List<string>();
        bag.ForEachInTag<TestSubject, ConfigurationCapability>("Configuration", cap =>
        {
            configurationSettings.Add(cap.Key);
        });
        Assert.Equal(2, configurationSettings.Count);
        Assert.Equal("SetupLogging", configurationSettings[0]);     // Order 10
        Assert.Equal("SetupMetrics", configurationSettings[1]);     // Order 20
    }
}