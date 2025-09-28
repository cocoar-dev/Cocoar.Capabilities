namespace Cocoar.Capabilities;

/// <summary>
/// Internal metadata structure for storing capabilities with their order and insertion ID.
/// The insertion ID serves as a stable, global ordering across all capabilities.
/// </summary>
internal readonly record struct CapabilityMetadata(object Capability, int? Order, int InsertionId);
