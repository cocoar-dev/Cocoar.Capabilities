namespace Cocoar.Capabilities.Core.Tests;

public class DebugAddTests
{
    public interface IValidationCapability : ICapability<string> { }
    
    public class EmailValidationCapability : IValidationCapability
    {
        public string Subject => "test";
        public string Email { get; }
        
        public EmailValidationCapability(string email)
        {
            Email = email;
        }
    }

    [Fact]
    public void Debug_Add_Only()
    {
        
        var capability = new EmailValidationCapability("test@example.com");
        
        
        var bag = Composer.For("test")
            .AddAs<IValidationCapability>(capability)
            .Build();
        
        
        Assert.True(bag.TryGet<IValidationCapability>(out var contract));
        Assert.Same(capability, contract);
        
        // Should NOT be queryable by concrete type (contract-only registration)
        Assert.False(bag.TryGet<EmailValidationCapability>(out var concrete));
        
        Assert.Equal(1, bag.Count<IValidationCapability>());
        Assert.Equal(0, bag.Count<EmailValidationCapability>());
    }
    
    [Fact]
    public void Debug_AddAs_Storage()
    {
        
        var capability = new EmailValidationCapability("test@example.com");
        
        
        var builder = Composer.For("test");
        builder.AddAs<IValidationCapability>(capability);
        var bag = builder.Build();
        
        // Debug - Check if anything is stored under the concrete type
        var allConcrete = bag.GetAll<EmailValidationCapability>();
        var allValidation = bag.GetAll<IValidationCapability>();
        
        Console.WriteLine($"Concrete type count: {allConcrete.Count}");
        Console.WriteLine($"Interface type count: {allValidation.Count}");
        
        // The AddAs should store under concrete type but filter for concrete queries
        Assert.Equal(0, allConcrete.Count); // Should be filtered out for concrete queries
        Assert.Equal(1, allValidation.Count); // Should be queryable via interface
        
        // But concrete queries should be filtered out
        Assert.False(bag.TryGet<EmailValidationCapability>(out _));
    }
    
    [Fact]
    public void Debug_Both_Separate()
    {
        
        var addCapability = new EmailValidationCapability("add@example.com");
        var addAsCapability = new EmailValidationCapability("addas@example.com");
        
        Console.WriteLine($"AddCapability: {addCapability.GetHashCode()} - {addCapability.Email}");
        Console.WriteLine($"AddAsCapability: {addAsCapability.GetHashCode()} - {addAsCapability.Email}");
        
        
        var bag = Composer.For("test")
            .AddAs<(EmailValidationCapability, IValidationCapability)>(addCapability)  // Concrete + interface
            .AddAs<IValidationCapability>(addAsCapability)                              // Interface only
            .Build();
        
        // Debug - Check concrete storage directly
        var allConcrete = bag.GetAll<EmailValidationCapability>();
        var allValidation = bag.GetAll<IValidationCapability>();
        
        Console.WriteLine($"Concrete storage count: {allConcrete.Count}");
        Console.WriteLine($"Interface query count: {allValidation.Count}");
        
        foreach (var item in allConcrete)
        {
            Console.WriteLine($"  Concrete item: {item.GetHashCode()} - {((EmailValidationCapability)item).Email}");
        }
        
        foreach (var item in allValidation)
        {
            Console.WriteLine($"  Interface item: {item.GetHashCode()} - {((EmailValidationCapability)item).Email}");
        }
        
        // Current behavior: contract-only filtering works correctly in mixed scenarios
        // Contract-only registrations are not returned for concrete queries
        Assert.Single(allConcrete); // Only the one registered for concrete type
        Assert.Equal(2, allValidation.Count); // Both (both registered for interface)
    }
}
