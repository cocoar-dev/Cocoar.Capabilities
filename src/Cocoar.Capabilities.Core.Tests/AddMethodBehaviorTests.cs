namespace Cocoar.Capabilities.Core.Tests;

/// <summary>
/// Tests for the new Add method behavior that always includes concrete type plus specified contracts
/// </summary>
public class AddMethodBehaviorTests
{
    public interface IValidationCapability : ICapability<string> { }
    public interface IEmailCapability : ICapability<string> { }
    
    public class EmailValidationCapability(string email) : IValidationCapability, IEmailCapability
    {
        public string Email { get; } = email;
    }

    private static string Subject => "test";

    [Fact]
    public void Add_WithoutContract_OnlyConcreteQueryable()
    {
        
        var capability = new EmailValidationCapability("test@example.com");
        
        // When we use Add(), should register under concrete type only
        var bag = Composer.For(Subject)
            .Add(capability)  // Only concrete type
            .Build();

        
        Assert.True(bag.TryGet<EmailValidationCapability>(out var concrete));
        Assert.Same(capability, concrete);
        
        // Interface contracts should NOT be queryable (they weren't specified)
        Assert.False(bag.TryGet<IValidationCapability>(out _));
        Assert.False(bag.TryGet<IEmailCapability>(out _));
    }

    [Fact]
    public void Add_WithSingleContract_ConcreteAndContractQueryable()
    {
        
        var capability = new EmailValidationCapability("test@example.com");
        
        
        var bag = Composer.For(Subject)
            .AddAs<(EmailValidationCapability, IValidationCapability)>(capability)
            .Build();
        
        
        Assert.True(bag.TryGet<EmailValidationCapability>(out var concrete));
        Assert.Same(capability, concrete);
        
        Assert.True(bag.TryGet<IValidationCapability>(out var contract));
        Assert.Same(capability, contract);
        
        // Unspecified contract should NOT be queryable
        Assert.False(bag.TryGet<IEmailCapability>(out _));
    }

    [Fact]
    public void Add_WithMultipleContracts_ConcreteAndAllContractsQueryable()
    {
        // This test checks that AddAs with multiple contracts works as expected
        // (The "Add" in the name is misleading - this tests AddAs behavior)
        
        
        var capability = new EmailValidationCapability("test@example.com");
        
        
        var bag = Composer.For(Subject)
            .AddAs<(IValidationCapability, IEmailCapability, EmailValidationCapability)>(capability)
            .Build();
        
        
        Assert.True(bag.TryGet<EmailValidationCapability>(out var concrete));
        Assert.Same(capability, concrete);
        
        Assert.True(bag.TryGet<IValidationCapability>(out var validation));
        Assert.Same(capability, validation);
        
        Assert.True(bag.TryGet<IEmailCapability>(out var email));
        Assert.Same(capability, email);
    }

    [Fact]
    public void Add_WithSingleContract_AutomaticallyIncludesConcreteType()
    {
        // This test verifies that Add() only registers under concrete type
        // and AddAs<T> with tuple can register under multiple types including concrete type
        
        
        var capability = new EmailValidationCapability("test@example.com");
        
        
        var bag1 = Composer.For(Subject)
            .Add(capability)
            .Build();
            
        
        var bag2 = Composer.For(Subject)
            .AddAs<(EmailValidationCapability, IValidationCapability)>(capability)
            .Build();
        
        
        Assert.True(bag1.TryGet<EmailValidationCapability>(out var concrete1));
        Assert.Same(capability, concrete1);
        Assert.False(bag1.TryGet<IValidationCapability>(out _)); // Interface NOT queryable
        
        
        Assert.True(bag2.TryGet<EmailValidationCapability>(out var concrete2));
        Assert.Same(capability, concrete2);
        Assert.True(bag2.TryGet<IValidationCapability>(out var validation2));
        Assert.Same(capability, validation2);
    }

    [Fact]
    public void Add_WithConcreteTypeInTuple_NoDuplication()
    {
        
        var capability = new EmailValidationCapability("test@example.com");
        
        
        var bag = Composer.For(Subject)
            .AddAs<(IValidationCapability, EmailValidationCapability)>(capability)
            .Build();
        
        
        Assert.True(bag.TryGet<EmailValidationCapability>(out var concrete));
        Assert.Same(capability, concrete);
        
        Assert.True(bag.TryGet<IValidationCapability>(out var validation));
        Assert.Same(capability, validation);
        
        // Should only have one instance of the capability
        var allConcrete = bag.GetAll<EmailValidationCapability>();
        Assert.Single(allConcrete);
    }

    [Fact] 
    public void Add_MixedWithAddAs_BothBehaviorsWork()
    {
        
        var addCapability = new EmailValidationCapability("add@example.com");
        var addAsCapability = new EmailValidationCapability("addas@example.com");
        
        
        var bag = Composer.For(Subject)
            .AddAs<(EmailValidationCapability, IValidationCapability)>(addCapability)        // Concrete + contract
            .AddAs<IValidationCapability>(addAsCapability)    // Contract only
            .Build();
        
        
        // Add capability should be queryable via both concrete and contract
        Assert.True(bag.TryGet<EmailValidationCapability>(out var concrete));
        Assert.Same(addCapability, concrete);  // Should get the one registered for concrete type
        
        // Both should be queryable via interface
        var allValidation = bag.GetAll<IValidationCapability>();
        Assert.Equal(2, allValidation.Count);
        Assert.Contains(addCapability, allValidation);
        Assert.Contains(addAsCapability, allValidation);
        
        // NEW BEHAVIOR: Contract-only registrations are not queryable by concrete type
        var allConcrete = bag.GetAll<EmailValidationCapability>();
        Assert.Single(allConcrete);  // Only the one registered for concrete type
        Assert.Contains(addCapability, allConcrete);
        Assert.DoesNotContain(addAsCapability, allConcrete);  // Contract-only not returned
    }
}
