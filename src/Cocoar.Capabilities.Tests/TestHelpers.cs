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

public record TestCapability(string Name);
public record DocumentCapability(string Type, string Content);
public record IntCapability(int Value);
public record GuidCapability(string Description);
public record StructCapability(string Data);

public record PrimaryTestCapability(string Name) : IPrimaryCapability;
public record DocumentPrimaryCapability(string Title) : IPrimaryCapability;
public record IntPrimaryCapability(string Description) : IPrimaryCapability;
public record AlternatePrimaryCapability(string Value) : IPrimaryCapability;

public record TestPrimaryCapability(string Id, string Description) : IPrimaryCapability;

public record OrderedCapability(int Order, string Name);
public record HighPriorityCapability(string Name)
{
    public static int Order => -100;
}
public record LowPriorityCapability(string Name)
{
    public static int Order => 100;
}

public record OrderedTestCapability(string Name, int Order);
public record OrderedPrimaryTestCapability(string Id, string Description, int Order) : IPrimaryCapability;

public interface IValidationCapability 
{
    bool IsValid { get; }
}

public interface ILoggingCapability   
{
    void Log(string message);
}

public interface ITestContract  { }
public interface IAlternateContract  { }

public record ValidationCapability(bool IsValid, string Rule) : IValidationCapability;
public record LoggingCapability(string LoggerName) : ILoggingCapability
{
    public void Log(string message) { /* No-op for tests */ }
}

public record TestContractImplementation(string Name, string Description) : ITestContract;
public record AlternateContractImplementation(string Name, int Value) : IAlternateContract;
public record MultiContractImplementation(string Name, string Description, int Value) : ITestContract, IAlternateContract;
public record TestContractPrimaryCapability(string Id, string Description) : IPrimaryCapability, ITestContract;

public record CompositeCapability(string Name, object Inner);
public record ConditionalCapability(string Name, bool Condition);

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
