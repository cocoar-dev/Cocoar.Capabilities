using Cocoar.Capabilities;
using System.Reflection;

namespace Cocoar.Capabilities.Tests;

public class MultipleObjectTagsTests
{
    // Test subject
    private sealed record TestSubject;
    
    // Enum for categorization 
    public enum DIOperations { Registration, Lifetime, Scoping }
    public enum ValidationTypes { Required, Format, Business }
    
    // Library marker types
    public static class CocoarConfigurationDI { }
    public static class CocoarValidation { }
    
    // Example capabilities with multiple object tags
    private sealed record DICapability(string Operation) 
        : ICapability<TestSubject>, ITaggedCapability<TestSubject>
    {
        public IReadOnlyCollection<object> Tags => [
            typeof(CocoarConfigurationDI),   // Library identification
            "DI",                           // String categorization  
            DIOperations.Registration,      // Enum-based operation
            Assembly.GetExecutingAssembly() // Assembly-based grouping
        ];
    }
    
    private sealed record ValidationCapability(string Rule) 
        : ICapability<TestSubject>, ITaggedCapability<TestSubject>
    {
        public IReadOnlyCollection<object> Tags => [
            typeof(CocoarValidation),       // Library identification
            "Validation",                   // String categorization
            ValidationTypes.Required        // Enum-based type
        ];
    }
    
    private sealed record MixedTagCapability(string Name) 
        : ICapability<TestSubject>, ITaggedCapability<TestSubject>
    {
        public IReadOnlyCollection<object> Tags => [
            "DI",                           // Shared string tag
            "Mixed",                        // Unique string tag
            42                             // Integer tag
        ];
    }
    
    [Fact]
    public void GetAllByTag_StringTag_ReturnsAllCapabilitiesWithStringTag()
    {
        // Arrange
        var bag = Composer.For(new TestSubject())
            .Add(new DICapability("RegisterAs"))
            .Add(new ValidationCapability("Required"))
            .Add(new MixedTagCapability("Test"))
            .Build();
            
        // Act - Filter by string tag (must query by concrete type)
        var diFromDICapability = bag.GetAllByTag<DICapability>("DI");
        var diFromMixedCapability = bag.GetAllByTag<MixedTagCapability>("DI");
        
        // Assert - Should get capabilities from both types
        Assert.Single(diFromDICapability);
        Assert.Single(diFromMixedCapability);
        
        // Total count should be 2
        var totalDICapabilities = diFromDICapability.Count + diFromMixedCapability.Count;
        Assert.Equal(2, totalDICapabilities);
    }
    
    [Fact]
    public void GetAllByTag_TypeTag_ReturnsCapabilitiesFromSpecificLibrary()
    {
        // Arrange
        var bag = Composer.For(new TestSubject())
            .Add(new DICapability("RegisterAs"))
            .Add(new ValidationCapability("Required"))
            .Add(new MixedTagCapability("Test"))
            .Build();
            
        // Act - Filter by library type
        var diLibraryCapabilities = bag.GetAllByTag<DICapability>(typeof(CocoarConfigurationDI));
        var validationLibraryCapabilities = bag.GetAllByTag<ValidationCapability>(typeof(CocoarValidation));
        
        // Assert - Should get only capabilities from respective libraries
        Assert.Single(diLibraryCapabilities);
        Assert.Single(validationLibraryCapabilities);
    }
    
    [Fact]
    public void GetAllByTag_EnumTag_ReturnscapabilitiesWithEnumTag()
    {
        // Arrange
        var bag = Composer.For(new TestSubject())
            .Add(new DICapability("RegisterAs"))
            .Add(new ValidationCapability("Required"))
            .Build();
            
        // Act - Filter by enum tags
        var registrationOps = bag.GetAllByTag<DICapability>(DIOperations.Registration);
        var requiredValidations = bag.GetAllByTag<ValidationCapability>(ValidationTypes.Required);
        
        // Assert
        Assert.Single(registrationOps);
        Assert.Single(requiredValidations);
    }
    
    [Fact]
    public void GetAllByTag_AssemblyTag_ReturnsCapabilitiesFromAssembly()
    {
        // Arrange
        var bag = Composer.For(new TestSubject())
            .Add(new DICapability("RegisterAs"))
            .Add(new ValidationCapability("Required"))
            .Build();
            
        // Act - Filter by assembly
        var currentAssemblyCapabilities = bag.GetAllByTag<DICapability>(Assembly.GetExecutingAssembly());
        
        // Assert - Only DICapability has assembly tag
        Assert.Single(currentAssemblyCapabilities);
    }
    
    [Fact]
    public void GetAllByTag_IntegerTag_ReturnsCapabilitiesWithIntegerTag()
    {
        // Arrange
        var bag = Composer.For(new TestSubject())
            .Add(new MixedTagCapability("Test"))
            .Build();
            
        // Act - Filter by integer tag
        var intTagCapabilities = bag.GetAllByTag<MixedTagCapability>(42);
        
        // Assert
        Assert.Single(intTagCapabilities);
    }
    
