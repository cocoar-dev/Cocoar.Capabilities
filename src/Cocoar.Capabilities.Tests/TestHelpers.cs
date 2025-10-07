namespace Cocoar.Capabilities.Tests;

public class StringSubject(string value)
{
    public string Value { get; } = value;
    public override string ToString() => Value;
}

public class DocumentSubject(string name, string content = "")
{
    public string Name { get; } = name;
    public string Content { get; } = content;
    public override string ToString() => Name;
}

public record struct IntSubject(int Value);
public record struct GuidSubject(Guid Id);
public record struct ComplexStruct(int Id, string Name, DateTime Created);

public record TestCapability(string Name) : ICapability<StringSubject>;
public record DocumentCapability(string Type, string Content) : ICapability<DocumentSubject>;
public record IntCapability(int Value) : ICapability<IntSubject>;
public record GuidCapability(string Description) : ICapability<GuidSubject>;
public record StructCapability(string Data) : ICapability<ComplexStruct>;

public record PrimaryTestCapability(string Name) : IPrimaryCapability<StringSubject>;
public record DocumentPrimaryCapability(string Title) : IPrimaryCapability<DocumentSubject>;
public record IntPrimaryCapability(string Description) : IPrimaryCapability<IntSubject>;
public record AlternatePrimaryCapability(string Value) : IPrimaryCapability<StringSubject>;

public record TestPrimaryCapability(string Id, string Description) : IPrimaryCapability<StringSubject>;

public record OrderedCapability(int Order, string Name) : ICapability<StringSubject>, IOrderedCapability;
public record HighPriorityCapability(string Name) : ICapability<StringSubject>, IOrderedCapability
{
    public int Order => -100;
}
public record LowPriorityCapability(string Name) : ICapability<StringSubject>, IOrderedCapability  
{
    public int Order => 100;
}

public record OrderedTestCapability(string Name, int Order) : ICapability<StringSubject>, IOrderedCapability;
public record OrderedPrimaryTestCapability(string Id, string Description, int Order) : IPrimaryCapability<StringSubject>, IOrderedCapability;

public interface IValidationCapability : ICapability<StringSubject>
{
    bool IsValid { get; }
}

public interface ILoggingCapability : ICapability<StringSubject>  
{
    void Log(string message);
}

public interface ITestContract : ICapability<StringSubject> { }
public interface IAlternateContract : ICapability<StringSubject> { }

public record ValidationCapability(bool IsValid, string Rule) : IValidationCapability;
public record LoggingCapability(string LoggerName) : ILoggingCapability
{
    public void Log(string message) { /* No-op for tests */ }
}

public record TestContractImplementation(string Name, string Description) : ITestContract;
public record AlternateContractImplementation(string Name, int Value) : IAlternateContract;
public record MultiContractImplementation(string Name, string Description, int Value) : ITestContract, IAlternateContract;
public record TestContractPrimaryCapability(string Id, string Description) : IPrimaryCapability<StringSubject>, ITestContract;

public record CompositeCapability(string Name, ICapability<StringSubject> Inner) : ICapability<StringSubject>;
public record ConditionalCapability(string Name, bool Condition) : ICapability<StringSubject>;

public static class TestOptions
{
    public static CapabilityScopeOptions Disabled => new() 
    { 
        UseComposerRegistry = false, 
        UseCompositionRegistry = false 
    };
    
    public static CapabilityScopeOptions ComposerOnly => new() 
    { 
        UseComposerRegistry = true, 
        UseCompositionRegistry = false 
    };
    
    public static CapabilityScopeOptions CompositionOnly => new() 
    { 
        UseComposerRegistry = false, 
        UseCompositionRegistry = true 
    };
    
    public static CapabilityScopeOptions BothEnabled => new() 
    { 
        UseComposerRegistry = true, 
        UseCompositionRegistry = true 
    };
}