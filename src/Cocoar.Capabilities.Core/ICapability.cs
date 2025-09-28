namespace Cocoar.Capabilities.Core;

public interface ICapability { }

public interface ICapability<in TSubject> : ICapability { }

public interface IPrimaryCapability<in T> : ICapability<T> { }

public interface IOrderedCapability
{
    int Order { get; }
}