    [Fact]
    public void GetAllByTags_MultipleObjectTags_ReturnsIntersection()
    {
        // Arrange
        var bag = Composer.For(new TestSubject())
            .Add(new DICapability("RegisterAs"))
            .Add(new ValidationCapability("Required"))  
            .Add(new MixedTagCapability("Test"))
            .Build();
            
        // Act - Find capabilities with BOTH "DI" string AND CocoarConfigurationDI type
        var specificDI = bag.GetAllByTags<DICapability>("DI", typeof(CocoarConfigurationDI));
        
        // Assert - Only DICapability has both tags
        Assert.Single(specificDI);
    }
    
    [Fact]
    public void GetAllTags_ReturnsAllUniqueTagsUsedInBag()
    {
        // Arrange
        var bag = Composer.For(new TestSubject())
            .Add(new DICapability("RegisterAs"))
            .Add(new ValidationCapability("Required"))
            .Add(new MixedTagCapability("Test"))
            .Build();
            
        // Act
        var allTags = bag.GetAllTags();
        
        // Assert - Should contain all unique tags from all capabilities
        Assert.Contains(typeof(CocoarConfigurationDI), allTags);
        Assert.Contains(typeof(CocoarValidation), allTags);
        Assert.Contains("DI", allTags);
        Assert.Contains("Validation", allTags);
        Assert.Contains("Mixed", allTags);
        Assert.Contains(DIOperations.Registration, allTags);
        Assert.Contains(ValidationTypes.Required, allTags);
        Assert.Contains(Assembly.GetExecutingAssembly(), allTags);
        Assert.Contains(42, allTags);
        
        // Should have exactly 9 unique tags
        Assert.Equal(9, allTags.Count);
    }
    
    [Fact]
    public void GetAllTagsOfType_StringTags_ReturnsOnlyStringTags()
    {
        // Arrange  
        var bag = Composer.For(new TestSubject())
            .Add(new DICapability("RegisterAs"))
            .Add(new ValidationCapability("Required"))
            .Add(new MixedTagCapability("Test"))
            .Build();
            
        // Act
        var stringTags = bag.GetAllTagsOfType<string>();
        
        // Assert - Should contain only string tags
        Assert.Contains("DI", stringTags);
        Assert.Contains("Validation", stringTags);
        Assert.Contains("Mixed", stringTags);
        Assert.Equal(3, stringTags.Count);
    }
    
    [Fact]
    public void GetAllTagsOfType_TypeTags_ReturnsOnlyTypeTags()
    {
        // Arrange
        var bag = Composer.For(new TestSubject())
            .Add(new DICapability("RegisterAs"))
            .Add(new ValidationCapability("Required"))
            .Build();
            
        // Act
        var typeTags = bag.GetAllTagsOfType<Type>();
        
        // Assert - Should contain only Type tags
        Assert.Contains(typeof(CocoarConfigurationDI), typeTags);
        Assert.Contains(typeof(CocoarValidation), typeTags);
        Assert.Equal(2, typeTags.Count);
    }

    [Fact]
    public void RealWorldScenario_LibraryDiscovery_FindsCapabilitiesFromSpecificLibraries()
    {
        // Arrange - Simulate capabilities from different libraries
        var bag = Composer.For(new TestSubject())
            // Cocoar.Configuration.DI capabilities
            .Add(new DICapability("RegisterAs"))
            .Add(new DICapability("SetLifetime"))
            
            // Cocoar.Validation capabilities  
            .Add(new ValidationCapability("Required"))
            .Add(new ValidationCapability("Format"))
            
            // Mixed/third-party capability
            .Add(new MixedTagCapability("ThirdParty"))
            .Build();
            
        // Act & Assert - Discover capabilities by library (query by concrete types)
        var diLibraryCapabilities = bag.GetAllByTag<DICapability>(typeof(CocoarConfigurationDI));
        var validationLibraryCapabilities = bag.GetAllByTag<ValidationCapability>(typeof(CocoarValidation));
        
        Assert.Equal(2, diLibraryCapabilities.Count);
        Assert.Equal(2, validationLibraryCapabilities.Count);
        
        // Act & Assert - Discover capabilities by functional area (combine results from different types)
        var diFromDIType = bag.GetAllByTag<DICapability>("DI");
        var diFromMixedType = bag.GetAllByTag<MixedTagCapability>("DI");
        var totalDICapabilities = diFromDIType.Count + diFromMixedType.Count;
        Assert.Equal(3, totalDICapabilities); // 2 DICapability + 1 MixedTagCapability
        
        // Act & Assert - Find specific intersections
        var officialDICapabilities = bag.GetAllByTags<DICapability>(
            "DI", 
            typeof(CocoarConfigurationDI)
        );
        Assert.Equal(2, officialDICapabilities.Count);
    }
}