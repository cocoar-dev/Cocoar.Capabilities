namespace Cocoar.Capabilities.Tests.ConfigTests;


public sealed class ConcreteConfigBuilder<T> : ConfigBuilder where T : class
{
    public Guid Id { get; } = Guid.NewGuid();
    internal ConcreteConfigBuilder(CapabilityScope capabilityScope): base(capabilityScope)
    {
       capabilityScope.Compose(this).WithPrimary(
            new ConcreteTypePrimary<ConfigBuilder>(typeof(T)));
    }
    

    public ConcreteConfigBuilder<T> ExposeAs<TInterface>() where TInterface : class
    {
        var interfaceType = typeof(TInterface);
        if (!interfaceType.IsInterface)
        {
            throw new InvalidOperationException($"{interfaceType.Name} must be an interface.");
        }
        if (!interfaceType.IsAssignableFrom(typeof(T)))
        {
            throw new InvalidOperationException($"Type {typeof(T).Name} does not implement interface {interfaceType.Name}");
        }
        
        GetComposer(this).Add(new ExposeAsCapability<ConfigBuilder>(interfaceType));

        return this;
    }

    internal override ConfigBuilder Build()
    {
        GetComposer(this).Build();
        return this;
    }
}
