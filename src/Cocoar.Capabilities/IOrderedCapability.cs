namespace Cocoar.Capabilities;

/// <summary>
/// Interface for capabilities that require specific ordering during processing.
/// Lower Order values run first (0, 10, 100...).
/// Non-IOrderedCapability implementations are treated as Order = 0.
/// </summary>
public interface IOrderedCapability
{
    /// <summary>
    /// The order priority for this capability. Lower values run first.
    /// </summary>
    int Order { get; }
}