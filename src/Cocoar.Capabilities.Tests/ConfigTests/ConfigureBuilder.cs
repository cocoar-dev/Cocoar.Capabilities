namespace Cocoar.Capabilities.Tests.ConfigTests;

public interface IConfigureBuilder
{
    public static abstract CapabilityScope GetCapabilityScopeFor(ConfigBuilder builder);
    
}

public abstract class ConfigBuilder(CapabilityScope capabilityScope): IConfigureBuilder
{
    protected readonly CapabilityScope CapabilityScope = capabilityScope;
    internal abstract ConfigBuilder Build();

    public static CapabilityScope GetCapabilityScopeFor(ConfigBuilder builder) => builder.CapabilityScope;

    public static Composer GetComposer(ConfigBuilder builder) =>
        GetCapabilityScopeFor(builder).Composers.FindRequired(builder);
}

public sealed class ConfigureBuilder(CapabilityScope capabilityScope)
{
    private readonly CapabilityScope _capabilityScope = capabilityScope;

    public Guid Id { get; } = Guid.NewGuid();

    public ConcreteConfigBuilder<T> ConcreteType<T>() where T : class => new(_capabilityScope);

    public static CapabilityScope GetCapabilityScopeFor(ConfigureBuilder builder) => builder._capabilityScope;
}
