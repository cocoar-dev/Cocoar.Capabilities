namespace Cocoar.Capabilities.Tests.ConfigTests;

public interface IPrimaryTypeCapability : IPrimaryCapability
{
    public Type SelectedType { get; }
}

public sealed record ConfigureSpec();


public interface IConfigSpec {}
public interface IConfigSpec<T> : IConfigSpec {}

public record ConfigSpec<T> : IConfigSpec<T> {}

public sealed record ConcreteTypePrimary<T>(Type Concrete) : IPrimaryTypeCapability
{
    public Type SelectedType => Concrete;
}

public sealed record ExposeAsCapability<T>(Type ContractType) ;
