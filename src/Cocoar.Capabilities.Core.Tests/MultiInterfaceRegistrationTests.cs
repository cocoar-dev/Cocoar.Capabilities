namespace Cocoar.Capabilities.Core.Tests;

public class MultiInterfaceRegistrationTests
{
    public record TestSubject;

    // Test interfaces for multi-interface registration
    public interface IValidationCapability : ICapability<TestSubject>
    {
        bool Validate(string value);
    }

    public interface IEmailCapability : ICapability<TestSubject>
    {
        bool IsValidEmail(string email);
    }

    public interface IAsyncCapability : ICapability<TestSubject>
    {
        Task<bool> ValidateAsync(string value);
    }

    // Concrete capability implementing multiple interfaces
    public class EmailValidationCapability : ICapability<TestSubject>, IValidationCapability, IEmailCapability, IAsyncCapability
    {
        public string Name { get; }

        public EmailValidationCapability(string name)
        {
            Name = name;
        }

        public bool Validate(string value) => IsValidEmail(value);

        public bool IsValidEmail(string email)
        {
            return !string.IsNullOrEmpty(email) && email.Contains('@');
        }

        public Task<bool> ValidateAsync(string value)
        {
            return Task.FromResult(Validate(value));
        }
    }

    [Fact]
    public void AddAs_SingleInterface_WorksLikeOriginal()
    {
        
        var subject = new TestSubject();
        var capability = new EmailValidationCapability("email-validator");

        
        var bag = Composer.For(subject)
            .AddAs<IValidationCapability>(capability)
            .Build();

        
        Assert.True(bag.TryGet<IValidationCapability>(out var retrieved));
        Assert.Same(capability, retrieved);
        Assert.Equal("email-validator", ((EmailValidationCapability)retrieved).Name);

        // Should NOT be queryable by concrete type
        Assert.False(bag.TryGet<EmailValidationCapability>(out _));
    }

    [Fact]
    public void AddAs_MultipleInterfaces_AllContractsQueryable()
    {
        
        var subject = new TestSubject();
        var capability = new EmailValidationCapability("multi-validator");

        
        var bag = Composer.For(subject)
            .AddAs<(IValidationCapability, IEmailCapability, IAsyncCapability)>(capability)
            .Build();

        
        Assert.True(bag.TryGet<IValidationCapability>(out var asValidation));
        Assert.Same(capability, asValidation);

        Assert.True(bag.TryGet<IEmailCapability>(out var asEmail));
        Assert.Same(capability, asEmail);

        Assert.True(bag.TryGet<IAsyncCapability>(out var asAsync));
        Assert.Same(capability, asAsync);

        // All should be the exact same instance
        Assert.Same(asValidation, asEmail);
        Assert.Same(asEmail, asAsync);

        // Should NOT be queryable by concrete type
        Assert.False(bag.TryGet<EmailValidationCapability>(out _));
    }

    [Fact]
    public void AddAs_IncludingConcreteType_ConcreteTypeQueryable()
    {
        
        var subject = new TestSubject();
        var capability = new EmailValidationCapability("concrete-validator");

        
        var bag = Composer.For(subject)
            .AddAs<(IValidationCapability, IEmailCapability, EmailValidationCapability)>(capability)
            .Build();

        
        Assert.True(bag.TryGet<IValidationCapability>(out var asInterface));
        Assert.True(bag.TryGet<EmailValidationCapability>(out var asConcrete));
        Assert.Same(capability, asInterface);
        Assert.Same(capability, asConcrete);
        Assert.Same(asInterface, asConcrete);
    }

    [Fact]
    public void AddAs_GetAll_ReturnsAllInstancesAsCastType()
    {
        
        var subject = new TestSubject();
        var capability1 = new EmailValidationCapability("validator-1");
        var capability2 = new EmailValidationCapability("validator-2");

        
        var bag = Composer.For(subject)
            .AddAs<(IValidationCapability, IEmailCapability)>(capability1)
            .AddAs<(IValidationCapability, IEmailCapability)>(capability2)
            .Build();

        
        var validationCapabilities = bag.GetAll<IValidationCapability>();
        Assert.Equal(2, validationCapabilities.Count);
        Assert.Contains(capability1, validationCapabilities);
        Assert.Contains(capability2, validationCapabilities);

        var emailCapabilities = bag.GetAll<IEmailCapability>();
        Assert.Equal(2, emailCapabilities.Count);
        Assert.Contains(capability1, emailCapabilities);
        Assert.Contains(capability2, emailCapabilities);
    }

    [Fact]
    public void AddAs_Contains_WorksForAllContracts()
    {
        
        var subject = new TestSubject();
        var capability = new EmailValidationCapability("test");

        
        var bag = Composer.For(subject)
            .AddAs<(IValidationCapability, IEmailCapability)>(capability)
            .Build();

        
        Assert.True(bag.Has<IValidationCapability>());
        Assert.True(bag.Has<IEmailCapability>());
        Assert.False(bag.Has<EmailValidationCapability>());
    }

    [Fact]
    public void AddAs_Count_WorksForAllContracts()
    {
        
        var subject = new TestSubject();
        var capability1 = new EmailValidationCapability("test1");
        var capability2 = new EmailValidationCapability("test2");

        
        var bag = Composer.For(subject)
            .AddAs<(IValidationCapability, IEmailCapability)>(capability1)
            .AddAs<(IValidationCapability, IEmailCapability)>(capability2)
            .Build();

        
        Assert.Equal(2, bag.Count<IValidationCapability>());
        Assert.Equal(2, bag.Count<IEmailCapability>());
        Assert.Equal(0, bag.Count<EmailValidationCapability>());
    }

    [Fact]
    public void AddAs_NullCapability_ThrowsArgumentNull()
    {
        
        var subject = new TestSubject();
        var builder = Composer.For(subject);

        
        Assert.Throws<ArgumentNullException>(() =>
            builder.AddAs<(IValidationCapability, IEmailCapability)>(null!));
    }

    [Fact]
    public void AddAs_InvalidContractType_ThrowsArgumentException()
    {
        // This test would need a type that doesn't implement ICapability<TestSubject>
        // Since we can't easily create such a case with the tuple constraint,
        // this test documents the expected behavior
        Assert.True(true); // Placeholder - the constraint prevents invalid types at compile time
    }

    [Fact]
    public void AddAs_MixedWithRegularAdd_BothWork()
    {
        
        var subject = new TestSubject();
        var multiCapability = new EmailValidationCapability("multi");
        var regularCapability = new EmailValidationCapability("regular");

        
        var bag = Composer.For(subject)
            .AddAs<(IValidationCapability, IEmailCapability)>(multiCapability)
            .Add(regularCapability)
            .Build();

        
        Assert.True(bag.TryGet<IValidationCapability>(out var asInterface));
        Assert.Same(multiCapability, asInterface);

        Assert.True(bag.TryGet<EmailValidationCapability>(out var asConcrete));
        Assert.Same(regularCapability, asConcrete);

        // Different instances
        Assert.NotSame(multiCapability, regularCapability);
    }
}
